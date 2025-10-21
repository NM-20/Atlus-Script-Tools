/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper.ListEntries;

internal class cmmPC_PARAM_Help {
  private const int FIXED_LENGTH = 20;

  public string Level1 { get; private set; }
  public string Level2 { get; private set; }
  public string Level3 { get; private set; }
  public string Level4 { get; private set; }
  public string Level5 { get; private set; }

  private cmmPC_PARAM_Help() {
    Level1 = "";
    Level2 = "";
    Level3 = "";
    Level4 = "";
    Level5 = "";
  }

  public static cmmPC_PARAM_Help ReadIn(BinaryValueReader in_reader) {
    cmmPC_PARAM_Help result = new();

    result.Level1 = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);
    result.Level2 = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);
    result.Level3 = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);
    result.Level4 = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);
    result.Level5 = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);

    return result;
  }

  public static cmmPC_PARAM_Help ReadIn(string[] in_strings, JObject in_metadata) {
    cmmPC_PARAM_Help result = new();

    result.Level1 =
      in_strings[in_metadata.Property("Level1")!.ToObject<uint>()];
    result.Level2 =
      in_strings[in_metadata.Property("Level2")!.ToObject<uint>()];
    result.Level3 =
      in_strings[in_metadata.Property("Level3")!.ToObject<uint>()];
    result.Level4 =
      in_strings[in_metadata.Property("Level4")!.ToObject<uint>()];
    result.Level5 =
      in_strings[in_metadata.Property("Level5")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Level1");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Level1));
    in_metadata_writer.WritePropertyName(
      "Level2");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Level2));
    in_metadata_writer.WritePropertyName(
      "Level3");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Level3));
    in_metadata_writer.WritePropertyName(
      "Level4");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Level4));
    in_metadata_writer.WritePropertyName(
      "Level5");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Level5));

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteStringTruncated(Level1, FIXED_LENGTH);
    in_writer.WriteStringTruncated(Level2, FIXED_LENGTH);
    in_writer.WriteStringTruncated(Level3, FIXED_LENGTH);
    in_writer.WriteStringTruncated(Level4, FIXED_LENGTH);
    in_writer.WriteStringTruncated(Level5, FIXED_LENGTH);
  }
}
