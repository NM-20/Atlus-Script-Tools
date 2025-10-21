/* References https://github.com/Secre-C/010-Editor-Templates/blob/master/templates/Persona_5R_FTD.bt. */

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper.ListEntries;

internal class FLDLMAPSTATION {
  private const int COMMON_TEXT_LENGTH = 32;
  private const int DESCRIPTION_LENGTH = 48;

  public string StationName  { get; private set; }
  public uint   Flags        { get; private set; }
  public uint   Unknown1     { get; private set; }
  public ushort BfProcedure  { get; private set; }
  public ushort Unknown2     { get; private set; }
  public uint   Unknown3     { get; private set; }
  public string Description1 { get; private set; }
  public string Description2 { get; private set; }
  public string AttracTag    { get; private set; }
  public string AttracDesc1  { get; private set; }
  public string AttracDesc2  { get; private set; }
  public string AttracDesc3  { get; private set; }
  public string AttracDesc4  { get; private set; }

  private FLDLMAPSTATION() {
    StationName  = "";
    Description1 = "";
    Description2 = "";
    AttracTag    = "";
    AttracDesc1  = "";
    AttracDesc2  = "";
    AttracDesc3  = "";
    AttracDesc4  = "";
  }

  public static FLDLMAPSTATION ReadIn(BinaryValueReader in_reader) {
    FLDLMAPSTATION result = new();

    /* We're using the P5R EFIGS encoding, so we shouldn't be dealing with multiple broken characters. */
    result.StationName  = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    result.Flags        = in_reader.ReadUInt32();
    result.Unknown1     = in_reader.ReadUInt32();
    result.BfProcedure  = in_reader.ReadUInt16();
    result.Unknown2     = in_reader.ReadUInt16();
    result.Unknown3     = in_reader.ReadUInt32();
    result.Description1 = in_reader.ReadString(StringBinaryFormat.FixedLength, DESCRIPTION_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.Description2 = in_reader.ReadString(StringBinaryFormat.FixedLength, DESCRIPTION_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.AttracTag    = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.AttracDesc1  = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.AttracDesc2  = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.AttracDesc3  = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    in_reader.ReadArray<byte>(16);
    result.AttracDesc4  = in_reader.ReadString(StringBinaryFormat.FixedLength, COMMON_TEXT_LENGTH);
    in_reader.ReadArray<byte>(16);

    /* The `FLDLMAPSTATION` will be added to the `DataCollection` automatically, so we don't have to deal
       with anything else. 
    */
    return result;
  }

  public static FLDLMAPSTATION ReadIn(string[] in_strings, JObject in_metadata) {
    FLDLMAPSTATION result = new();

    result.StationName = in_strings[in_metadata.Property("StationName")!.ToObject<uint>()];
    result.Flags = in_metadata.Property("Flags")!.ToObject<uint>();
    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();
    result.BfProcedure = in_metadata.Property("BfProcedure")!.ToObject<ushort>();

    result.Unknown2 = in_metadata.Property("Unknown2")!.ToObject<ushort>();
    result.Unknown3 = in_metadata.Property("Unknown3")!.ToObject<uint>();

    result.Description1 = in_strings[in_metadata.Property("Description1")!.ToObject<uint>()];
    result.Description2 = in_strings[in_metadata.Property("Description2")!.ToObject<uint>()];

    result.AttracTag = in_strings[in_metadata.Property("AttracTag")!.ToObject<uint>()];

    result.AttracDesc1 = in_strings[in_metadata.Property("AttracDesc1")!.ToObject<uint>()];
    result.AttracDesc2 = in_strings[in_metadata.Property("AttracDesc2")!.ToObject<uint>()];
    result.AttracDesc3 = in_strings[in_metadata.Property("AttracDesc3")!.ToObject<uint>()];
    result.AttracDesc4 = in_strings[in_metadata.Property("AttracDesc4")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "StationName");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(StationName));

    in_metadata_writer.WritePropertyName(
      "Flags");
    in_metadata_writer.WriteValue(Flags);

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);

    in_metadata_writer.WritePropertyName(
      "BfProcedure");
    in_metadata_writer.WriteValue(BfProcedure);

    in_metadata_writer.WritePropertyName(
      "Unknown2");
    in_metadata_writer.WriteValue(Unknown2);

    in_metadata_writer.WritePropertyName(
      "Unknown3");
    in_metadata_writer.WriteValue(Unknown3);

    in_metadata_writer.WritePropertyName(
      "Description1");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Description1));

    in_metadata_writer.WritePropertyName(
      "Description2");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Description2));

    in_metadata_writer.WritePropertyName(
      "AttracTag");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(AttracTag));

    in_metadata_writer.WritePropertyName(
      "AttracDesc1");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(AttracDesc1));

    in_metadata_writer.WritePropertyName(
      "AttracDesc2");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(AttracDesc2));

    in_metadata_writer.WritePropertyName(
      "AttracDesc3");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(AttracDesc3));

    in_metadata_writer.WritePropertyName(
      "AttracDesc4");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(AttracDesc4));

    in_metadata_writer.WriteEndObject();

    /* We've made sure the property names areas close to the original classes for when we parse later. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteStringTruncated(StationName, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt32(Flags);
    in_writer.WriteUInt32(Unknown1);
    in_writer.WriteUInt16(BfProcedure);
    in_writer.WriteUInt16(Unknown2);
    in_writer.WriteUInt32(Unknown3);

    in_writer.WriteStringTruncated(Description1, DESCRIPTION_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteStringTruncated(Description2, DESCRIPTION_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);

    in_writer.WriteStringTruncated(AttracTag, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);

    in_writer.WriteStringTruncated(AttracDesc1, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteStringTruncated(AttracDesc2, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteStringTruncated(AttracDesc3, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteStringTruncated(AttracDesc4, COMMON_TEXT_LENGTH);
    in_writer.WriteUInt64(0x0000000000000000);
    in_writer.WriteUInt64(0x0000000000000000);

    /* TODO: Could we theoretically abuse the padding to create larger-length strings, since their method
       reads the string until a null terminator? 
    */
  }
}
