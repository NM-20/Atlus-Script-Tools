/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper.ListEntries;

internal class cmmPC_PARAM_Name {
  private const int FIXED_LENGTH = 20;

  public string Name { get; private set; }

  private cmmPC_PARAM_Name() {
    Name = "";
  }

  public static cmmPC_PARAM_Name ReadIn(BinaryValueReader in_reader) {
    cmmPC_PARAM_Name result = new();

    result.Name = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);

    return result;
  }

  public static cmmPC_PARAM_Name ReadIn(string[] in_strings, JObject in_metadata) {
    cmmPC_PARAM_Name result = new();

    result.Name = in_strings[in_metadata.Property("Name")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Name");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Name));

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteStringTruncated(Name, FIXED_LENGTH);
  }
}
