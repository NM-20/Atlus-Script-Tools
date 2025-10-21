/* References https://github.com/Secre-C/010-Editor-Templates/blob/master/templates/Persona_5R_FTD.bt. */

using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace TdStringsHelper;

internal class TdList : ITdData {
  /* If we know that a particular instance is a `TdList`, we don't need this field, so hide it. */
  DataKind ITdData.Kind => DataKind.List;

  public uint        Unknown1 { get; private set; }
  public IListValue? Value    { get; private set; }
  public ushort      Unknown2 { get; private set; }

  private TdList()
  {}

  public static TdList ReadIn(BinaryValueReader in_reader) {
    /* Lists are bit weird in that they can embed another FTD, so we will need to ensure that this
       is handled appropriately. 
    */
    TdList result = new();

    result.Unknown1  = in_reader.ReadUInt32();
    uint data_size   = in_reader.ReadUInt32();
    uint entry_count = in_reader.ReadUInt32();

    var value_kind = (ListValueKind)(in_reader.ReadUInt16());

    result.Unknown2 = in_reader.ReadUInt16();

    /* If we're dealing with an embedded FTD, we will simply want to call the appropriate `ReadIn`
       implementation from `TdFile`. 
    */
    if (value_kind is ListValueKind.EmbeddedFtd) {
      result.Value = TdFile.ReadIn(in_reader);
      return result;
    }

    /* Now, we will need to create the appropriate element for the table. The only way to view the
       type of the elements is via the filename.
    */
    string filename = Path.GetFileNameWithoutExtension(in_reader.FilePath);

    /* We'll be taking a reflection approach to implementing new data types, making it easier when
       we need to implement new ones. 
    */
    Assembly assembly = Assembly.GetExecutingAssembly();
    Type? type = assembly.GetType($"TdStringsHelper.ListEntries.{filename}");

    DataCollection<object> collection = new();
    collection.EnsureCapacity((int)(entry_count));

    if (type is null) {
      /* We can still provide a `TdList` even if there's not an entry type. We can simply use byte
         arrays as a temporary substitute. 
      */
      collection.ElementKind = typeof(byte[]);

      if (entry_count is not 0) {
        uint entry_size = (data_size / entry_count);
        for (uint i = 0; i < entry_count; i++)
          collection.Add(in_reader.ReadArray<byte>((int)(entry_size)));
      }

      result.Value = collection;

      /* We have done all we can without a proper entry type here, so we will return the entry. */
      return result;
    }

    collection.ElementKind = type;

    MethodInfo method = type.GetMethod("ReadIn", (BindingFlags.FlattenHierarchy | BindingFlags.Public |
      BindingFlags.Static), new Type[] { typeof(BinaryValueReader) })!;

    /* If we've found an entry type, we'll now want to apply it for each entry within the file. */
    for (uint i = 0; i < entry_count; i++)
      collection.Add(method.Invoke(null, new object[] { in_reader })!);

    /* Once we've filled the collection, we can safely assign it to the value of the `TdList`, and
       return it.
    */
    result.Value = collection;

    return result;
  }

  public static TdList ReadIn(string[] in_strings, JObject in_metadata) {
    /* We haven't personally encountered an embedded `FTD` yet, but we'll keep consistency and add
       support for them here. 
    */
    TdList result = new();

    var value_kind = in_metadata.Property("ValueKind")!.ToObject<ListValueKind>();

    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();
    result.Unknown2 = in_metadata.Property("Unknown2")!.ToObject<ushort>();

    var value = (JObject)(in_metadata.Property("Value")!.Value);
    if (value_kind is ListValueKind.EmbeddedFtd) {
      result.Value = TdFile.ReadIn(in_strings, value);
      return result;
    }

    /* If we are dealing with `DataEntries`, we will need to first retrieve the collection itself,
       then determine its `ElementKind`. 
    */
    var elements = (JArray)(value.Property("Elements")!.Value);
    
    DataCollection<object> collection = new();
    collection.ElementKind = Type.GetType(value.Property("ElementKind")!.ToObject<string>()!)!;

    /* We have handled all of the used tables that contain strings, but to be safe, we will ensure
       that `byte[]` is supported. 
    */
    if (collection.ElementKind == typeof(byte[])) {
      /* Here, we can rely on `Newtonsoft.Json` to handle conversion from `JArray` to `byte[]`. */
      foreach (JObject current in elements)
        collection.Add(current.ToObject<byte[]>()!);
    }
    else {
      /* Any other `ElementKind` will be dynamic, so we will need to use reflection to handle them
         appropriately. 
      */
      MethodInfo method = collection.ElementKind.GetMethod(
        "ReadIn", (BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static),
        new Type[] { typeof(string[]), typeof(JObject) })!;

      foreach (JObject current in elements) {
        collection.Add(
          method.Invoke(current, new object[] { in_strings, current })!);
      }
    }

    /* Finally, store the collection. We will need to make sure we implement `ReadIn` in all types
       for this to work. 
    */
    result.Value = collection;

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Kind");
    in_metadata_writer.WriteValue(DataKind.List);

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);

