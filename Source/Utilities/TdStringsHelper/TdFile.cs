/* References https://github.com/Secre-C/010-Editor-Templates/blob/master/templates/Persona_5R_FTD.bt. */

using Amicitia.IO.Binary;
using Amicitia.IO.Streams;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper;

internal sealed class TdFile : IListValue {
  private static IReadOnlyDictionary<uint, Endianness> s_EndiannessMap = new Dictionary<uint, Endianness>
  {
    /* CTD, FTD, TTD */
    { 0x46544430, Endianness.Little },
    { 0x30445446, Endianness.Big    },
    /* Only for MTD. */
    { 0x30445477, Endianness.Big    },
    { 0x77544430, Endianness.Little },
  };

  ListValueKind IListValue.Kind => ListValueKind.EmbeddedFtd;

  public Endianness    Endianness { get; private set; }
  public TdKind        Kind       { get; private set; }
  /* Just in case it's different somewhere. */
  public uint          Unknown1   { get; private set; }
  public DataKind      DataKind   { get; private set; }
  public List<ITdData> Containers { get; private set; }

  private TdFile() {
    Containers = new List<ITdData>();
  }

  public static TdFile ReadIn(BinaryValueReader in_reader) {
    TdFile result = new();

    switch (Path.GetExtension(in_reader.FilePath).ToLowerInvariant()) {
    case ".ctd":
      result.Kind = TdKind.Ctd;
      break;

    case ".ftd":
      result.Kind = TdKind.Ftd;
      break;

    case ".mtd":
      result.Kind = TdKind.Mtd;
      break;

    case ".ttd":
      result.Kind = TdKind.Ttd;
      break;
    }

    /* We will need to skip over to the magic first, set our endianness, then move back to the beginning.
       We'll also re-read the unknown in the process.
    */
    in_reader.ReadInt32();
    result.Endianness = s_EndiannessMap[in_reader.ReadUInt32()];
    in_reader.Endianness = result.Endianness;

    in_reader.Seek(0, SeekOrigin.Begin);
    result.Unknown1 = in_reader.ReadUInt32();

    /* We'll need to skip over the magic, as we've previously read it in. From there, we read the rest of
       the table.
    */
    in_reader.ReadInt32();

    /* Since we're potentially writing larger data, we'll skip over the file size for now. We're changing
       it anyways. 
    */
    in_reader.ReadInt32();

    result.DataKind = (DataKind)(in_reader.ReadUInt16());
    ushort data_count = in_reader.ReadUInt16();
    result.Containers.EnsureCapacity(data_count);

    /* I love Atlus. The `DataKind` for TTDs is `String`, but the actual layout is closer to `List`. They
       also use fixed size strings instead of variable ones. 
    */
    if (result.Kind is TdKind.Ttd) {
      using SeekToken token = in_reader.At(0x20, SeekOrigin.Begin);
      result.Containers.Add(TdList.ReadIn(in_reader));
    }
    else if (result.DataKind is DataKind.List) {
      for (ushort i = 0; i < data_count; i++) {
        uint offset = in_reader.ReadUInt32();
        /* Temporarily jump over to the offset position, then reloc back to our original position as soon
           as this iteration exits.
        */
        using SeekToken token = in_reader.At(offset, SeekOrigin.Begin);
        result.Containers.Add(TdList.ReadIn(in_reader));
      }
    }
    else {
      for (ushort i = 0; i < data_count; i++) {
        uint offset = in_reader.ReadUInt32();
        /* Temporarily jump over to the offset position, then reloc back to our original position as soon
           as this iteration exits.
        */
        using SeekToken token = in_reader.At(offset, SeekOrigin.Begin);
        result.Containers.Add(TdString.ReadIn(in_reader));
      }
    }

    return result;
  }

  public static TdFile ReadIn(string[] in_strings, JObject in_metadata) {
    TdFile result = new();

    /* First, we'll need to read in the basic properties for the file as a whole. We don't include a size
       since we'll be calculating one at export time. 
    */
    result.DataKind = in_metadata.Property("DataKind")!.ToObject<DataKind>();
    result.Kind = in_metadata.Property("Kind")!.ToObject<TdKind>();
    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();
    result.Endianness = in_metadata.Property("Endianness")!.ToObject<Endianness>();

    var containers = (JArray)(in_metadata.Property("Containers")!.Value);

    if (result.Kind is TdKind.Ttd) {
      /* `TTD`s are guaranteed to contain one `TdList`, so we can simply index zero into `containers`. */
      var list = (JObject)(containers[0]);
      result.Containers.Add(TdList.ReadIn(in_strings, list));
    }
    else if (result.DataKind is DataKind.List) {
      foreach (JObject current in containers)
        result.Containers.Add(TdList.ReadIn(in_strings, current));
    }
    else {
      foreach (JObject current in containers)
        result.Containers.Add(TdString.ReadIn(in_strings, current));
    }

    /* The majority of the reading has essentially finished in the other types, so we can return here. */
    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    /* We have opted to isolate the writing functionality here to keep the `Program` as lean as possible,
       since the scale is a bit bigger here. 
    */
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Endianness");
    in_metadata_writer.WriteValue(Endianness);

    /* We could use the extension to determine what we are dealing with in exports, but we will write the
       `Kind` regardless. 
    */
    in_metadata_writer.WritePropertyName(
      "Kind");
    in_metadata_writer.WriteValue(Kind);

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);
    
