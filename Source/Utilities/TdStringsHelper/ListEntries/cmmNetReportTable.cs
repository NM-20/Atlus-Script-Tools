/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace TdStringsHelper.ListEntries;

internal class cmmNetReportTable {
  /* TODO: Properly verify this; things could get pretty ugly if it turns out there is additional
     data. 
  */
  private const int FIXED_LENGTH = 48;

  public uint   Unknown1 { get; private set; }
  public string Report   { get; private set; }

  private cmmNetReportTable() {
    Report = "";
  }

  public static cmmNetReportTable ReadIn(BinaryValueReader in_reader) {
    cmmNetReportTable result = new();

    result.Unknown1 = in_reader.ReadUInt32();
    result.Report = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);

    return result;
  }

  public static cmmNetReportTable ReadIn(string[] in_strings, JObject in_metadata) {
    cmmNetReportTable result = new();

    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();
    result.Report = in_strings[in_metadata.Property("Report")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);

    in_metadata_writer.WritePropertyName(
      "Report");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Report));

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteUInt32(Unknown1);
    in_writer.WriteStringTruncated(Report, FIXED_LENGTH);
  }
}
