using Amicitia.IO.Binary;
using AtlusScriptLibrary.Common.Text.Encodings;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TdStringsHelper;

Console.WriteLine("TdStringsHelper for Persona 5 Royal");
Console.WriteLine("By Not a Mitten");

if (args.Length is not 2 || args[0] is not ("E" or "I")) {
  Console.WriteLine(
"""

Argument Format: "./TdStringsHelper.exe" <E/I> <Path>

Options:
  E: Export FTD, etc. to a text and multiple JSON files.
  I: Recreate CTD, etc. using previously exported files.
""");
  return;
}

if (!Directory.Exists(args[1])) {
  Console.WriteLine("The provided directory does not exist.");
  return;
}

if (args[0] is "E")
  Export(args[1]);
else
  Import(args[1]);

void Export(string in_path) {
  /* We'll need to create our strings writer first, then begin to iterate over any table that's within
     the directory.
  */
  using UniqueStreamWriter strings_writer = new(Path.Combine(in_path, "Strings.txt"));

  foreach (string current in Directory.EnumerateFiles(
    in_path, "*.*TD", SearchOption.AllDirectories))
  {
    using BinaryValueReader reader = new(current, Endianness.Little,
      AtlusEncoding.Persona5RoyalEFIGS);
    TdFile table_data = TdFile.ReadIn(reader);

    Console.WriteLine($"Parsed `{Path.GetExtension(current)}`: {Path.GetFileName(current)}");

    using StreamWriter stream_writer = new(current + ".JSON");
    using JsonTextWriter metadata_writer = new(stream_writer);

    metadata_writer.Indentation = 2;
    metadata_writer.Formatting = Formatting.Indented;

    table_data.WriteOut(strings_writer, metadata_writer);
  }
}

void Import(string in_path) {
  /* As we've done within `TranslationHelper`, we'll first need to push every string into an array. */
  Console.WriteLine("Indexing strings...");

  string[] indexed = File.ReadAllLines(Path.Combine(in_path, "Strings.txt"));
  foreach (string current in Directory.EnumerateFiles(in_path,
    "*.*TD.JSON", SearchOption.AllDirectories))
  {
    string original_path = Path.ChangeExtension(current, null);

    /* We don't really need to open the original file here, as the `JSON` contains all the information
       we need to recreate a table.
    */
    JObject metadata = JObject.Parse(File.ReadAllText(current));

    Console.WriteLine(
      $"Parsed `{Path.GetExtension(original_path)}`: {Path.GetFileName(original_path)}`");

    using BinaryValueWriter writer = new(
      Path.ChangeExtension(original_path, $"MODIFIED{Path.GetExtension(original_path)}"),
      Endianness.Little,
      AtlusEncoding.Persona5RoyalEFIGS);

    /* We really do not need to do much with the `TdFile` other than writing it out, so we'll directly
       call `WriteOut` afterwards. 
    */
    TdFile.ReadIn(indexed, metadata).WriteOut(writer);
  }
}
