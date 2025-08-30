using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ImGuiNET.SampleProgram.XNA;
using ClassicUO.Game.UI.ImGuiControls;

namespace ClassicUO.Game.UI
{
    internal static class ImGuiManager
    {
        private static ImGuiRenderer _imGuiRenderer;
        private static bool _isInitialized;
        private static bool _showDemoWindow;
        private static bool _showDebugWindow = true;
        private static readonly List<ImGuiWindow> _windows = new List<ImGuiWindow>();
        private static Microsoft.Xna.Framework.Game _game;

        public static bool IsInitialized => _isInitialized;
        public static ImGuiRenderer Renderer => _imGuiRenderer;

        public static void AddWindow(ImGuiWindow window)
        {
            if (window == null) return;
            if (!_windows.Contains(window))
                _windows.Add(window);
        }

        public static void RemoveWindow(ImGuiWindow window)
        {
            if (window == null) return;
            _windows.Remove(window);
        }

        public static void RemoveAllWindows()
        {
            foreach (var window in _windows)
            {
                window?.Dispose();
            }
            _windows.Clear();
        }

        private static void SetDarkTheme()
        {
            var io = ImGui.GetIO();
            unsafe
            {
                fixed (byte* fontPtr = TrueTypeLoader.Instance.ImGuiFont)
                {
                    ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                        (IntPtr)fontPtr,
                        TrueTypeLoader.Instance.ImGuiFont.Length,
                        16.0f // font size
                    );
                }
            }

            var style = ImGui.GetStyle();

            // Style settings
            style.WindowMinSize = new System.Numerics.Vector2(160, 20);
            style.FramePadding = new System.Numerics.Vector2(4, 2);
            style.ItemSpacing = new System.Numerics.Vector2(6, 2);
            style.ItemInnerSpacing = new System.Numerics.Vector2(6, 4);
            style.Alpha = 0.95f;
            style.WindowRounding = 4.0f;
            style.FrameRounding = 2.0f;
            style.IndentSpacing = 6.0f;
            style.ColumnsMinSpacing = 50.0f;
            style.GrabMinSize = 14.0f;
            style.GrabRounding = 16.0f;
            style.ScrollbarSize = 12.0f;
            style.ScrollbarRounding = 16.0f;

