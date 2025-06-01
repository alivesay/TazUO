using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace ClassicUO.Game.Managers
{
    using System.Text.Json.Serialization;

    [JsonSerializable(typeof(Dictionary<string, ushort>))]
    public partial class TileMarkerJsonContext : JsonSerializerContext
    {
    }
    public class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<string, ushort> markedTiles = new Dictionary<string, ushort>();

        private TileMarkerManager() { Load(); }

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "TileMarkers.json");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            if (markedTiles.TryGetValue(FormatLocKey(x, y, map), out hue))
            {
                markedTiles.Add(FormatLocKey(x, y, map), hue);
            }
            else
            {
                markedTiles.Add(FormatLocKey(x, y, map), hue);
            }
        }

        public void RemoveTile(int x, int y, int map)
        {
            if (markedTiles.ContainsKey(FormatLocKey(x, y, map)))
                markedTiles.Remove(FormatLocKey(x, y, map));
        }

        public bool IsTileMarked(int x, int y, int map, out ushort hue)
        {
            if (markedTiles.TryGetValue(FormatLocKey(x, y, map), out hue)) return true;
            return false;
        }

        private string FormatLocKey(int x, int y, int map)
        {
            return $"{x}.{y}.{map}";
        }

        public void Save()
        {
            try
            {
                var contents = JsonSerializer.Serialize(markedTiles, TileMarkerJsonContext.Default.DictionaryStringUInt16);
                File.WriteAllText(savePath, contents);
            }
            catch { Console.WriteLine("Failed to save marked tile data."); }
        }

        private void Load()
        {
            if (File.Exists(savePath))
                try
                {
                    markedTiles = JsonSerializer.Deserialize(File.ReadAllText(savePath), TileMarkerJsonContext.Default.DictionaryStringUInt16);
                }
                catch { }
        }
    }
}
