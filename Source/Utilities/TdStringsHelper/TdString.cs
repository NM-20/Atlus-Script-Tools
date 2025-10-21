/* References https://github.com/Secre-C/010-Editor-Templates/blob/master/templates/Persona_5R_FTD.bt. */

using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper;

internal class TdString : ITdData {
  /* If we know that a particular instance is a `TdList`, we don't need this field, so hide it. */
  DataKind ITdData.Kind => DataKind.String;

  public byte   Unknown1 { get; private set; }
  public byte   Unknown2 { get; private set; }
  public byte   Unknown3 { get; private set; }
  public string Value    { get; private set; }

  private TdString() {
    Value = "";
  }

  public static TdString ReadIn(BinaryValueReader in_reader) {
    TdString result = new();
    
    byte length     = in_reader.ReadByte();
    result.Unknown1 = in_reader.ReadByte();
    result.Unknown2 = in_reader.ReadByte();
    result.Unknown3 = in_reader.ReadByte();

    /* We'll need to keep in mind when we write these back in that the buffer should be aligned to
       16 bytes.
    */
    result.Value = in_reader.ReadString(StringBinaryFormat.FixedLength, length);

    return result;
  }

  public static TdString ReadIn(string[] in_strings, JObject in_metadata) {
    TdString result = new();

    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<byte>();
    result.Unknown2 = in_metadata.Property("Unknown2")!.ToObject<byte>();
    result.Unknown3 = in_metadata.Property("Unknown3")!.ToObject<byte>();

    result.Value = in_strings[in_metadata.Property("StringIndex")!.ToObject<uint>()];

    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Kind");
    in_metadata_writer.WriteValue(DataKind.String);

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);

    in_metadata_writer.WritePropertyName(
      "Unknown2");
    in_metadata_writer.WriteValue(Unknown2);

    in_metadata_writer.WritePropertyName(
      "Unknown3");
    in_metadata_writer.WriteValue(Unknown3);

    in_metadata_writer.WritePropertyName(
      "StringIndex");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(Value));

    in_metadata_writer.WriteEndObject();
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* While this type is "variable-length", the length itself is a byte, so we realistically only
       can write strings that are 255 characters long. We'll need to truncate. 
    */
    if (Value.Length <= (byte.MaxValue - "\x00".Length)) {
      in_writer.WriteByte((byte)(Value.Length));
      in_writer.WriteByte(Unknown1);
      in_writer.WriteByte(Unknown2);
      in_writer.WriteByte(Unknown3);

      /* Otherwise, the strings can be written without truncation. We will be writing every string
         with the length that they exactly need still, however. 
      */
      in_writer.WriteString(StringBinaryFormat.NullTerminated, Value);

      /* The `TdString`s seem to be get 16 byte alignment, so we will be replicating this behavior
         as well.
      */
      in_writer.Align(16);
    }
    else {
      in_writer.WriteByte(byte.MaxValue);
      in_writer.WriteByte(Unknown1);
      in_writer.WriteByte(Unknown2);
      in_writer.WriteByte(Unknown3);
      in_writer.WriteString(
        StringBinaryFormat.NullTerminated, Value.Substring(0, (byte.MaxValue - "\x00".Length)));

      in_writer.Align(16);

      Console.WriteLine(
        $"\"{Value}\" exceeds maximum `TdString` length of 255 characters! It will be truncated.");
    }
  }
}