    in_metadata_writer.WritePropertyName(
      "DataKind");
    in_metadata_writer.WriteValue(DataKind);

    in_metadata_writer.WritePropertyName(
      "Containers");
    in_metadata_writer.WriteStartArray();

    if (Kind is TdKind.Ttd) {
      /* `TTD`s are guaranteed to contain one `TdList`, so we can simply index zero into `Containers`. */
      var list = (TdList)(Containers[0]);
      list.WriteOut(in_strings_writer, in_metadata_writer);
    }
    else if (DataKind is DataKind.List) {
      foreach (ITdData current in Containers) {
        var list = (TdList)(current);
        list.WriteOut(in_strings_writer, in_metadata_writer);
      }
    }
    else {
      foreach (ITdData current in Containers) {
        var @string = (TdString)(current);
        @string.WriteOut(in_strings_writer, in_metadata_writer);
      }
    }

    in_metadata_writer.WriteEndArray();
    in_metadata_writer.WriteEndObject();
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* Compared to JSON export, we don't need a strings file to write to as well, since all strings embed
       into the tables. 
    */
    in_writer.Endianness = Endianness;

    in_writer.WriteUInt32(Unknown1);
    in_writer.WriteUInt32(Kind is TdKind.Mtd ? 0x77544430u : 0x46544430u);

    /* We will be writing a temporary value here, as this is the `DataSize`, which we will be calculating
       in post. 
    */
    in_writer.WriteUInt32(0x00000000);

    List<uint> offsets = new();
    offsets.EnsureCapacity(Containers.Count);

    /* There's a bit of a deviation here, as for `TTD` files, we'll want to always write `String` for the
       `DataKind` and zero for the `DataCount`. I love Atlus. 
    */
    if (Kind is TdKind.Ttd) {
      in_writer.WriteUInt16((ushort)(DataKind.String));
      in_writer.WriteUInt16(0x0000);
      in_writer.WriteUInt32(0x00000020);
    }
    else {
      in_writer.WriteUInt16((ushort)(DataKind));
      in_writer.WriteUInt16((ushort)(Containers.Count));

      /* For now, we'll want to store temporary values for the offsets, as we have not written containers
         yet. 
      */
      for (int i = 0; i < Containers.Count; i++)
        in_writer.WriteUInt32(0x00000000);
    }

    /* Regardless of `Kind`, we must align at 16 bytes, as this is done within all the original files. */
    in_writer.Align(16);

    if (Kind is TdKind.Ttd) {
      /* We do not want to add any offsets to our collection, since we've already written the offsets. */
      ((TdList)(Containers[0])).WriteOut(in_writer);
    }
    else if (DataKind is DataKind.String) {
      /* String writing is pretty straightforward: store the initial offset, then pass to `TdString` when
         we need to write. 
      */
      foreach (ITdData current in Containers) {
        offsets.Add((uint)(in_writer.Position));
        ((TdString)(current)).WriteOut(in_writer);
      }
    }
    else {
      /* This is also pretty straightforward (at least here): store the initial offset, then move over to
         `TdList` for additional writing. 
      */
      foreach (ITdData current in Containers) {
        offsets.Add((uint)(in_writer.Position));
        ((TdList)(current)).WriteOut(in_writer);
      }
    }

    in_writer.Align(16);

    /* Finally, we'll need to fixup the offsets and the file size that we temporarily wrote earlier. File
       size is relative to the entire file. 
    */
    in_writer.Seek(8, SeekOrigin.Begin);
    in_writer.WriteUInt32((uint)(in_writer.Length));

    /* The size of the header is 16, so we can seek there to reach the collection of offsets. We can then
       write our offsets. 
    */
    in_writer.Seek(16, SeekOrigin.Begin);
    foreach (uint current in offsets)
      in_writer.WriteUInt32(current);

    /* TODO: Some tables seem to contain an additional, albeit empty, table at the end. Look into whether
       or not these should be replicated. 
    */
  }
}
