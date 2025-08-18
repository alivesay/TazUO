// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using SDL2;
using static SDL2.SDL;

namespace ClassicUO
{
    internal unsafe class GameController : Microsoft.Xna.Framework.Game
    {
        private SDL_EventFilter _filter;

        private bool _ignoreNextTextInput;
        private readonly float[] _intervalFixedUpdate = new float[2];
        private double _totalElapsed, _currentFpsTime, _nextSlowUpdate;
        private uint _totalFrames;
        private UltimaBatcher2D _uoSpriteBatch;
        private bool _suppressedDraw;
        private Texture2D _background;
        private bool _pluginsInitialized = false;
        private Rectangle bufferRect = Rectangle.Empty;

        private static Vector3 bgHueShader = new Vector3(0, 0, 0.3f);
        private bool drawScene = false;

        public GameController(IPluginHost pluginHost)
        {
            GraphicManager = new GraphicsDeviceManager(this);

            GraphicManager.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                    RenderTargetUsage.DiscardContents;
            };

            GraphicManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            SetVSync(false);

            Window.ClientSizeChanged += WindowOnClientSizeChanged;
            Window.AllowUserResizing = true;
            Window.Title = $"TazUO - {CUOEnviroment.Version}";
            IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread;

            IsFixedTimeStep = false; // Settings.GlobalSettings.FixedTimeStep;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 250.0);
            PluginHost = pluginHost;
            bufferRect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
        }

        public Scene Scene { get; private set; }
        public AudioManager Audio { get; private set; }
        public UltimaOnline UO { get; } = new UltimaOnline();
        public IPluginHost PluginHost { get; private set; }
        public GraphicsDeviceManager GraphicManager { get; }
        public readonly uint[] FrameDelay = new uint[2];

        private readonly List<(uint, Action)> _queuedActions = new();

        public void EnqueueAction(uint time, Action action)
        {
            _queuedActions.Add((Time.Ticks + time, action));
        }

        protected override void Initialize()
        {
            if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
            {
                GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
            }

            GraphicManager.ApplyChanges();

            SetRefreshRate(Settings.GlobalSettings.FPS);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);

            _filter = HandleSdlEvent;
            SDL_SetEventFilter(_filter, IntPtr.Zero);

            base.Initialize();
        }

        private const int MAX_PACKETS_PER_FRAME = 25;

        private void ProcessNetworkPackets()
        {
            int packetsProcessed = 0;
            while (packetsProcessed < MAX_PACKETS_PER_FRAME && AsyncNetClient.Socket.TryDequeuePacket(out byte[] message))
            {
                var c = PacketHandlers.Handler.ParsePackets(Client.Game.UO.World, message);
                AsyncNetClient.Socket.Statistics.TotalPacketsReceived += (uint)c;
                packetsProcessed++;
            }
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Fonts.Initialize(GraphicsDevice);
            SolidColorTextureCache.Initialize(GraphicsDevice);

            Audio = new AudioManager();

            var bytes = Loader.GetBackgroundImage().ToArray();
            using var ms = new MemoryStream(bytes);
            _background = Texture2D.FromStream(GraphicsDevice, ms);

#if false
            SetScene(new MainScene(this));
#else
            UO.Load(this);

            PNGLoader.Instance.GraphicsDevice = GraphicsDevice;
            PNGLoader.Instance.LoadResourceAssets(Client.Game.UO.Gumps.GetGumpsLoader);

            Audio.Initialize();
            // TODO: temporary fix to avoid crash when laoding plugins
            Settings.GlobalSettings.Encryption = (byte)NetClient.Load(UO.FileManager.Version, (EncryptionType)Settings.GlobalSettings.Encryption);

            Log.Trace("Loading plugins...");
            PluginHost?.Initialize();

            foreach (string p in Settings.GlobalSettings.Plugins)
            {
                Plugin.Create(p);
            }
            _pluginsInitialized = true;

            Log.Trace("Done!");

            UIManager.World = UO.World;

            SetScene(new LoginScene(UO.World));
#endif
            SetWindowPositionBySettings();
            new DiscordManager(UO.World); //Instance is set inside the constructor
            DiscordManager.Instance.FromSavedToken();
        }

        protected override void UnloadContent()
        {
            DiscordManager.Instance.BeginDisconnect();
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out _, out _);

            Settings.GlobalSettings.WindowPosition = new Point(
                Math.Max(0, Window.ClientBounds.X - left),
                Math.Max(0, Window.ClientBounds.Y - top)
            );

            Audio?.StopMusic();
            Settings.GlobalSettings.Save();
            Plugin.OnClosing();


            UO.Unload();
            DiscordManager.Instance.FinalizeDisconnect();
            base.UnloadContent();
        }

        public void SetWindowTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
