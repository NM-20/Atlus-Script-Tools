/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace TdStringsHelper.ListEntries;

internal class fclHelpTable_COMBINE_HELP {
  /* TODO: Properly verify this; things could get pretty ugly if it turns out there is additional
     data. 
  */
  private const int FIXED_LENGTH = 128;

  public uint   Unknown1 { get; private set; }
  public uint   Unknown2 { get; private set; }
  public string Text     { get; private set; }
  public ushort Unknown3 { get; private set; }
  public ushort Unknown4 { get; private set; }

  private fclHelpTable_COMBINE_HELP() {
    Text = "";
  }

  public static fclHelpTable_COMBINE_HELP ReadIn(BinaryValueReader in_reader) {
    fclHelpTable_COMBINE_HELP result = new();

    result.Unknown1 = in_reader.ReadUInt32();
    result.Unknown2 = in_reader.ReadUInt32();
    result.Text = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);
    result.Unknown3 = in_reader.ReadUInt16();
    result.Unknown4 = in_reader.ReadUInt16();

    return result;
  }

  public static fclHelpTable_COMBINE_HELP ReadIn(string[] in_strings, JObject in_metadata) {
    fclHelpTable_COMBINE_HELP result = new();

    result.Text = in_strings[in_metadata.Property("Text")!.ToObject<uint>()];
    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();
    result.Unknown2 = in_metadata.Property("Unknown2")!.ToObject<uint>();
    result.Unknown3 = in_metadata.Property("Unknown3")!.ToObject<ushort>();
    result.Unknown4 = in_metadata.Property("Unknown4")!.ToObject<ushort>();

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);
    in_metadata_writer.WritePropertyName(
      "Unknown2");
    in_metadata_writer.WriteValue(Unknown2);

    in_metadata_writer.WritePropertyName(
      "Text");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Text));

    in_metadata_writer.WritePropertyName(
      "Unknown3");
    in_metadata_writer.WriteValue(Unknown3);

    in_metadata_writer.WritePropertyName(
      "Unknown4");
    in_metadata_writer.WriteValue(Unknown4);

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteUInt32(Unknown1);
    in_writer.WriteUInt32(Unknown2);
    in_writer.WriteStringTruncated(
      Text, FIXED_LENGTH);
    in_writer.WriteUInt16(Unknown3);
    in_writer.WriteUInt16(Unknown4);
  }
}
