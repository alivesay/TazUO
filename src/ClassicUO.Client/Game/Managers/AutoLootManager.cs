using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(AutoLootManager.AutoLootConfigEntry))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootConfigEntry>))]
    public partial class AutoLootJsonContext : JsonSerializerContext
    {
    }

    public class AutoLootManager
    {
        public static AutoLootManager Instance { get; private set; } = new AutoLootManager();
        public bool IsLoaded { get { return loaded; } }
        public List<AutoLootConfigEntry> AutoLootList { get => autoLootItems; set => autoLootItems = value; }

        private HashSet<uint> quickContainsLookup = new HashSet<uint>();
        private static Queue<uint> lootItems = new Queue<uint>();
        private List<AutoLootConfigEntry> autoLootItems = new List<AutoLootConfigEntry>();
        private bool loaded = false;
        private readonly string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");
        private long nextLootTime = Time.Ticks;
        private ProgressBarGump progressBarGump;
        private int currentLootTotalCount = 0;
        private bool IsEnabled { get { return ProfileManager.CurrentProfile.EnableAutoLoot; } }

        private World World;

        private AutoLootManager()
        {
            World = Client.Game.UO.World;
        }


        public bool IsBeingLooted(uint serial)
        {
            return quickContainsLookup.Contains(serial);
        }

        public void LootItem(uint serial)
        {
            LootItem(World.Items.Get(serial));
        }
        public void LootItem(Item i)
        {
            if (i == null || quickContainsLookup.Contains(i.Serial)) return;

            lootItems.Enqueue(i);
            quickContainsLookup.Add(i.Serial);
            currentLootTotalCount++;
        }

        /// <summary>
        /// Check an item against the loot list, if it needs to be auto looted it will be.
        /// I reccomend running this method in a seperate thread if it's a lot of items.
        /// </summary>
        private void CheckAndLoot(Item i)
        {
            if (!loaded || i == null || quickContainsLookup.Contains(i.Serial)) return;

            if (IsOnLootList(i))
            {
                LootItem(i);
            }
        }

        /// <summary>
        /// Check if an item is on the auto loot list.
        /// </summary>
        /// <param name="i">The item to check the loot list against</param>
        /// <returns></returns>
        private bool IsOnLootList(Item i)
        {
            if (!loaded) return false;

            foreach (var entry in autoLootItems)
            {
                if (entry.Match(i))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Add an entry for auto looting to match against when opening corpses.
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="hue"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AutoLootConfigEntry AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            AutoLootConfigEntry item = new AutoLootConfigEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry entry in autoLootItems)
            {
                if (entry.Equals(item))
                {
                    return entry;
                }
            }

            autoLootItems.Add(item);

            return item;
        }

        /// <summary>
        /// Search through a corpse and check items that need to be looted.
        /// Only call this after checking that autoloot IsEnabled
        /// </summary>
        /// <param name="corpse"></param>
        private void HandleCorpse(Item corpse)
        {
            if (corpse != null && corpse.IsCorpse && corpse.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange && (!corpse.IsHumanCorpse || ProfileManager.CurrentProfile.AutoLootHumanCorpses))
            {
                for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                {
                    CheckAndLoot((Item)i);
                }
            }
        }

        public void TryRemoveAutoLootEntry(string UID)
        {
            int removeAt = -1;

            for (int i = 0; i < autoLootItems.Count; i++)
            {
                if (autoLootItems[i].UID == UID)
                {
                    removeAt = i;
                }
            }

            if (removeAt > -1)
            {
                autoLootItems.RemoveAt(removeAt);
            }
        }

        public void OnSceneLoad()
        {
            Load();
            EventSink.OPLOnReceive += OnOPLReceived;
            EventSink.OnItemCreated += OnItemCreatedOrUpdated;
            EventSink.OnItemUpdated += OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer += OnOpenContainer;

        }

        public void OnSceneUnload()
        {
            EventSink.OPLOnReceive -= OnOPLReceived;
            EventSink.OnItemCreated -= OnItemCreatedOrUpdated;
            EventSink.OnItemUpdated -= OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer -= OnOpenContainer;
            Save();
        }

        private void CheckItem(Item i)
        {
            if (i == null) return;

            if (i.IsCorpse)
            {
                HandleCorpse(i);
                return;
            }

            var root = World.Items.Get(i.RootContainer);
            if (root != null && root.IsCorpse)
            {
                HandleCorpse(root);
                return;
            }
        }

        private void OnOpenContainer(object sender, uint e)
        {
            if (!loaded || !IsEnabled) return;

            HandleCorpse((Item)sender);
        }
        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            if (sender is Item i)
                CheckItem(i);
        }
        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            var item = World.Items.Get(e.Serial);
            if (item != null)
                CheckItem(item);
        }

        public void Update()
        {
            if (!loaded || !IsEnabled || !World.InGame) return;

            if (lootItems.Count == 0)
            {
                progressBarGump?.Dispose();
                return;
            }

            if (nextLootTime > Time.Ticks) return;


            var moveItem = lootItems.Dequeue();
            if (moveItem != 0)
            {
                if (lootItems.Count == 0) //Que emptied out                
                    currentLootTotalCount = 0;

                quickContainsLookup.Remove(moveItem);

                CreateProgressBar();

                if (progressBarGump != null && !progressBarGump.IsDisposed)
                {
                    progressBarGump.CurrentPercentage = 1 - ((double)lootItems.Count / (double)currentLootTotalCount);
                }

                Item m = World.Items.Get(moveItem);

                if (m != null)
                {
                    if (m.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                    {
                        Item rc = World.Items.Get(m.RootContainer);
                        if (rc != null && rc.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                            return;
                    }
                    GameActions.GrabItem(World, m, m.Amount);
                    nextLootTime = Time.Ticks + ProfileManager.CurrentProfile.MoveMultiObjectDelay;
                }
            }
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (progressBarGump == null || progressBarGump.IsDisposed))
            {
                progressBarGump = new ProgressBarGump(World, "Auto looting...", 0)
                {
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150,
                    ForegrouneColor = Color.DarkOrange
                };
                progressBarGump.CenterXInViewPort();
                UIManager.Add(progressBarGump);
            }
        }

        private void Load()
        {
            if (loaded) return;

            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(savePath))
                {
                    autoLootItems = new List<AutoLootConfigEntry>();
                    loaded = true;
                }
                else
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        autoLootItems = JsonSerializer.Deserialize(data, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                        loaded = true;
                    }
                    catch
                    {
                        GameActions.Print(World, "There was an error loading your auto loot config file, please check it with a json validator.", 32);
                        loaded = false;
                    }

                }
            });
        }

        public void Save()
        {
            if (loaded)
            {
                try
                {
                    string fileData = JsonSerializer.Serialize(autoLootItems, AutoLootJsonContext.Default.ListAutoLootConfigEntry);

                    File.WriteAllText(savePath, fileData);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }
        }

        public class AutoLootConfigEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            private bool RegexMatch => !string.IsNullOrEmpty(RegexSearch);
            /// <summary>
            /// Do not set this manually.
            /// </summary>
            public string UID { get; set; } = Guid.NewGuid().ToString();

            public bool Match(Item compareTo)
            {
                if (Graphic != -1 && Graphic != compareTo.Graphic) return false;

                if (!HueCheck(compareTo.Hue)) return false;

                if (RegexMatch && !RegexCheck(compareTo.World, compareTo)) return false;

                return true;
            }

            private bool HueCheck(ushort value)
            {
                if (Hue == ushort.MaxValue) //Ignore hue.
                {
                    return true;
                }
                else if (Hue == value) //Hue must match, and it does
                {
                    return true;
                }
                else //Hue is not ignored, and does not match
                {
                    return false;
                }
            }

            private bool RegexCheck(World world, Item compareTo)
            {
                string search = "";
                if (world.OPL.TryGetNameAndData(compareTo, out string name, out string data))
                    search += name + data;
                else
                    search = StringHelper.GetPluralAdjustedString(compareTo.ItemData.Name);

                return System.Text.RegularExpressions.Regex.IsMatch(search, RegexSearch, System.Text.RegularExpressions.RegexOptions.Multiline);
            }

            public bool Equals(AutoLootConfigEntry other)
            {
                return other.Graphic == Graphic && other.Hue == Hue;
            }
        }
    }
}
