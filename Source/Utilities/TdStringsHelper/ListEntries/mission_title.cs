/* TODO: Make sure we take advantage of the `FixedSizeString` attribute and truncate strings when
   they exceed the length. 
*/

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper.ListEntries;

internal class mission_title {
  /* The mission tables contain the only genuine case of variable-length strings, as it relies on
     `TdList`'s element count, which is an unsigned integer. 
  */
  public string Title { get; private set; }

  protected mission_title() {
    Title = "";
  }

  public static mission_title ReadIn(BinaryValueReader in_reader) {
    mission_title result = new();

    result.Title = in_reader.ReadString(StringBinaryFormat.NullTerminated);

    return result;
  }

  public static mission_title ReadIn(string[] in_strings, JObject in_metadata) {
    mission_title result = new();

    result.Title = in_strings[in_metadata.Property("Title")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Title");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Title));

    in_metadata_writer.WriteEndObject();

    /* We've kept the property names close to the original classes to make the parsing easier. */
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* Since this relies on `TdList`, the result will automatically have been aligned to 16 bytes
       for us.
    */
    in_writer.WriteString(StringBinaryFormat.NullTerminated, Title);
  }
}