    in_metadata_writer.WritePropertyName(
      "Unknown2");
    in_metadata_writer.WriteValue(Unknown2);
    
    in_metadata_writer.WritePropertyName(
      "ValueKind");
    in_metadata_writer.WriteValue(Value!.Kind);

    /* `Value` is not necessarily guaranteed to be assigned since its assignment depends on a call
       to `ReadIn`, but we'll assume it's not null regardless.
    */
    in_metadata_writer.WritePropertyName(
      "Value");

    if (Value!.Kind is ListValueKind.EmbeddedFtd) {
      var table_data = (TdFile)(Value);
      table_data.WriteOut(in_strings_writer, in_metadata_writer);
    }
    else {
      var collection = (DataCollection<object>)(Value!);

      in_metadata_writer.WriteStartObject();

      /* We're writing a `DataCollection`, so we'll want to ensure that its `ElementKind` field is
         written. 
      */
      in_metadata_writer.WritePropertyName(
        "ElementKind");
      in_metadata_writer.WriteValue(collection.ElementKind.AssemblyQualifiedName);

      in_metadata_writer.WritePropertyName(
        "Elements");
      in_metadata_writer.WriteStartArray();
      foreach (object current_object in collection) {
        if (current_object is byte[]) {
          /* If it's an entry that we haven't defined a type for yet, then we can simply write the
             byte array as the value. 
          */
          in_metadata_writer.WriteStartArray();

          foreach (byte current_byte in (byte[])(current_object))
            in_metadata_writer.WriteValue(current_byte);

          in_metadata_writer.WriteEndArray();
        }
        else {
          /* Here, we will have to use reflection to call the appropriate `WriteOut` function, due
             to the contents of a `DataCollection` varying greatly.
          */
          MethodInfo method = current_object.GetType().GetMethod(
            "WriteOut", new Type[] { typeof(UniqueStreamWriter), typeof(JsonTextWriter) })!;
          method.Invoke(current_object, new object[] { in_strings_writer, in_metadata_writer });
        }
      }
      in_metadata_writer.WriteEndArray();
      in_metadata_writer.WriteEndObject();
    }

    /* Once we've written the value based on the `ListValueKind`, we then proceed with sealing. */
    in_metadata_writer.WriteEndObject();
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* Writing `TdList` is a bit more complicated, as the types we're dealing with are dynamic. We
       will need to offset responsibility for the majority of the writing to the individual types. 
    */
    in_writer.WriteUInt32(Unknown1);

    long data_size_offset = in_writer.Position;
    /* As we've done with writing `TdFile`, we will need to ensure that we write a temporary value
       for `DataSize`. 
    */
    in_writer.WriteUInt32(0x00000000);

    long header_post = 0;
    uint element_count = 0;

    /* TODO: Figure out what we write for element count when we're dealing with embedded `FTD`. */
    if (Value!.Kind is ListValueKind.EmbeddedFtd) {
      in_writer.WriteUInt32(0x00000000);
      element_count = 0;

      in_writer.WriteUInt16((ushort)(ListValueKind.EmbeddedFtd));
      in_writer.WriteUInt16(Unknown2);
      header_post = in_writer.Position;
      /* From now, we can simply pass execution over into `TdFile` to write the embedded `FTD`. */
      ((TdFile)(Value)).WriteOut(in_writer);
    }
    else {
      var collection = (DataCollection<object>)(Value);

      in_writer.WriteUInt32(0x00000000);
      element_count = (uint)(collection.Count);

      in_writer.WriteUInt16((ushort)(ListValueKind.DataEntries));
      in_writer.WriteUInt16(Unknown2);
      /* We'll then want to store the position after the header (effectively the collection base),
         so that we can subtract it from the last position before padding to obtain the data size. 
      */
      header_post = in_writer.Position;

      if (collection.ElementKind == typeof(byte[])) {
        /* For byte arrays, we can simply call the appropriate `BinaryValueWriter` method for each
           element. 
        */
        foreach (object current in collection)
          in_writer.WriteArray((byte[])(current));
      }
      else {
        MethodInfo method =
          collection.ElementKind.GetMethod("WriteOut", new Type[] { typeof(BinaryValueWriter) })!;
        foreach (object current in collection) {
          /* Here, we will have to use reflection to call the appropriate `WriteOut` function, due
             to the contents of a `DataCollection` varying greatly.
          */
          method.Invoke(current, new object[] { in_writer });
        }
      }

      /* Once we've written all of the data types, we've reached the last offset before alignment.
         We'll now want to store the data size, align, then hop over to `DataSize`'s position. 
      */
      var data_size = (uint)(in_writer.Position - header_post);
      in_writer.Align(16);
      using SeekToken token = in_writer.At(data_size_offset, SeekOrigin.Begin);

      /* Once we've done this, the file should be ready to use, albeit there is seemingly a couple
         files that have empty tables at the end. Look into this.
      */
      in_writer.WriteUInt32(data_size);

      /* There's some really weird behavior with `Seek` in that it overwrites eight bytes where we
         seek to, which overwrites the element count. We'll have to write it here. 
      */
      in_writer.WriteUInt32(element_count);
    }
  }
}
