using Amicitia.IO.Binary;

namespace TdStringsHelper;

internal static class BinaryValueWriterExtensions {
  public static void WriteStringTruncated(this BinaryValueWriter extended, string in_string, int in_fixed_length) {
    /* We'll commonly need to write strings and determine if they'll be truncated, so we implemented this method. */
    if (in_string.Length <= (in_fixed_length - "\x00".Length)) {
      extended.WriteString(StringBinaryFormat.FixedLength, in_string, in_fixed_length);
    }
    else {
      /* The behavior of `WriteStringFixedLength` does not seem it'd work with string truncation, so we've done this
         ourselves to be safe. 
      */
      extended.WriteString(
        StringBinaryFormat.FixedLength, in_string.Substring(0, (in_fixed_length - "\x00".Length)), in_fixed_length);

      Console.WriteLine(
        $"\"{in_string}\" exceeds the possible fixed length \"{in_fixed_length}\"! It'll be truncated as a result.");
    }
  }
}