#if DEV_BUILD
                Window.Title = $"TazUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"[TazUO {CUOEnviroment.Version}]";
#endif
            }
            else
            {
#if DEV_BUILD
                Window.Title = $"{title} - TazUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"{title} - [TazUO {CUOEnviroment.Version}]";
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetScene<T>() where T : Scene
        {
            return Scene as T;
        }

        public void SetScene(Scene scene)
        {
            Scene?.Dispose();
            Scene = scene;
            Scene?.Load();

            if (Scene != null && Scene.IsLoaded)
                drawScene = true;
            else
                drawScene = false;
        }

        public void SetVSync(bool value)
        {
            GraphicManager.SynchronizeWithVerticalRetrace = value;
        }

        public void SetRefreshRate(int rate)
        {
            if (rate < Constants.MIN_FPS)
            {
                rate = Constants.MIN_FPS;
            }
            else if (rate > Constants.MAX_FPS)
            {
                rate = Constants.MAX_FPS;
            }

            float frameDelay;

            if (rate == Constants.MIN_FPS)
            {
                // The "real" UO framerate is 12.5. Treat "12" as "12.5" to match.
                frameDelay = 80;
            }
            else
            {
                frameDelay = 1000.0f / rate;
            }

            FrameDelay[0] = FrameDelay[1] = (uint)frameDelay;
            FrameDelay[1] = FrameDelay[1] >> 1;

            Settings.GlobalSettings.FPS = rate;

            _intervalFixedUpdate[0] = frameDelay;
            _intervalFixedUpdate[1] = 217; // 5 FPS
        }

        private void SetWindowPosition(int x, int y)
        {
            SDL_SetWindowPosition(Window.Handle, x, y);
        }

        public void SetWindowSize(int width, int height)
        {
            //width = (int) ((double) width * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width);
            //height = (int) ((double) height * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height);

            /*if (CUOEnviroment.IsHighDPI)
            {
                width *= 2;
                height *= 2;
            }
            */

            GraphicManager.PreferredBackBufferWidth = width;
            GraphicManager.PreferredBackBufferHeight = height;
            GraphicManager.ApplyChanges();
            bufferRect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
        }

        public void SetWindowBorderless(bool borderless)
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0 && borderless)
            {
                return;
            }

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == 0 && !borderless)
            {
                return;
            }

            SDL_SetWindowBordered(
                Window.Handle,
                borderless ? SDL_bool.SDL_FALSE : SDL_bool.SDL_TRUE
            );
            SDL_GetCurrentDisplayMode(
                SDL_GetWindowDisplayIndex(Window.Handle),
                out SDL_DisplayMode displayMode
            );

            int width = displayMode.w;
            int height = displayMode.h;

            if (borderless)
            {
                SetWindowSize(width, height);
                SDL_GetDisplayUsableBounds(
                    SDL_GetWindowDisplayIndex(Window.Handle),
                    out SDL_Rect rect
                );
                SDL_SetWindowPosition(Window.Handle, rect.x, rect.y);
            }
            else
            {
                SDL_GetWindowBordersSize(Window.Handle, out int top, out _, out int bottom, out _);

                SetWindowSize(width, height - (top - bottom));
                SetWindowPositionBySettings();
            }

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
            bufferRect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
        }

        public void MaximizeWindow()
        {
            SDL_MaximizeWindow(Window.Handle);

            GraphicManager.PreferredBackBufferWidth = Client.Game.Window.ClientBounds.Width;
            GraphicManager.PreferredBackBufferHeight = Client.Game.Window.ClientBounds.Height;
            GraphicManager.ApplyChanges();
            bufferRect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
        }

        public bool IsWindowMaximized()
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            return (flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
        }

        public void RestoreWindow()
        {
            SDL_RestoreWindow(Window.Handle);
        }

        public void SetWindowPositionBySettings()
        {
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out _, out _);

            if (Settings.GlobalSettings.WindowPosition.HasValue)
            {
                int x = left + Settings.GlobalSettings.WindowPosition.Value.X;
                int y = top + Settings.GlobalSettings.WindowPosition.Value.Y;
                x = Math.Max(0, x);
                y = Math.Max(0, y);

                SetWindowPosition(x, y);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            Profiler.ExitContext("OutOfContext");

            Time.Ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds;
            Time.Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Profiler.EnterContext("Mouse");
            Mouse.Update();
            Profiler.ExitContext("Mouse");

            Profiler.EnterContext("Packets");
            ProcessNetworkPackets();
            Profiler.ExitContext("Packets");

            Plugin.Tick();

            if(drawScene)
            {
                Profiler.EnterContext("Update");
                Scene.Update();
                Profiler.ExitContext("Update");
            }

            Profiler.EnterContext("UI Update");
            UIManager.Update();
            Profiler.ExitContext("UI Update");

            Profiler.EnterContext("LScript");
            LegionScripting.LegionScripting.OnUpdate();
            Profiler.ExitContext("LScript");

            MainThreadQueue.ProcessQueue();

            if (Time.Ticks >= _nextSlowUpdate)
            {
                _nextSlowUpdate = Time.Ticks + 500;
                UIManager.SlowUpdate();
            }

            _totalElapsed += gameTime.ElapsedGameTime.TotalMilliseconds;
            _currentFpsTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_currentFpsTime >= 1000)
            {
                CUOEnviroment.CurrentRefreshRate = _totalFrames;

                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            double x = _intervalFixedUpdate[
                !IsActive
                && ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.ReduceFPSWhenInactive
                    ? 1
                    : 0
            ];
            _suppressedDraw = false;

            if (_totalElapsed > x)
            {
                _totalElapsed %= x;
            }
            else
            {
                _suppressedDraw = true;
                SuppressDraw();

                if (!gameTime.IsRunningSlowly)
                {
                    Thread.Sleep(1);
                }
            }

            UO.GameCursor?.Update();
            Audio?.Update();

            DiscordManager.Instance.Update();


            for (var i = _queuedActions.Count - 1; i >= 0; i--)
            {
                (var time, var fn) = _queuedActions[i];

                if (Time.Ticks > time)
                {
                    fn();
                    _queuedActions.RemoveAt(i);
                    break;
                }
            }


            for (var i = _queuedActions.Count - 1; i >= 0; i--)
            {
                (var time, var fn) = _queuedActions[i];

                if (Time.Ticks > time)
                {
                    fn();
                    _queuedActions.RemoveAt(i);
                    break;
                }
            }

            base.Update(gameTime);
        }

        public static void UpdateBackgroundHueShader()
        {
            if (ProfileManager.CurrentProfile != null)
                bgHueShader = ShaderHueTranslator.GetHueVector(ProfileManager.CurrentProfile.MainWindowBackgroundHue, false, bgHueShader.Z);
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.EndFrame();
            Profiler.BeginFrame();
            Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("Draw-Tiles");

            _totalFrames++;
            GraphicsDevice.Clear(Color.Black);

            _uoSpriteBatch.Begin();
            _uoSpriteBatch.DrawTiled(_background, bufferRect, _background.Bounds, bgHueShader);
            _uoSpriteBatch.End();
            Profiler.ExitContext("Draw-Tiles");

            Profiler.EnterContext("Draw-Scene");
            if (drawScene)
                Scene.Draw(_uoSpriteBatch);
            Profiler.ExitContext("Draw-Scene");

            Profiler.EnterContext("Draw-UI");
            UIManager.Draw(_uoSpriteBatch);
            Profiler.ExitContext("Draw-UI");
            Profiler.EnterContext("OutOfContext");

            SelectedObject.HealthbarObject = null;
            SelectedObject.SelectedContainer = null;

            _uoSpriteBatch.Begin();
            UO.GameCursor?.Draw(_uoSpriteBatch);
            _uoSpriteBatch.End();

            base.Draw(gameTime);

            Plugin.ProcessDrawCmdList(GraphicsDevice);

            base.Draw(gameTime);
        }

        protected override bool BeginDraw()
        {
            return !_suppressedDraw && base.BeginDraw();
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs e)
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;

            if (!IsWindowMaximized())
            {
                if (ProfileManager.CurrentProfile != null)
                    ProfileManager.CurrentProfile.WindowClientBounds = new Point(width, height);
            }

            SetWindowSize(width, height);

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }

        private int HandleSdlEvent(IntPtr userData, IntPtr ptr)
        {
            SDL_Event* sdlEvent = (SDL_Event*)ptr;

            // Don't pass SDL events to the plugin host before the plugins are initialized
            // or the garbage collector can get screwed up
            if (_pluginsInitialized && Plugin.ProcessWndProc(sdlEvent) != 0)
            {
                if (sdlEvent->type == SDL_EventType.SDL_MOUSEMOTION)
                {
                    if (UO.GameCursor != null)
                    {
                        UO.GameCursor.AllowDrawSDLCursor = false;
                    }
                }

                return 1;
            }

            switch (sdlEvent->type)
            {
                case SDL_EventType.SDL_AUDIODEVICEADDED:
                    Log.Trace($"AUDIO ADDED: {sdlEvent->adevice.which}");

                    break;

                case SDL_EventType.SDL_AUDIODEVICEREMOVED:
                    Log.Trace($"AUDIO REMOVED: {sdlEvent->adevice.which}");

                    break;

                case SDL_EventType.SDL_WINDOWEVENT:

                    switch (sdlEvent->window.windowEvent)
                    {
                        case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                            Mouse.MouseInWindow = true;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                            Mouse.MouseInWindow = false;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                            Plugin.OnFocusGained();

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            Plugin.OnFocusLost();

                            break;
                    }

                    break;

                case SDL_EventType.SDL_KEYDOWN:

                    Keyboard.OnKeyDown(sdlEvent->key);

                    if (
                        Plugin.ProcessHotkeys(
                            (int)sdlEvent->key.keysym.sym,
                            (int)sdlEvent->key.keysym.mod,
                            true
                        )
                    )
                    {
                        _ignoreNextTextInput = false;

                        UIManager.KeyboardFocusControl?.InvokeKeyDown(
                            sdlEvent->key.keysym.sym,
                            sdlEvent->key.keysym.mod
                        );

                        Scene.OnKeyDown(sdlEvent->key);
                    }
                    else
                    {
                        _ignoreNextTextInput = true;
                    }

                    break;

                case SDL_EventType.SDL_KEYUP:

                    Keyboard.OnKeyUp(sdlEvent->key);
                    UIManager.KeyboardFocusControl?.InvokeKeyUp(
                        sdlEvent->key.keysym.sym,
                        sdlEvent->key.keysym.mod
                    );
                    Scene.OnKeyUp(sdlEvent->key);
                    Plugin.ProcessHotkeys(0, 0, false);

                    if (sdlEvent->key.keysym.sym == SDL_Keycode.SDLK_PRINTSCREEN)
                    {
                        if (Keyboard.Ctrl)
                        {
                            if (Tooltip.IsEnabled)
                            {
                                ClipboardScreenshot(new Rectangle(Tooltip.X, Tooltip.Y, Tooltip.Width, Tooltip.Height), GraphicsDevice);
                            }
                            else if (MultipleToolTipGump.SSIsEnabled)
                            {
                                ClipboardScreenshot(new Rectangle(MultipleToolTipGump.SSX, MultipleToolTipGump.SSY, MultipleToolTipGump.SSWidth, MultipleToolTipGump.SSHeight), GraphicsDevice);
                            }
                            else if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.IsVisible)
                            {
                                Control c = UIManager.MouseOverControl.RootParent;
                                if (c != null)
                                {
                                    ClipboardScreenshot(c.Bounds, GraphicsDevice);
                                }
                                else
                                {
                                    ClipboardScreenshot(UIManager.MouseOverControl.Bounds, GraphicsDevice);
                                }
                            }
                        }
                        else
                        {
                            TakeScreenshot();
                        }
                    }

                    break;

                case SDL_EventType.SDL_TEXTINPUT:

                    if (_ignoreNextTextInput)
                    {
                        break;
                    }

                    // Fix for linux OS: https://github.com/andreakarasho/ClassicUO/pull/1263
                    // Fix 2: SDL owns this behaviour. Cheating is not a real solution.
                    /*if (!Utility.Platforms.PlatformHelper.IsWindows)
                    {
                        if (Keyboard.Alt || Keyboard.Ctrl)
                        {
                            break;
                        }
                    }*/

                    string s = UTF8_ToManaged((IntPtr)sdlEvent->text.text, false);

                    if (!string.IsNullOrEmpty(s))
                    {
                        UIManager.KeyboardFocusControl?.InvokeTextInput(s);
                        Scene.OnTextInput(s);
                    }

                    break;

                case SDL_EventType.SDL_MOUSEMOTION:

                    if (UO.GameCursor != null && !UO.GameCursor.AllowDrawSDLCursor)
                    {
                        UO.GameCursor.AllowDrawSDLCursor = true;
                        UO.GameCursor.Graphic = 0xFFFF;
                    }

                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        if (!Scene.OnMouseDragging())
                        {
                            UIManager.OnMouseDragging();
                        }
                    }

                    break;

                case SDL_EventType.SDL_MOUSEWHEEL:
                    Mouse.Update();
                    bool isScrolledUp = sdlEvent->wheel.y > 0;

                    Plugin.ProcessMouse(0, sdlEvent->wheel.y);

                    if (!Scene.OnMouseWheel(isScrolledUp))
                    {
                        UIManager.OnMouseWheel(isScrolledUp);
                    }

                    break;

                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    {
                        SDL_MouseButtonEvent mouse = sdlEvent->button;

                        // The values in MouseButtonType are chosen to exactly match the SDL values
                        MouseButtonType buttonType = (MouseButtonType)mouse.button;

                        uint lastClickTime = 0;

                        switch (buttonType)
                        {
                            case MouseButtonType.Left:
                                lastClickTime = Mouse.LastLeftButtonClickTime;

                                break;

                            case MouseButtonType.Middle:
                                lastClickTime = Mouse.LastMidButtonClickTime;

                                break;

                            case MouseButtonType.Right:
                                lastClickTime = Mouse.LastRightButtonClickTime;

                                break;

                            case MouseButtonType.XButton1:
                            case MouseButtonType.XButton2:
                                break;

                            default:
                                Log.Warn($"No mouse button handled: {mouse.button}");

                                break;
                        }

                        Mouse.ButtonPress(buttonType);
                        Mouse.Update();

                        uint ticks = Time.Ticks;

                        if (lastClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                        {
                            lastClickTime = 0;

                            bool res =
                                Scene.OnMouseDoubleClick(buttonType)
                                || UIManager.OnMouseDoubleClick(buttonType);

                            if (!res)
                            {
                                if (!Scene.OnMouseDown(buttonType))
                                {
                                    UIManager.OnMouseButtonDown(buttonType);
                                }
                            }
                            else
                            {
                                lastClickTime = 0xFFFF_FFFF;
                            }
                        }
                        else
                        {
                            if (
                                buttonType != MouseButtonType.Left
                                && buttonType != MouseButtonType.Right
                            )
                            {
                                Plugin.ProcessMouse(sdlEvent->button.button, 0);
                            }

                            if (!Scene.OnMouseDown(buttonType))
                            {
                                UIManager.OnMouseButtonDown(buttonType);
                            }

                            lastClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                        }

                        switch (buttonType)
                        {
                            case MouseButtonType.Left:
                                Mouse.LastLeftButtonClickTime = lastClickTime;

                                break;

                            case MouseButtonType.Middle:
                                Mouse.LastMidButtonClickTime = lastClickTime;

                                break;

                            case MouseButtonType.Right:
                                Mouse.LastRightButtonClickTime = lastClickTime;

                                break;
                        }

                        break;
                    }

                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    {
                        SDL_MouseButtonEvent mouse = sdlEvent->button;

                        // The values in MouseButtonType are chosen to exactly match the SDL values
                        MouseButtonType buttonType = (MouseButtonType)mouse.button;

                        uint lastClickTime = 0;

                        switch (buttonType)
                        {
                            case MouseButtonType.Left:
                                lastClickTime = Mouse.LastLeftButtonClickTime;

                                break;

                            case MouseButtonType.Middle:
                                lastClickTime = Mouse.LastMidButtonClickTime;

                                break;

                            case MouseButtonType.Right:
                                lastClickTime = Mouse.LastRightButtonClickTime;

                                break;

                            default:
                                Log.Warn($"No mouse button handled: {mouse.button}");

                                break;
                        }

                        if (lastClickTime != 0xFFFF_FFFF)
                        {
                            if (
                                !Scene.OnMouseUp(buttonType)
                                || UIManager.LastControlMouseDown(buttonType) != null
                            )
                            {
                                UIManager.OnMouseButtonUp(buttonType);
                            }
                        }

                        Mouse.ButtonRelease(buttonType);
                        Mouse.Update();

                        break;
                    }

                case SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    if (!IsActive || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.ControllerEnabled)
                    {
                        break;
                    }
                    Controller.OnButtonDown(sdlEvent->cbutton);
                    UIManager.KeyboardFocusControl?.InvokeControllerButtonDown((SDL_GameControllerButton)sdlEvent->cbutton.button);
                    Scene.OnControllerButtonDown(sdlEvent->cbutton);

                    if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = SDL_EventType.SDL_MOUSEBUTTONDOWN;
                        e.button.button = (byte)MouseButtonType.Left;
                        SDL2.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = SDL_EventType.SDL_MOUSEBUTTONDOWN;
                        e.button.button = (byte)MouseButtonType.Right;
                        SDL2.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START && UO.World.InGame)
                    {
                        Gump g = UIManager.GetGump<ModernOptionsGump>();
                        if (g == null)
                        {
                            UIManager.Add(new ModernOptionsGump(UIManager.World));
                        }
                        else
                        {
                            g.Dispose();
                        }
                    }
                    break;

                case SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    if (!IsActive)
                    {
                        break;
                    }
                    Controller.OnButtonUp(sdlEvent->cbutton);
                    UIManager.KeyboardFocusControl?.InvokeControllerButtonUp((SDL_GameControllerButton)sdlEvent->cbutton.button);
                    Scene.OnControllerButtonUp(sdlEvent->cbutton);

                    if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = SDL_EventType.SDL_MOUSEBUTTONUP;
                        e.button.button = (byte)MouseButtonType.Left;
                        SDL2.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = SDL_EventType.SDL_MOUSEBUTTONUP;
                        e.button.button = (byte)MouseButtonType.Right;
                        SDL2.SDL.SDL_PushEvent(ref e);
                    }
                    break;

                case SDL_EventType.SDL_CONTROLLERAXISMOTION: //Work around because sdl doesn't see trigger buttons as buttons, they are axis probably for pressure support
                                                             //GameActions.Print(typeof(SDL_GameControllerButton).GetEnumName((SDL_GameControllerButton)sdlEvent->cbutton.button));
                    if (!IsActive)
                    {
                        break;
                    }
                    if (sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK || sdlEvent->cbutton.button == (byte)SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) //Left trigger BACK Right trigger GUIDE
                    {
                        if (sdlEvent->caxis.axisValue > 32000)
                        {
                            if (
                                ((SDL_GameControllerButton)sdlEvent->cbutton.button == SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK && !Controller.Button_LeftTrigger)
                                || ((SDL_GameControllerButton)sdlEvent->cbutton.button == SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE && !Controller.Button_RightTrigger)
                                )
                            {
                                Controller.OnButtonDown(sdlEvent->cbutton);
                                UIManager.KeyboardFocusControl?.InvokeControllerButtonDown((SDL_GameControllerButton)sdlEvent->cbutton.button);
                                Scene.OnControllerButtonDown(sdlEvent->cbutton);
                            }
                        }
                        else if (sdlEvent->caxis.axisValue < 5000)
                        {
                            Controller.OnButtonUp(sdlEvent->cbutton);
                            UIManager.KeyboardFocusControl?.InvokeControllerButtonUp((SDL_GameControllerButton)sdlEvent->cbutton.button);
                            Scene.OnControllerButtonUp(sdlEvent->cbutton);
                        }
                    }
                    break;
            }

            return 1;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Scene?.Dispose();

            base.OnExiting(sender, args);
        }

        private void TakeScreenshot()
        {
            string screenshotsFolder = FileSystemHelper.CreateFolderIfNotExists(
                CUOEnviroment.ExecutablePath,
                "Data",
                "Client",
                "Screenshots"
            );

            string path = Path.Combine(
                screenshotsFolder,
                $"screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.png"
            );

            Color[] colors = new Color[
                GraphicManager.PreferredBackBufferWidth * GraphicManager.PreferredBackBufferHeight
            ];

            GraphicsDevice.GetBackBufferData(colors);

            using (
                Texture2D texture = new Texture2D(
                    GraphicsDevice,
                    GraphicManager.PreferredBackBufferWidth,
                    GraphicManager.PreferredBackBufferHeight,
                    false,
                    SurfaceFormat.Color
                )
            )
            using (FileStream fileStream = File.Create(path))
            {
                texture.SetData(colors);
                texture.SaveAsPng(fileStream, texture.Width, texture.Height);
                string message = string.Format(ResGeneral.ScreenshotStoredIn0, path);

                if (
                    ProfileManager.CurrentProfile == null
                    || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage
                )
                {
                    Log.Info(message);
                }
                else
                {
                    GameActions.Print(UO.World, message, 0x44, MessageType.System);
                }
            }
        }

        public void ClipboardScreenshot(Rectangle position, GraphicsDevice graphicDevice)
        {
            Color[] colors = new Color[position.Width * position.Height];

            graphicDevice.GetBackBufferData(position, colors, 0, colors.Length);

            using (
                Texture2D texture = new Texture2D(
                    GraphicsDevice,
                    position.Width,
                    position.Height,
                    false,
                    SurfaceFormat.Color
                )
            )
            {
                texture.SetData(colors);

                string screenshotsFolder = FileSystemHelper.CreateFolderIfNotExists(
                    CUOEnviroment.ExecutablePath,
                    "Data",
                    "Client",
                    "Screenshots"
                );

                string path = Path.Combine(
                    screenshotsFolder,
                    $"screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.png"
                );

                using FileStream fileStream = File.Create(path);
                texture.SaveAsPng(fileStream, texture.Width, texture.Height);
                string message = string.Format(ResGeneral.ScreenshotStoredIn0, path);

                if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage)
                {
                    Log.Info(message);
                }
                else
                {
                    GameActions.Print(UO.World, message, 0x44, MessageType.System);
                }
            }
        }
    }
}
