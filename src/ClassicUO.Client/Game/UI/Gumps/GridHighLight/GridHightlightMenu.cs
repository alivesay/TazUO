using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    internal class GridHighlightMenu : NineSliceGump
    {
        private const int WIDTH = 350, HEIGHT = 500;
        private SettingsSection highlightSection;
        private ScrollArea highlightSectionScroll;

        public GridHighlightMenu(int x = 100, int y = 100) : base(x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, WIDTH, HEIGHT)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            BuildGump();
        }

        private void BuildGump()
        {
            Clear();
            int y = 0;
            {
                SettingsSection section = new SettingsSection("Grid highlighting settings", Width - (BorderSize*2));
                section.X = BorderSize;
                section.Y = BorderSize;
                section.Add(new Label("You can add object properties that you would like the grid to be highlighted for here.", true, 0xffff, section.Width - 15));

                NiceButton _;
                section.Add(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Add +") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        highlightSectionScroll?.Add(NewAreaSection(ProfileManager.CurrentProfile.GridHighlightSetup.Count, y));
                        y += 21;
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Export") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ExportGridHighlightSettings();
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Import") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ImportGridHighlightSettings();
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Configs") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        UIManager.GetGump<GridHighlightConfig>()?.Dispose();
                        UIManager.Add(new GridHighlightConfig(100, 100));
                    }
                };

                Add(section);
                y = section.Y + section.Height;
            }

            highlightSection = new SettingsSection("", Width - (BorderSize * 2)) { Y = y, X = BorderSize };
            highlightSection.Add(highlightSectionScroll = new ScrollArea(0, 0, highlightSection.Width - 20, Height - y - 10, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways }); ;

            y = 0;
            for (int i = 0; i < ProfileManager.CurrentProfile.GridHighlightSetup.Count; i++)
            {
                highlightSectionScroll.Add(NewAreaSection(i, y));
                y += 21;
            }

            Add(highlightSection);
        }

        private Area NewAreaSection(int keyLoc, int y)
        {
            GridHighlightData data = GridHighlightData.GetGridHighlightData(keyLoc);
            Area area = new Area() { Y = y, X = BorderSize };
            area.Width = highlightSectionScroll.Width - 18;
            area.Height = 150;
            y = 0;
            int spaceBetween = 7;

            InputField _name;
            area.Add(_name = new InputField(0x0BB8, 0xFF, 0xFFFF, true, area.Width - 105, 20)
                {
                    X = 0,
                    Y = y,
                    AcceptKeyboardInput = true
                }
            );
            _name.SetText(data.Name);
            CancellationTokenSource cts = new CancellationTokenSource();
            _name.TextChanged += async (s, e) =>
            {
                cts.Cancel();
                cts = new CancellationTokenSource();
                var token = cts.Token;

                try
                {
                    await Task.Delay(500, token);
                    if (!token.IsCancellationRequested)
                    {
                        var tVal = _name.Text;
                        if (_name.Text == tVal)
                        {
                            data.Name = _name.Text;
                        }
                    }
                }
                catch (TaskCanceledException) { }
            };

            ModernColorPicker.HueDisplay hueDisplay;
            area.Add(hueDisplay = new ModernColorPicker.HueDisplay(data.Hue, null, true) { X = area.Width - 98 - spaceBetween, Y = y });
            hueDisplay.SetTooltip("Select grid highlight hue");
            hueDisplay.HueChanged += (s, e) =>
            {
                data.Hue = hueDisplay.Hue;
            };

            NiceButton _button;
            area.Add(_button = new NiceButton(area.Width - 20 - 60 - spaceBetween, y, 60, 20, ButtonAction.Activate, "Properties") { IsSelectable = false });
            _button.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    UIManager.GetGump<GridHighlightProperties>()?.Dispose();
                    UIManager.Add(new GridHighlightProperties(keyLoc, 100, 100));
                }
            };

            NiceButton _del;
            area.Add(_del = new NiceButton(area.Width - 20 - spaceBetween, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this highlight configuration");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Delete();
                    Dispose();
                    UIManager.Add(new GridHighlightMenu(X, Y));
                }
            };
            area.ForceSizeUpdate(false);
            return area;
        }

        private static void SaveProfile()
        {
            GridHighlightRules.SaveGridHighlightConfiguration();
        }

        public static void Open()
        {
            UIManager.GetGump<GridHighlightMenu>()?.Dispose();
            UIManager.Add(new GridHighlightMenu());
        }

        private static void ExportGridHighlightSettings()
        {
            var data = ProfileManager.CurrentProfile.GridHighlightSetup;

            RunFileDialog(true, "Save grid highlight settings", file =>
            {
                if (Directory.Exists(file))
                {
                    // If the path is a directory, append default filename
                    file = Path.Combine(file, "highlights.json");
                }
                else if (!Path.HasExtension(file))
                {
                    // If it's not a directory and has no extension, assume they meant a file name
                    file += ".json";
                }

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
                GameActions.Print($"Saved highlight export to: {file}");
            });
        }


        private static void ImportGridHighlightSettings()
        {
            RunFileDialog(false, "Import grid highlight settings", file =>
            {
                try
                {
                    if (!File.Exists(file))
                        return;

                    string json = File.ReadAllText(file);
                    var imported = JsonSerializer.Deserialize<List<GridHighlightSetupEntry>>(json);
                    if (imported != null)
                    {
                        ProfileManager.CurrentProfile.GridHighlightSetup.AddRange(imported);;
                        SaveProfile();
                        UIManager.GetGump<GridHighlightMenu>()?.Dispose();
                        UIManager.Add(new GridHighlightMenu());
                        GameActions.Print($"Imported highlight config from: {file}");
                    }
                }
                catch (Exception ex)
                {
                    GameActions.Print("Error importing highlight config", 32);
                    Log.Error(ex.ToString());
                }
            });
        }

        private static void RunFileDialog(bool save, string title, Action<string> onResult)
        {
            FileSelector.ShowFileBrowser(save ? FileSelectorType.Directory : FileSelectorType.File, null, save ? null : ["*.json"], onResult, title);
        }
    }
}
