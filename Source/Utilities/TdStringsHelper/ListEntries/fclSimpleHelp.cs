/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace TdStringsHelper.ListEntries;

internal class fclSimpleHelp {
  /* TODO: Properly verify this; things could get pretty ugly if it turns out there is additional
     data. 
  */
  private const int FIXED_LENGTH = 64;

  public string Text { get; private set; }

  private fclSimpleHelp() {
    Text = "";
  }

  public static fclSimpleHelp ReadIn(BinaryValueReader in_reader) {
    fclSimpleHelp result = new();

    result.Text = in_reader.ReadString(
      StringBinaryFormat.FixedLength, FIXED_LENGTH);

    return result;
  }

  public static fclSimpleHelp ReadIn(string[] in_strings, JObject in_metadata) {
    fclSimpleHelp result = new();

    result.Text = in_strings[in_metadata.Property("Text")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Text");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Text));

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* We'll be preferring our `WriteStringTruncated` extension to communicate with the user when
       a string has been truncated. 
    */
    in_writer.WriteStringTruncated(Text, FIXED_LENGTH);
  }
}
