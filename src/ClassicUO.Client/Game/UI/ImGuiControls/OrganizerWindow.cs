using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class OrganizerWindow : ImGuiWindow
    {
        private int _selectedConfigIndex = -1;
        private OrganizerConfig _selectedConfig = null;
        private string _newConfigName = "";
        private string _addItemGraphicInput = "";
        private string _addItemHueInput = "";
        private bool _showAddItemManual = false;

        public OrganizerWindow() : base("Organizer")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        protected override void DrawContent()
        {
            if (OrganizerAgent.Instance == null)
            {
                ImGui.Text("Organizer Agent not loaded");
                return;
            }

            // Main layout: left panel for organizer list, right panel for details
            if (ImGui.BeginTable("OrganizerTable", 2, ImGuiTableFlags.SizingFixedFit, new System.Numerics.Vector2(650, 250)))
            {
                ImGui.TableSetupColumn("Organizers", ImGuiTableColumnFlags.WidthFixed, 275);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawOrganizerList();

                ImGui.TableSetColumnIndex(1);
                DrawOrganizerDetails();

                ImGui.EndTable();
            }
        }

        private void DrawOrganizerList()
        {
            ImGui.Text("Organizers");
            ImGui.Separator();

            // Add new organizer
            ImGui.InputText("##NewOrganizerName", ref _newConfigName, 100);
            ImGui.SameLine();
            if (ImGui.Button("Add Organizer"))
            {
                var newConfig = OrganizerAgent.Instance.NewOrganizerConfig();
                if (!string.IsNullOrEmpty(_newConfigName))
                {
                    newConfig.Name = _newConfigName;
                }
                _newConfigName = "";
                _selectedConfigIndex = OrganizerAgent.Instance.OrganizerConfigs.IndexOf(newConfig);
                _selectedConfig = newConfig;
            }

            ImGui.Separator();

            // List existing organizers
            var configs = OrganizerAgent.Instance.OrganizerConfigs;
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                bool isSelected = i == _selectedConfigIndex;

                string label = $"{config.Name}##Config{i}";
                if (config.Enabled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                }

                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedConfigIndex = i;
                    _selectedConfig = config;
                }

                if (config.Enabled)
                {
                    ImGui.PopStyleColor();
                }

                // Show item count as tooltip
                if (ImGui.IsItemHovered())
                {
                    int enabledItems = config.ItemConfigs.Count(ic => ic.Enabled);
                    ImGui.SetTooltip($"{enabledItems} enabled items");
                }
            }
        }

        private void DrawOrganizerDetails()
        {
            if (_selectedConfig == null)
            {
                ImGui.Text("Select an organizer to view details");
                return;
            }

            ImGui.Text($"Organizer Details: {_selectedConfig.Name}");
            ImGui.Separator();

            // Name input
            string name = _selectedConfig.Name;
            if (ImGui.InputText("Name", ref name, 100))
            {
                _selectedConfig.Name = name;
            }

            ImGui.SameLine();
            bool enabled = _selectedConfig.Enabled;
            ImGui.Checkbox("Enabled", ref enabled);

            // Action buttons
            if (ImGui.Button("Run Organizer"))
            {
                OrganizerAgent.Instance?.RunOrganizer(_selectedConfig.Name);
            }

            ImGui.SameLine();
            if (ImGui.Button("Duplicate"))
            {
                var duplicated = OrganizerAgent.Instance?.DupeConfig(_selectedConfig);
                if (duplicated != null)
                {
                    _selectedConfigIndex = OrganizerAgent.Instance.OrganizerConfigs.IndexOf(duplicated);
                    _selectedConfig = duplicated;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Create Macro"))
            {
                OrganizerAgent.Instance?.CreateOrganizerMacroButton(_selectedConfig.Name);
                GameActions.Print($"Created Organizer Macro: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            if (ImGui.Button("Delete"))
            {
                OrganizerAgent.Instance?.DeleteConfig(_selectedConfig);
                _selectedConfig = null;
                _selectedConfigIndex = -1;
                return;
            }
            ImGui.PopStyleColor();

            ImGui.Separator();

            // Container settings
            DrawContainerSettings();

            ImGui.Separator();

            // Items section
            DrawItemsSection();
        }

        private void DrawContainerSettings()
        {
            ImGui.Text("Container Settings");

            if (ImGui.Button("Set Source Container"))
            {
                GameActions.Print("Select [SOURCE] Container", 82);
                TargetHelper.TargetObject(World.Instance, (source) =>
                {
                    if (source == null || !SerialHelper.IsItem(source))
                    {
                        GameActions.Print("Only items can be selected!");
                        return;
                    }
                    _selectedConfig.SourceContSerial = source.Serial;
                    GameActions.Print($"Source container set to {source.Serial:X}", 63);
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Set Destination Container"))
            {
                GameActions.Print("Select [DESTINATION] Container", 82);
                TargetHelper.TargetObject(World.Instance, (destination) =>
                {
                    if (destination == null || !SerialHelper.IsItem(destination))
                    {
                        GameActions.Print("Only items can be selected!");
                        return;
                    }
                    _selectedConfig.DestContSerial = destination.Serial;
                    GameActions.Print($"Destination container set to {destination.Serial:X}", 63);
                });
            }

            // Display current containers
            if (_selectedConfig.SourceContSerial != 0)
            {
                var sourceItem = World.Instance.Items.Get(_selectedConfig.SourceContSerial);
                ImGui.Text($"Source: {sourceItem?.Name ?? "Unknown"} ({_selectedConfig.SourceContSerial:X})");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Source: Your backpack");
            }

            if (_selectedConfig.DestContSerial != 0)
            {
                var destItem = World.Instance.Items.Get(_selectedConfig.DestContSerial);
                ImGui.Text($"Destination: {destItem?.Name ?? "Unknown"} ({_selectedConfig.DestContSerial:X})");
            }
            else
            {
                ImGui.Text("Destination: Not set");
            }
        }

        private void DrawItemsSection()
        {
            ImGui.Text("Items to Organize");

            // Add item buttons
            if (ImGui.Button("Target Item to Add"))
            {
                TargetHelper.TargetObject(World.Instance, (obj) =>
                {
                    if (!SerialHelper.IsItem(obj))
                    {
                        GameActions.Print("Only items can be added!");
                        return;
                    }

                    var newItemConfig = _selectedConfig.NewItemConfig();
                    newItemConfig.Graphic = obj.Graphic;
                    newItemConfig.Hue = obj.Hue;

                    GameActions.Print($"Added item: Graphic {obj.Graphic:X}, Hue {obj.Hue:X}");
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Add Item Manually"))
            {
                _showAddItemManual = !_showAddItemManual;
            }

            // Manual add item section
            if (_showAddItemManual)
            {
                ImGui.InputText("Graphic (hex)", ref _addItemGraphicInput, 10);
                ImGui.SameLine();
                ImGui.InputText("Hue (-1 for any)", ref _addItemHueInput, 10);
                ImGui.SameLine();
                if (ImGui.Button("Add"))
                {
                    if (ushort.TryParse(_addItemGraphicInput, System.Globalization.NumberStyles.HexNumber, null, out ushort graphic))
                    {
                        var newItemConfig = _selectedConfig.NewItemConfig();
                        newItemConfig.Graphic = graphic;

                        if (int.TryParse(_addItemHueInput, out int hue) && hue >= -1)
                        {
                            newItemConfig.Hue = hue == -1 ? ushort.MaxValue : (ushort)hue;
                        }

                        _addItemGraphicInput = "";
                        _addItemHueInput = "";
                        _showAddItemManual = false;
                    }
                }
            }

            ImGui.Separator();

            // Items table
            if (ImGui.BeginTable("ItemsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableHeadersRow();

                for (int i = _selectedConfig.ItemConfigs.Count - 1; i >= 0; i--)
                {
                    var itemConfig = _selectedConfig.ItemConfigs[i];
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"{itemConfig.Graphic:X4}");

                    ImGui.TableSetColumnIndex(1);
                    string hueText = itemConfig.Hue == ushort.MaxValue ? "ANY" : itemConfig.Hue.ToString();
                    ImGui.Text(hueText);

                    ImGui.TableSetColumnIndex(2);
                    int amount = itemConfig.Amount;
                    if (ImGui.InputInt($"##Amount{i}", ref amount, 0, 0))
                    {
                        itemConfig.Amount = (ushort)Math.Max(0, Math.Min(65535, amount));
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("0 = move all items");
                    }

                    ImGui.TableSetColumnIndex(3);
                    bool enabled = itemConfig.Enabled;
                    ImGui.Checkbox($"##Enabled{i}", ref enabled);

                    ImGui.TableSetColumnIndex(4);
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    if (ImGui.Button($"X##Delete{i}"))
                    {
                        _selectedConfig.DeleteItemConfig(itemConfig);
                    }
                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }
        }
    }
}
