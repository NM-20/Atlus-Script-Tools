using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.BinaryModel;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.BinaryModel.V1;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using TranslationHelper;

const int AVERAGE_NEWLINE_INSERT_POSITION = 33;

bool s_PreserveSpeakerNames = false;

Console.WriteLine("TranslationHelper for Persona 5 Royal");
Console.WriteLine("By Not a Mitten");

if (args.Length < 2 || args[0] is not ("E" or "I")) {
  Console.WriteLine(
"""

Argument Format: "./TranslationHelper.exe" <E/I> <Path>

Options:
  E: Export BF/BMDs to a text and multiple JSON files.
  I: Recompile to BF/BMDs via previously exported files.
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

void WriteToken(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer,
  IDialog in_dialog, IToken in_token)
{
  /* We'll need to conditionally handle each token and write them based on the kind. */
  in_metadata_writer.WriteStartObject();

  in_metadata_writer.WritePropertyName(
    "Kind");
  in_metadata_writer.WriteValue((int)(in_token.Kind));
  
  switch (in_token.Kind) {
  case TokenKind.CodePoint:
    Debug.Fail($"Found unhandled codepoint in {in_dialog.Name}!");
    break;
  
  case TokenKind.Function:
    var function_token = (FunctionToken)(in_token);
  
    in_metadata_writer.WritePropertyName(
      "FunctionIndex");
    in_metadata_writer.WriteValue(function_token.FunctionIndex);

    in_metadata_writer.WritePropertyName(
      "FunctionTableIndex");
    in_metadata_writer.WriteValue(function_token.FunctionTableIndex);
    
    in_metadata_writer.WritePropertyName(
      "Arguments");
    in_metadata_writer.WriteStartArray();
  
    foreach (ushort current in function_token.Arguments)
      in_metadata_writer.WriteValue(current);
  
    in_metadata_writer.WriteEndArray();
  
    break;
  
  case TokenKind.NewLine:
    Debug.Fail($"Found an existing newline in {in_dialog.Name}!");
    break;
  
  case TokenKind.String:
    var string_token = (StringToken)(in_token);
  
    in_metadata_writer.WritePropertyName(
      "StringIndex");
    in_metadata_writer.WriteValue(in_strings_writer.WriteLine(string_token.Value));
  
    break;
  }
  
  in_metadata_writer.WriteEndObject();
}

void HandleUnique(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer,
  IDialog in_dialog)
{ 
  if (in_dialog.Kind is DialogKind.Selection) {
    /* `DialogKind` can only be one other value, so we are able to use `else` here. */
    var selection = (SelectionDialog)(in_dialog);
    
    in_metadata_writer.WritePropertyName(
      "Pattern");
    in_metadata_writer.WriteValue((int)(selection.Pattern));

    /* `Options` is the same as `Dialogs`, so we don't have to handle it separately. */
  }
  else {
    /* `Message` technically has subtypes since `SpeakerKind` exists,
       so we'll need to conditionally handle it. 
    */
    var message = (MessageDialog)(in_dialog);
    if (message.Speaker is null)
      return;

    in_metadata_writer.WritePropertyName(
      "SpeakerKind");
    in_metadata_writer.WriteValue((int)(message.Speaker.Kind));

    if (message.Speaker.Kind is SpeakerKind.Named) {
      var named = (NamedSpeaker)(message.Speaker);

      /* `MSG_dummy`. */
      if (named.Name is null)
        return;

      in_metadata_writer.WritePropertyName("SpeakerName");
      in_metadata_writer.WriteStartArray();

      foreach (IToken current in named) {
        WriteToken(in_strings_writer, in_metadata_writer, in_dialog, current);
      }

      in_metadata_writer.WriteEndArray();
    }
    else {
      var variable = (VariableSpeaker)(message.Speaker);
      in_metadata_writer.WritePropertyName(
        "SpeakerIndex");
      in_metadata_writer.WriteValue(variable.Index);
    }
  }
}

void WriteDialogs(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer,
  IDialog in_dialog)
{
  /* To achieve something akin to the decompiler's format, we will be storing `Dialog`
     representations in an array. 
  */
  in_metadata_writer.WritePropertyName(
    "Dialogs");
  in_metadata_writer.WriteStartArray();

  foreach (TokenText current_text in in_dialog) {
    in_metadata_writer.WriteStartObject();
    
    in_metadata_writer.WritePropertyName(
      "Tokens");
    in_metadata_writer.WriteStartArray();

    foreach (IToken current_token in current_text) {
      WriteToken(in_strings_writer, in_metadata_writer, in_dialog,
        current_token);
    }

    /* Close off the `Tokens` array we created previously, then leave the function. */
    in_metadata_writer.WriteEndArray();
    in_metadata_writer.WriteEndObject();
  }

  /* We have successfully written the `Dialog`s, so we can now close off the array. */
  in_metadata_writer.WriteEndArray();
}

void ExportMessageScript(
  UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer,
  MessageScript in_script)
{
  in_metadata_writer.WriteStartArray();
  
  /* Once we've loaded the `MessageScript`, we will need to go over each `Dialog`, i.e
     `MSG`, `SEL`, etc.
  */
  foreach (IDialog current_dialog in in_script.Dialogs) {
    in_metadata_writer.WriteStartObject();
  
    in_metadata_writer.WritePropertyName(
      "Name");
    in_metadata_writer.WriteValue(current_dialog.Name);
  
    /* We need to be able to identify the type `Dialog` we're dealing with whenever we
       receive an import request.
    */
    in_metadata_writer.WritePropertyName(
      "Kind");
    in_metadata_writer.WriteValue((int)(current_dialog.Kind));
  
    /* We will need to do some conditional handling based on `current_dialog` in order
       to handle `SEL` vs `MSG`. 
    */
    HandleUnique(in_strings_writer, in_metadata_writer, current_dialog);
    WriteDialogs(in_strings_writer, in_metadata_writer, current_dialog);
  
    in_metadata_writer.WriteEndObject();
  }
  
  /* At this point, our metadata is finished, so ensure that it is sealed off here. */
  in_metadata_writer.WriteEndArray();
}

void Export(string in_path) {
  using UniqueStreamWriter strings_writer = new(Path.Combine(in_path, "Strings.txt"));

  foreach (string current_path in Directory.EnumerateFiles(
    in_path, "*.BF", SearchOption.AllDirectories))
  {
    FlowScript script = FlowScript.FromFile(current_path, AtlusEncoding.Persona5RoyalEFIGS);

    Console.WriteLine($"Parsed `FlowScript`: {Path.GetFileName(current_path)}");

    using StreamWriter stream_writer = new(
      new FileStream(Path.ChangeExtension(current_path, "BF.JSON"), FileMode.OpenOrCreate));
    using JsonTextWriter metadata_writer = new(stream_writer);

    metadata_writer.Formatting  = Formatting.Indented;
    metadata_writer.Indentation = 2;

    ExportMessageScript(strings_writer, metadata_writer, script.MessageScript);
  }

  foreach (string current_path in Directory.EnumerateFiles(
    in_path, "*.BMD", SearchOption.AllDirectories))
  {
    MessageScript script = MessageScript.FromFile(
      current_path, encoding: AtlusEncoding.Persona5RoyalEFIGS);

    Console.WriteLine($"Parsed `MessageScript`: {Path.GetFileName(current_path)}");

    using StreamWriter stream_writer = new(
      new FileStream(Path.ChangeExtension(current_path, "BMD.JSON"), FileMode.OpenOrCreate));
    using JsonTextWriter metadata_writer = new(stream_writer);

    metadata_writer.Formatting  = Formatting.Indented;
    metadata_writer.Indentation = 2;

    ExportMessageScript(strings_writer, metadata_writer, script);
  }

  Console.WriteLine($"Exported \"Strings.txt\" and .BF/BMD JSON files to \"{in_path}\"");
}

TokenText GetTokensFromJsonArray(string[] in_strings, JArray in_array) {
  TokenTextBuilder builder = new();

  foreach (JObject current_object in in_array) {
    var kind = (TokenKind)((int)(current_object.Property("Kind")!));

    switch (kind) {
    case TokenKind.Function:
      var function_table_index = current_object.Property("FunctionTableIndex")!.ToObject<int>();
      var arguments = (JArray)(current_object.Property("Arguments")!.Value);
      var function_index = current_object.Property("FunctionIndex")!.ToObject<int>();

      builder.AddFunction(function_table_index, function_index, arguments.ToObject<ushort[]>());
      break;

    case TokenKind.String:
      /* Rather than use the string directly, we must insert newlines in a new string,
         as we previously made them removed.
      */
      string mapped_string = in_strings[current_object.Property("StringIndex")!.ToObject<int>()];
      StringBuilder substring = new();

      /* We don't want to insert newlines in the middle of words, so instead split the
         string by spaces, then incrementally insert each word back until a particular
         threshold is reached.
      */
      foreach (string current in mapped_string.Split(' ')) {
        substring.Append(current);

        /* No newline is necessary in this case, since we haven't the average position
           for them. 
        */
        if (substring.Length < AVERAGE_NEWLINE_INSERT_POSITION) {
          substring.Append(' ');
        }
        else {
          /* Reset the string to empty so that we are not adding unwanted newlines. */
          builder.AddString(substring.ToString());
          substring.Clear();
          builder.AddNewLine();
        }
      }

      /* Once our iteration is done, we can push the final string into the builder. */
      if (substring.Length is not 0)
        builder.AddString(substring.ToString());

      break;

    default:
      Debug.Fail($"Unexpected `TokenKind` while parsing tokens in JSON array!");
      break;
    }
  }

  /* Now, we can build the `TokenText` and return it. This should never really have an
     issue.
  */
  return builder.Build();
}

void FillDialogSpeaker(
  string[] in_strings, JObject in_element, MessageDialog in_dialog)
{
  /* Not all messages are required to contain a `SpeakerKind`, so we'll have to ensure
     one exists. 
  */
  if (!in_element.ContainsKey("SpeakerKind"))
    return;

  var speaker_kind = in_element.Property("SpeakerKind")!.ToObject<SpeakerKind>();

  if (speaker_kind is SpeakerKind.Named) {
    if (!in_element.ContainsKey("SpeakerName"))
      return;

    in_dialog.Speaker = new NamedSpeaker(GetTokensFromJsonArray(in_strings,
      (JArray)(in_element.Property("SpeakerName")!.Value)));
  }
  else {
    /* There's only one other type, so we can go ahead and choose `else` over here. */
    in_dialog.Speaker =
      new VariableSpeaker(in_element.Property("SpeakerIndex")!.ToObject<int>());
  }
}

void InsertDialogLines(
  string[] in_strings, JObject in_element, IDialog in_dialog)
{
  foreach (JObject current in (JArray)(in_element.Property("Dialogs")!.Value)) {
    in_dialog.Lines.Add(GetTokensFromJsonArray(
      in_strings, (JArray)(current.Property("Tokens")!.Value)));
  }
}

void ImportMessageScript(
  string[] in_strings, JArray in_metadata, MessageScriptBinaryBuilder in_builder)
{
  /* Every created JSON file has an array of `Dialog`s, so we will need to iterate. */
  foreach (JObject current in in_metadata) {
    var dialog_kind = current.Property("Kind")!.ToObject<DialogKind>();

    switch (dialog_kind) {
    case DialogKind.Message:
      MessageDialog message = new(current.Property("Name")!.ToObject<string>());

      FillDialogSpeaker(in_strings, current, message);
      InsertDialogLines(in_strings, current, message);

      in_builder.AddDialog(message);
      break;

    case DialogKind.Selection:
      SelectionDialog selection =
        new(current.Property("Name")!.ToObject<string>());

      /* Pretty straightforward here: fill the pattern, then fill the dialog lines. */
      InsertDialogLines(in_strings, current, selection);
      selection.Pattern = current.Property("Pattern")!.ToObject<SelectionDialogPattern>();

      in_builder.AddDialog(selection);
      break;
    }
  }
}

void Import(string in_path) {
  /* Earlier, we adjusted the extensions of the JSON files based on the original file.
     We can take advantage of that now to differentiate between the two when we get to
     recompiling.
  */
  Console.WriteLine("Indexing strings...");

  string[] indexed = File.ReadAllLines(Path.Combine(in_path, "Strings.txt"));
  foreach (string current in Directory.EnumerateFiles(
    in_path, "*.BF.JSON", SearchOption.AllDirectories))
  {
    string compiled_flow_path = Path.ChangeExtension(current, null);

    FlowScriptBinary binary = FlowScriptBinary.FromFile(
      compiled_flow_path);

    Console.WriteLine($"Parsed `FlowScript`: {Path.GetFileName(current)}");

    /* Next, we will need to read in the JSON itself. Once that is finished, execution
       must go over to `ImportMessageScript`.
    */
    JArray metadata = JArray.Parse(File.ReadAllText(current));
    MessageScriptBinaryBuilder message_builder =
      new(binary.MessageScriptSection.FormatVersion);

    ImportMessageScript(indexed, metadata, message_builder);

    /* Once we have built the message script, we can build the `FlowScript` binary. */
    FlowScriptBinaryBuilder flow_builder = new(binary.FormatVersion);

    flow_builder.SetMessageScriptSection(message_builder.Build());
    
    /* The `MessageScript` section is the only part we're changing; everything else is
       taken from the original file.
    */
    flow_builder.SetJumpLabelSection(binary.JumpLabelSection);
    flow_builder.SetTextSection(binary.TextSection);
    flow_builder.SetStringSection(binary.StringSection);
    flow_builder.SetProcedureLabelSection(binary.ProcedureLabelSection);

    /* Finally, write out the modified `BF`. We preserve the original for the purposes
       of data validation. 
    */
    flow_builder.Build().ToFile(Path.ChangeExtension(compiled_flow_path, "MODIFIED.BF"));
  }

  /* Now, handle `BMD` files. These are much simpler, as we don't have to work with an
     embedded message. 
  */
  foreach (string current in Directory.EnumerateFiles(
    in_path, "*.BMD.JSON", SearchOption.AllDirectories))
  {
    string compiled_bmd_path = Path.ChangeExtension(current, null);

    MessageScriptBinary binary = MessageScriptBinary.FromFile(
      compiled_bmd_path);

    Console.WriteLine($"Parsed `MessageScript`: {Path.GetFileName(current)}");

    /* Next, we will need to read in the JSON itself. Once that is finished, execution
       must go over to `ImportMessageScript`.
    */
    JArray metadata = JArray.Parse(File.ReadAllText(current));
    MessageScriptBinaryBuilder message_builder =
      new(binary.FormatVersion);

    ImportMessageScript(indexed, metadata, message_builder);

    /* Finally, write out the modified `BMD`. We preserve the original for purposes of
       data validation. 
    */
    message_builder.Build().ToFile(Path.ChangeExtension(compiled_bmd_path, "MODIFIED.BMD"));
  }

  Console.WriteLine($"Saved imported .BF/.BMD JSON files to directories within \"{in_path}\"");
}