            // Dark theme colors
            var colors = style.Colors;
            colors[(int)ImGuiCol.Text] = new System.Numerics.Vector4(0.86f, 0.93f, 0.89f, 0.78f);
            colors[(int)ImGuiCol.TextDisabled] = new System.Numerics.Vector4(0.86f, 0.93f, 0.89f, 0.28f);
            colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.13f, 0.14f, 0.17f, 1.00f);
            colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.31f, 0.31f, 1.00f, 0.00f);
            colors[(int)ImGuiCol.BorderShadow] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.78f);
            colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 0.75f);
            colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 0.47f);
            colors[(int)ImGuiCol.ScrollbarBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new System.Numerics.Vector4(0.09f, 0.15f, 0.16f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.78f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(0.71f, 0.22f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4(0.47f, 0.77f, 0.83f, 0.14f);
            colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.47f, 0.77f, 0.83f, 0.14f);
            colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.86f);
            colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.76f);
            colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.86f);
            colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new System.Numerics.Vector4(0.14f, 0.16f, 0.19f, 1.00f);
            colors[(int)ImGuiCol.SeparatorHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new System.Numerics.Vector4(0.47f, 0.77f, 0.83f, 0.04f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.78f);
            colors[(int)ImGuiCol.ResizeGripActive] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.PlotLines] = new System.Numerics.Vector4(0.86f, 0.93f, 0.89f, 0.63f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new System.Numerics.Vector4(0.86f, 0.93f, 0.89f, 0.63f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new System.Numerics.Vector4(0.92f, 0.18f, 0.29f, 0.43f);
            colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 0.9f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.27f, 0.73f);
        }

        public static void Initialize(Microsoft.Xna.Framework.Game game)
        {
            //return; //Disable for now, basic implementation is done

            _game = game;
            try
            {
                _imGuiRenderer = new ImGuiRenderer(game);
                SetDarkTheme();
                _imGuiRenderer.RebuildFontAtlas();

                _isInitialized = true;
                Log.Info("ImGui initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize ImGui: {ex.Message}");
                _isInitialized = false;
            }
        }

        public static void Update(GameTime gameTime)
        {
            if (!_isInitialized)
                return;

            try
            {
                if (!_imGuiRenderer.BeforeLayout(gameTime))
                    return;

                DrawImGui();

                _imGuiRenderer.AfterLayout();
            }
            catch (Exception ex)
            {
                Log.Error($"ImGui update error: {ex.Message}");
            }
        }

        private static void DrawImGui()
        {
            // Draw managed windows
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                var window = _windows[i];
                if (window != null)
                {
                    if (window.IsOpen)
                    {
                        window.Update();
                        window.Draw();
                    }
                    else
                    {
                        window.Dispose();
                        _windows.RemoveAt(i);
                    }
                }
                else
                {
                    _windows.RemoveAt(i);
                }
            }
            //
            // if (_showDemoWindow)
            // {
            //     ImGui.ShowDemoWindow(ref _showDemoWindow);
            // }
            //
            // if (_showDebugWindow)
            // {
            //     DrawDebugWindow();
            // }
            //
            // // Example custom window
            // DrawExampleWindow();
        }

        private static void DrawDebugWindow()
        {
            if (ImGui.Begin("TazUO Debug", ref _showDebugWindow))
            {
                ImGui.Text($"TazUO Version: {CUOEnviroment.Version}");
                ImGui.Text($"FPS: {CUOEnviroment.CurrentRefreshRate}");
                ImGui.Separator();

                if (ImGui.CollapsingHeader("Settings"))
                {
                    if (Settings.GlobalSettings != null)
                    {
                        ImGui.Text($"FPS Limit: {Settings.GlobalSettings.FPS}");
                        ImGui.Text($"VSync: {Settings.GlobalSettings.FixedTimeStep}");
                        if (ProfileManager.CurrentProfile != null)
                        {
                            ImGui.Text($"Window Size: {ProfileManager.CurrentProfile.WindowClientBounds}");
                        }
                    }
                }

                if (ImGui.CollapsingHeader("ImGui"))
                {
                    ImGui.Checkbox("Show Demo Window", ref _showDemoWindow);

                    if (ImGui.Button("Test Notification"))
                    {
                        Log.Info("ImGui test notification triggered!");
                    }
                }

                if (ImGui.CollapsingHeader("World"))
                {
                    var player = World.Instance.Player;
                    if (player != null)
                    {
                        ImGui.Text($"Player Name: {player.Name}");
                        ImGui.Text($"Player Position: {player.X}, {player.Y}, {player.Z}");
                        ImGui.Text($"Player Health: {player.Hits}/{player.HitsMax}");
                        ImGui.Text($"Player Mana: {player.Mana}/{player.ManaMax}");
                        ImGui.Text($"Player Stamina: {player.Stamina}/{player.StaminaMax}");
                    }
                    else
                    {
                        ImGui.Text("Player: Not in game");
                    }
                }
            }
            ImGui.End();
        }

        private static void DrawExampleWindow()
        {
            if (ImGui.Begin("ImGui Example Window"))
            {
                ImGui.Text("Welcome to ImGui in TazUO!");
                ImGui.Separator();

                ImGui.Text("This is a basic example window to demonstrate ImGui integration.");

                if (ImGui.Button("Test Button"))
                {
                    Log.Info("ImGui test button clicked!");
                }

                ImGui.SameLine();
                if (ImGui.Button("Another Button"))
                {
                    Log.Info("Another ImGui button clicked!");
                }

                ImGui.Text($"Mouse Position: {ImGui.GetMousePos()}");
                ImGui.Text($"Time: {DateTime.Now:HH:mm:ss}");

                if (ImGui.CollapsingHeader("Advanced Controls"))
                {
                    ImGui.SliderFloat("Test Slider", ref _testSlider, 0.0f, 1.0f);
                    ImGui.ColorEdit3("Test Color", ref _testColor);
                    ImGui.Checkbox("Test Checkbox", ref _testCheckbox);
                }
            }
            ImGui.End();
        }

        private static float _testSlider = 0.5f;
        private static System.Numerics.Vector3 _testColor = new System.Numerics.Vector3(1.0f, 0.0f, 1.0f);
        private static bool _testCheckbox = true;

        public static void Dispose()
        {
            if (_isInitialized)
            {
                RemoveAllWindows();
                _imGuiRenderer = null;
                _isInitialized = false;
                Log.Info("ImGui disposed");
            }
        }
    }
}
