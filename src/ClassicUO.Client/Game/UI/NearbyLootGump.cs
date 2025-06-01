using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using static ClassicUO.Game.UI.Gumps.GridHightlightMenu;

namespace ClassicUO.Game.UI
{
    public class NearbyLootGump : Gump
    {
        public const int WIDTH = 250;

        public static int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (value < -1)
                    selectedIndex = -1;
                else
                    selectedIndex = value;
            }
        }

        private ScrollArea scrollArea;
        private DataBox dataBox;
        private NiceButton lootButton;
        private AlphaBlendControl alphaBG;
        private int itemCount = 0;

        private HitBox resizeDrag;
        private bool dragging = false;
        private int dragStartH = 0;

        private static HashSet<uint> _corpsesRequested = new HashSet<uint>();
        private static HashSet<uint> _openedCorpses = new HashSet<uint>();
        private static int selectedIndex;
        private static Point lastLocation;
        private World world;

        public NearbyLootGump(World world) : base(world, 0, 0)
        {
            UIManager.GetGump<NearbyLootGump>()?.Dispose();
            this.world = world;
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            CanCloseWithRightClick = true;
            Width = WIDTH;
            Height = ProfileManager.CurrentProfile.NearbyLootGumpHeight;

            if (lastLocation == default)
            {
                CenterXInViewPort();
                CenterYInViewPort();
            }
            else
                Location = lastLocation;

            Add(alphaBG = new AlphaBlendControl() { Width = Width, Height = Height });

            Control c;
            c = TextBox.GetOne("Nearby corpse loot", Assets.TrueTypeLoader.EMBEDDED_FONT, 24, Color.OrangeRed, TextBox.RTLOptions.DefaultCentered(WIDTH));
            c.AcceptMouseInput = false;
            Add(c);

            Add(c = new NiceButton(Width - 20, 0, 20, 20, ButtonAction.Default, "+"));
            c.SetTooltip("Options");
            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                    GenOptionsContext().Show();
            };

            Add(lootButton = new NiceButton(0, c.Height, WIDTH >> 1, 20, ButtonAction.Default, "Loot All"));
            lootButton.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    foreach (Control control in dataBox.Children)
                    {
                        if (control is NearbyItemDisplay display)
                        {
                            AutoLootManager.Instance.LootItem(display.LocalSerial);
                        }
                    }
                }
            };

            Add(c = new NiceButton(WIDTH >> 1, c.Height, WIDTH >> 1, 20, ButtonAction.Default, "Set Loot Bag"));
            c.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;

                GameActions.Print(World, Resources.ResGumps.TargetContainerToGrabItemsInto);
                World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            };

            Add(scrollArea = new ScrollArea(0, lootButton.Y + lootButton.Height, Width, Height - lootButton.Y - lootButton.Height, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            scrollArea.Add(dataBox = new DataBox(0, 0, Width, scrollArea.Height));

            Add(resizeDrag = new HitBox(Width / 2 - 10, Height - 10, 20, 10, "Drag to resize", 0.50f));
            resizeDrag.Add(new AlphaBlendControl(0.25f) { Width = 20, Height = 10, BaseColor = Color.OrangeRed });
            resizeDrag.MouseDown += ResizeDrag_MouseDown;
            resizeDrag.MouseUp += ResizeDrag_MouseUp;

            EventSink.OnCorpseCreated += EventSink_OnCorpseCreated;
            EventSink.OnPositionChanged += EventSink_OnPositionChanged;
            EventSink.OPLOnReceive += EventSink_OPLOnReceive;
            RequestUpdateContents();
        }

        private void EventSink_OPLOnReceive(object sender, OPLEventArgs e)
        {
            Item i = World.Items.Get(e.Serial);

            if (i != null && _openedCorpses.Contains(i.RootContainer))
                RequestUpdateContents();
        }

        private void EventSink_OnPositionChanged(object sender, PositionChangedArgs e)
        {
            RequestUpdateContents();
        }

        private void ResizeDrag_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void ResizeDrag_MouseDown(object sender, MouseEventArgs e)
        {
            dragStartH = Height;
            dragging = true;
        }

        private void EventSink_OnCorpseCreated(object sender, System.EventArgs e)
        {
            Item item = (Item)sender;
            if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
            {
                TryRequestOpenCorpse(item);
            }
        }

        private ContextMenuControl GenOptionsContext()
        {
            var c = new ContextMenuControl(this);
            c.Add(new ContextMenuItemEntry("Open human corpses?", () =>
            {
                ProfileManager.CurrentProfile.NearbyLootOpensHumanCorpses ^= true;
                RequestUpdateContents();
            }, true, ProfileManager.CurrentProfile.NearbyLootOpensHumanCorpses));

            return c;
        }
        private void UpdateNearbyLoot()
        {
            itemCount = 0;

            ClearDataBox();
            _openedCorpses.Clear();

            List<Item> finalItemList = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    ProcessCorpse(item, ref finalItemList);
                }
            }

            finalItemList = finalItemList
                .OrderBy(item => !item.IsCoin) // Items that are coins come first
                .ThenBy(item => item.Graphic)           // Sort by Graphic
                .ThenBy(item => item.Hue)               // Sort by Hue
                .ToList();

            foreach (Item lootItem in finalItemList)
            {
                dataBox.Add(NearbyItemDisplay.GetOne(world, lootItem, itemCount));
                itemCount++;
            }

            dataBox.ReArrangeChildren(1);
            dataBox.ForceSizeUpdate(false);

            if (SelectedIndex >= itemCount)
                SelectedIndex = itemCount - 1;
        }
        private void ProcessCorpse(Item corpse, ref List<Item> itemList)
        {
            if (corpse == null || !corpse.IsCorpse || (corpse.IsHumanCorpse && !ProfileManager.CurrentProfile.NearbyLootOpensHumanCorpses))
                return;

            if (corpse.Items != null)
            {
                if (_corpsesRequested.Contains(corpse))
                    _corpsesRequested.Remove(corpse);

                _openedCorpses.Add(corpse);
                for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                {

                    Item item = (Item)i;
                    if (item == null || item.Graphic == default(ushort) || !item.IsLootable)
                        continue;

                    itemList.Add(item);
                }

            }
            else
            {
                TryRequestOpenCorpse(corpse);
            }
        }
        private void TryRequestOpenCorpse(Item corpse)
        {
            if (_corpsesRequested.Contains(corpse))
                return;
            if (corpse.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                return;

            _corpsesRequested.Add(corpse.Serial);

            GameActions.DoubleClickQueued(corpse.Serial);
        }
        private void LootSelectedIndex()
        {
            if (SelectedIndex == -1)
                lootButton.InvokeMouseUp(lootButton.Location, MouseButtonType.Left);
            else if (dataBox.Children.Count > SelectedIndex)
            {
                AutoLootManager.Instance.LootItem(dataBox.Children[SelectedIndex].LocalSerial);
            }
        }
        private void ClearDataBox()
        {
            List<Control> removeAfter = new List<Control>();

            foreach (Control c in dataBox.Children)
            {
                if (c is NearbyItemDisplay cd)
                {
                    cd.ReturnToPool();
                    removeAfter.Add(c);
                }
                else
                    c.Dispose();
            }

            foreach (Control c in removeAfter)
                dataBox.Remove(c);
        }

        public static bool IsCorpseRequested(uint serial, bool remove = true)
        {
            if (_corpsesRequested.Contains(serial))
            {
                if (remove) _corpsesRequested.Remove(serial);
                return true;
            }

            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            _corpsesRequested.Clear();
            EventSink.OnCorpseCreated -= EventSink_OnCorpseCreated;
            resizeDrag.MouseUp -= ResizeDrag_MouseUp;
            resizeDrag.MouseDown -= ResizeDrag_MouseDown;
            EventSink.OPLOnReceive -= EventSink_OPLOnReceive;
            lastLocation = Location;
        }
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            base.OnKeyDown(key, mod);

            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_UP:
                    SelectedIndex--;
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN:
                    SelectedIndex++;
                    break;
                case SDL.SDL_Keycode.SDLK_RETURN:
                    LootSelectedIndex();
                    break;

            }
        }
        protected override void OnControllerButtonDown(SDL.SDL_GameControllerButton button)
        {
            base.OnControllerButtonDown(button);
            switch (button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    SelectedIndex--;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    SelectedIndex++;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    LootSelectedIndex();
                    break;
            }
        }
        public override void Update()
        {
            base.Update();

            if (selectedIndex == -1)
                lootButton.IsSelected = true;
            else
                lootButton.IsSelected = false;

            int steps = Mouse.LDragOffset.Y;

            if (dragging && steps != 0)
            {
                Height = dragStartH + steps;
                if (Height < 200)
                    Height = 200;
                ProfileManager.CurrentProfile.NearbyLootGumpHeight = Height;


                scrollArea.Height = Height - lootButton.Y - lootButton.Height;
                alphaBG.Height = Height;
                resizeDrag.Y = Height - 10;
                scrollArea.SlowUpdate();//Recalculate scrollbar
            }
        }
        protected override void UpdateContents()
        {
            base.UpdateContents();
            UpdateNearbyLoot();
        }
    }

    public class NearbyItemDisplay : Control
    {
        private const int ITEM_SIZE = 40;
        private static Queue<NearbyItemDisplay> pool = new Queue<NearbyItemDisplay>();
        private Label itemLabel;
        private AlphaBlendControl alphaBG;
        private Item currentItem;
        private int index;
        private bool highlight = false;
        private ushort borderHighlightHue = 0;
        private readonly Texture2D borderTexture;
        private World world;

        private ushort bgHue
        {
            get
            {
                if (AutoLootManager.Instance.IsBeingLooted(LocalSerial))
                    return 32;

                if (NearbyLootGump.SelectedIndex == index)
                    return 53;

                return 0;
            }
        }
        public static NearbyItemDisplay GetOne(World world, Item item, int index)
        {
            NearbyItemDisplay nearbyItemDisplay = pool.Count > 0 ? pool.Dequeue() : new NearbyItemDisplay(world, item, index);

            if (nearbyItemDisplay.IsDisposed)
                return new NearbyItemDisplay(world, item, index);

            nearbyItemDisplay.SetItem(item, index);
            return nearbyItemDisplay;
        }

        public NearbyItemDisplay(World world, Item item, int index)
        {
            if (item == null)
            {
                Dispose();
                return;
            }
            this.world = world;

            borderTexture = SolidColorTextureCache.GetTexture(Color.White);
            CanMove = true;
            AcceptMouseInput = true;
            Width = NearbyLootGump.WIDTH;
            Height = ITEM_SIZE;
            this.index = index;

            Add(alphaBG = new AlphaBlendControl() { Width = Width, Height = Height, Hue = bgHue });

            SetItem(item, index);
        }

        public void SetItem(Item item, int index)
        {
            highlight = false;
            currentItem = item;
            this.index = index;
            if (item == null) return;

            LocalSerial = item.Serial;

            alphaBG.Hue = bgHue; //Prevent weird flashing

            string name = item.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = StringHelper.CapitalizeAllWords(
                            StringHelper.GetPluralAdjustedString(
                                item.ItemData.Name,
                                item.Amount > 1
                            )
                        );
            }

            if (itemLabel == null)
            {
                Add(itemLabel = new Label(name, true, 43, ishtml: true) { X = ITEM_SIZE });
                itemLabel.Y = (ITEM_SIZE - itemLabel.Height) >> 1;
            }
            else
            {
                itemLabel.Text = name;
            }

            ItemPropertiesData data = new ItemPropertiesData(world, item);
            foreach (GridHighlightData config in GridHighlightData.AllConfigs)
            {
                if (config.IsMatch(data))
                {
                    highlight = true;
                    borderHighlightHue = config.Hue;
                }
            }

            SetTooltip(item);
        }

        public void ReturnToPool()
        {
            pool.Enqueue(this);
        }

        public override void Update()
        {
            base.Update();
            if (alphaBG.Hue != bgHue)
                alphaBG.Hue = bgHue;
        }

        protected override void OnMouseEnter(int x, int y)
        {
            base.OnMouseEnter(x, y);
            NearbyLootGump.SelectedIndex = index;
        }

        protected override void OnDragBegin(int x, int y)
        {
            base.OnDragBegin(x, y);
            Parent?.InvokeDragBegin(new Point(x, y));
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            Parent?.InvokeDragEnd(new Point(x, y));
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button != MouseButtonType.Left || !MouseIsOver) return;

            if (Keyboard.Shift && currentItem != null && ProfileManager.CurrentProfile.EnableAutoLoot && !ProfileManager.CurrentProfile.HoldShiftForContext && !ProfileManager.CurrentProfile.HoldShiftToSplitStack)
            {
                AutoLootManager.Instance.AddAutoLootEntry(currentItem.Graphic, currentItem.Hue, currentItem.Name);
                GameActions.Print(world, $"Added this item to auto loot.");
            }

            AutoLootManager.Instance.LootItem(LocalSerial);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(currentItem.Hue, currentItem.ItemData.IsPartialHue, 1, true);

            ref readonly var texture = ref Client.Game.UO.Arts.GetArt((uint)currentItem.DisplayedGraphic);
            Rectangle _rect = Client.Game.UO.Arts.GetRealArtBounds((uint)currentItem.DisplayedGraphic);


            Point _originalSize = new Point(ITEM_SIZE, ITEM_SIZE);
            Point _point = new Point((ITEM_SIZE >> 1) - (_originalSize.X >> 1), (ITEM_SIZE >> 1) - (_originalSize.Y >> 1));

            if (texture.Texture != null)
            {
                if (_rect.Width < ITEM_SIZE)
                {
                    _originalSize.X = _rect.Width;
                    _point.X = (ITEM_SIZE >> 1) - (_originalSize.X >> 1);
                }

                if (_rect.Height < ITEM_SIZE)
                {
                    _originalSize.Y = _rect.Height;
                    _point.Y = (ITEM_SIZE >> 1) - (_originalSize.Y >> 1);
                }

                if (_rect.Width > ITEM_SIZE)
                {
                    _originalSize.X = ITEM_SIZE;
                    _point.X = 0;
                }

                if (_rect.Height > ITEM_SIZE)
                {
                    _originalSize.Y = ITEM_SIZE;
                    _point.Y = 0;
                }

                batcher.Draw
                (
                    texture.Texture,
                    new Rectangle
                    (
                        x + _point.X,
                        y + _point.Y,
                        _originalSize.X,
                        _originalSize.Y
                    ),
                    new Rectangle
                    (
                        texture.UV.X + _rect.X,
                        texture.UV.Y + _rect.Y,
                        _rect.Width,
                        _rect.Height
                    ),
                    hueVector
                );
            }

            if (highlight)
            {
                int bx = x + 6;
                int by = y + 6;

                Vector3 borderHueVec = ShaderHueTranslator.GetHueVector(borderHighlightHue, false, 0.8f);

                batcher.Draw( //Top bar
                    borderTexture,
                    new Rectangle(bx, by, ITEM_SIZE - 12, 1),
                    borderHueVec
                    );

                batcher.Draw( //Left Bar
                    borderTexture,
                    new Rectangle(bx, by + 1, 1, ITEM_SIZE - 10),
                    borderHueVec
                    );

                batcher.Draw( //Right Bar
                    borderTexture,
                    new Rectangle(bx + ITEM_SIZE - 12 - 1, by + 1, 1, ITEM_SIZE - 10),
                    borderHueVec
                    );

                batcher.Draw( //Bottom bar
                    borderTexture,
                    new Rectangle(bx, by + ITEM_SIZE - 11, ITEM_SIZE - 12, 1),
                    borderHueVec
                    );
            }

            return true;
        }
    }
}
