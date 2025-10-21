using Amicitia.IO.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TdStringsHelper.ListEntries;

internal class FLDWHOLEMAPTABLE {
  private const int MAP_LOCATION_COUNT = 20;

  public struct MapLocation {
    internal const int FIXED_LENGTH = 40;

    /* The string itself is likely 40 bytes in length. */
    public string Name     { get; internal set; }
    /* These two unknowns possibly relate to which visual
       is displayed?
    */
    public ushort Unknown1 { get; internal set; }
    public ushort Unknown2 { get; internal set; }
    public ushort Unknown3 { get; internal set; }
    public ushort Unknown4 { get; internal set; }
    public ushort Unknown5 { get; internal set; }
    /* This is accessed via 0x1411DC450 when a map option
       is chosen.
    */
    public ushort Unknown6 { get; internal set; }
    public uint   Unknown7 { get; internal set; }
  }

  public MapLocation[] Locations { get; private set; }
  public uint Unknown1           { get; private set; }

  protected FLDWHOLEMAPTABLE() {
    Locations = new MapLocation[MAP_LOCATION_COUNT];
  }

  public static FLDWHOLEMAPTABLE ReadIn(BinaryValueReader in_reader) {
    FLDWHOLEMAPTABLE result = new();
    for (int i = 0; i < MAP_LOCATION_COUNT; i++) {
      MapLocation current = new();
      current.Name = in_reader.ReadString(StringBinaryFormat.FixedLength, MapLocation.FIXED_LENGTH);
      current.Unknown1 = in_reader.ReadUInt16();
      current.Unknown2 = in_reader.ReadUInt16();
      current.Unknown3 = in_reader.ReadUInt16();
      current.Unknown4 = in_reader.ReadUInt16();
      current.Unknown5 = in_reader.ReadUInt16();
      current.Unknown6 = in_reader.ReadUInt16();
      current.Unknown7 = in_reader.ReadUInt32();

      /* At this point, we've read 56 bytes, which is the size of a `MapLocation`. We can now add it
         to our array. 
      */
      result.Locations[i] = current;
    }

    /* TODO: We should try to figure out what this last value is; I could not seem to get a hit from
       access breakpoints. 
    */
    result.Unknown1 = in_reader.ReadUInt32();

    /* Once we've indexed all `MapLocation`s and filled `Unknown1`, we are able to safely return. */
    return result;
  }

  public static FLDWHOLEMAPTABLE ReadIn(string[] in_strings, JObject in_metadata) {
    FLDWHOLEMAPTABLE result = new();

    /* We will first need to retrieve the `MapLocations` array before we can iterate. After this, we
       will need to assign `Unknown1`. 
    */
    var array = (JArray)(in_metadata.Property("Locations")!.Value);
    for (int i = 0; i < MAP_LOCATION_COUNT; i++) {
      var current = (JObject)(array[i]);
      MapLocation location = new();

      location.Name = in_strings[current.Property("Name")!.ToObject<uint>()];
      location.Unknown1 = current.Property("Unknown1")!.ToObject<ushort>();
      location.Unknown2 = current.Property("Unknown2")!.ToObject<ushort>();
      location.Unknown3 = current.Property("Unknown3")!.ToObject<ushort>();
      location.Unknown4 = current.Property("Unknown4")!.ToObject<ushort>();
      location.Unknown5 = current.Property("Unknown5")!.ToObject<ushort>();
      location.Unknown6 = current.Property("Unknown6")!.ToObject<ushort>();
      location.Unknown7 = current.Property("Unknown7")!.ToObject<uint>();

      /* The `MapLocation` has loaded at this point, so we can index the instance into our array. */
      result.Locations[i] = location;
    }

    result.Unknown1 = in_metadata.Property("Unknown1")!.ToObject<uint>();

    /* Once we've indexed all `MapLocation`s and filled `Unknown1`, we are able to safely return. */
    return result;
  }

  public void WriteOut(UniqueStreamWriter in_strings_writer, JsonTextWriter in_metadata_writer) {
    in_metadata_writer.WriteStartObject();

    in_metadata_writer.WritePropertyName(
      "Unknown1");
    in_metadata_writer.WriteValue(Unknown1);

    in_metadata_writer.WritePropertyName(
      "Locations");
    in_metadata_writer.WriteStartArray();

    for (int i = 0; i < MAP_LOCATION_COUNT; i++) {
      MapLocation current = Locations[i];

      in_metadata_writer.WriteStartObject();

      in_metadata_writer.WritePropertyName(
        "Name");
      in_metadata_writer.WriteValue(in_strings_writer.WriteLine(current.Name));

      in_metadata_writer.WritePropertyName(
        "Unknown1");
      in_metadata_writer.WriteValue(current.Unknown1);
      in_metadata_writer.WritePropertyName(
        "Unknown2");
      in_metadata_writer.WriteValue(current.Unknown2);
      in_metadata_writer.WritePropertyName(
        "Unknown3");
      in_metadata_writer.WriteValue(current.Unknown3);
      in_metadata_writer.WritePropertyName(
        "Unknown4");
      in_metadata_writer.WriteValue(current.Unknown4);
      in_metadata_writer.WritePropertyName(
        "Unknown5");
      in_metadata_writer.WriteValue(current.Unknown5);
      in_metadata_writer.WritePropertyName(
        "Unknown6");
      in_metadata_writer.WriteValue(current.Unknown6);
      in_metadata_writer.WritePropertyName(
        "Unknown7");
      in_metadata_writer.WriteValue(current.Unknown7);

      in_metadata_writer.WriteEndObject();
    }

    /* Finally, ensure that array and object we opened up earlier within this function is sealed. */
    in_metadata_writer.WriteEndArray();
    in_metadata_writer.WriteEndObject();
  }

  public void WriteOut(BinaryValueWriter in_writer) {
    /* This is the first case where we genuinely have a type requiring an array. We'll first need to
       write it, then the final unknown. 
    */
    for (int i = 0; i < MAP_LOCATION_COUNT; i++) {
      MapLocation current = Locations[i];
      in_writer.WriteStringTruncated(current.Name, MapLocation.FIXED_LENGTH);
      in_writer.WriteUInt16(current.Unknown1);
      in_writer.WriteUInt16(current.Unknown2);
      in_writer.WriteUInt16(current.Unknown3);
      in_writer.WriteUInt16(current.Unknown4);
      in_writer.WriteUInt16(current.Unknown5);
      in_writer.WriteUInt16(current.Unknown6);
      in_writer.WriteUInt32(current.Unknown7);
    }

    /* Finally, all we need to do is write the final unknown. From there, the binary is finished. */
    in_writer.WriteUInt32(Unknown1);
  }
}
