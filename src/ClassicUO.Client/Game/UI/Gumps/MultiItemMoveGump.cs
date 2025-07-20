using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using System.Collections.Concurrent;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MultiItemMoveGump : Gump
    {
        private Label label;

        public static ConcurrentQueue<Item> MoveItems = new ConcurrentQueue<Item>();

        public static int ObjDelay = 1000;

        private static bool processing;
        private static ProcessType processType;
        private static long nextMove;
        private static uint tradeId, containerId;
        private static int groundX, groundY, groundZ;

        public MultiItemMoveGump(int x, int y) : base(0, 0)
        {
            Width = 200;
            Height = 105;

            X = x < 0 ? 0 : x;
            Y = y < 0 ? 0 : y;
            SetInScreen();

            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            ObjDelay = ProfileManager.CurrentProfile.MoveMultiObjectDelay;

            Add(new AlphaBlendControl(0.75f) { Width = Width, Height = Height });

            Add(label = new Label($"Moving {MoveItems.Count} items.", true, 0xff, Width, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));

            Add(new Label($"Object delay:", true, 0xff, 150) { Y = label.Height + 5 });
            StbTextBox delay;
            Add(delay = new StbTextBox(0xFF, 3000, 50, true, FontStyle.None, 0x048)
            {
                X = 150,
                Y = label.Height + 5,
                Width = 50,
                Height = 20,
                Multiline = false,
                NumbersOnly = true,
            });
            delay.SetText(ObjDelay.ToString());
            delay.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = delay.Width,
                Height = delay.Height
            });
            delay.TextChanged += (s, e) =>
            {
                if (int.TryParse(delay.Text, out int newDelay))
                {
                    ObjDelay = newDelay;
                    ProfileManager.CurrentProfile.MoveMultiObjectDelay = newDelay;
                }
            };

            NiceButton moveToBackpack;
            Add(moveToBackpack = new NiceButton(0, Height - 60, Width, 20, ButtonAction.Default, "Move to backpack", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            moveToBackpack.SetTooltip("Move selected items to your backpack.");
            moveToBackpack.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    delay.IsEditable = false;
                    processItemMoves(World.Player.FindItemByLayer(Data.Layer.Backpack));
                }
            };

            NiceButton setFavorite;
            Add(setFavorite = new NiceButton(0, Height - 40, 100, 20, ButtonAction.Default, "Set favorite bag", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            setFavorite.SetTooltip("Set your preferred destination container for future item moves.");
            setFavorite.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.Print("Target a container to set as your favorite.");
                    TargetManager.SetTargeting(CursorTarget.SetFavoriteMoveBag, CursorType.Target, TargetType.Neutral);
                    delay.IsEditable = false;
                }
            };

            NiceButton moveToFavorite;
            Add(moveToFavorite = new NiceButton(100, Height - 40, 100, 20, ButtonAction.Default, "To favorite", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            moveToFavorite.SetTooltip("Move selected items to your favorite container.");
            moveToFavorite.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    uint favoriteMoveBag = ProfileManager.CurrentProfile.SetFavoriteMoveBagSerial;
                    if (favoriteMoveBag == 0)
                    {
                        GameActions.Print("No favorite container set. Please target one.");
                        TargetManager.SetTargeting(CursorTarget.SetFavoriteMoveBag, CursorType.Target, TargetType.Neutral);
                        return;
                    }

                    Item container = World.Items.Get(favoriteMoveBag);
                    if (container != null)
                    {
                        delay.IsEditable = false;
                        processItemMoves(container);
                    }
                    else
                    {
                        GameActions.Print("Favorite container is not available.");
                    }
                }
            };

            NiceButton cancel;
            Add(cancel = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, "Cancel", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            cancel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    MoveItems = new ConcurrentQueue<Item>();
                    cancel.Dispose();
                }
            };

            NiceButton move;
            Add(move = new NiceButton(100, Height - 20, 100, 20, ButtonAction.Default, "Move to", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            move.SetTooltip("Select a container or a ground tile to move these items to.");
            move.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.Print("Where should we move these items?");
                    TargetManager.SetTargeting(CursorTarget.MoveItemContainer, CursorType.Target, TargetType.Neutral);
                    delay.IsEditable = false;
                }
            };

            Add(new SimpleBorder() { Width = Width, Height = Height, Alpha = 0.75f });
        }

        public static void OnContainerTarget(uint serial)
        {
            if (SerialHelper.IsItem(serial))
            {
                Item moveToContainer = World.Items.Get(serial);
                if (!moveToContainer.ItemData.IsContainer)
                {
                    GameActions.Print("That does not appear to be a container...");
                    return;
                }
                GameActions.Print("Moving items to the selected container..");
                processItemMoves(moveToContainer);
            }
        }
        public static void OnContainerTarget(int x, int y, int z)
        {
            processItemMoves(x, y, z);
        }
        public static void OnTradeWindowTarget(uint tradeID)
        {
            processItemMoves(tradeID);
        }

        public static void AddMultiItemMoveGumpToUI(int x, int y)
        {
            if (MoveItems.Count > 0)
            {
                Gump moveItemGump = UIManager.GetGump<MultiItemMoveGump>();
                if (moveItemGump == null)
                    UIManager.Add(new MultiItemMoveGump(x, y));
            }
        }

        private static void processItemMoves(Item container)
        {
            if (container != null)
            {
                containerId = container.Serial;
                processType = ProcessType.Container;
                processing = true;
            }
        }

        private static void processItemMoves(int x, int y, int z)
        {
            processType = ProcessType.Ground;
            groundX = x;
            groundY = y;
            groundZ = z;
            processing = true;
        }

        private static void processItemMoves(uint tradeID)
        {
            tradeId = tradeID;
            processType = ProcessType.TradeWindow;
            processing = true;
        }

        public override void Update()
        {
            base.Update();

            if (!processing)
                return;
            
            if (Time.Ticks < nextMove)
                return;
            
            if (Client.Game.GameCursor.ItemHold.Enabled)
                return;
            
            if(MoveItems.TryDequeue(out Item moveItem))
            {
                switch (processType)
                {
                    case ProcessType.Ground: 
                        Assets.StaticTiles itemData = Assets.TileDataLoader.Instance.StaticData[moveItem.Graphic];
                        MoveItemQueue.Instance.Enqueue(moveItem, 0, moveItem.Amount, groundX, groundY, groundZ + (sbyte)(itemData.Height == 0xFF ? 0 : itemData.Height));
                        break;

                    case ProcessType.Container:
                        MoveItemQueue.Instance.Enqueue(moveItem, containerId, moveItem.Amount);
                        break;
                    
                    case ProcessType.TradeWindow:
                        MoveItemQueue.Instance.Enqueue(moveItem, tradeId, moveItem.Amount, RandomHelper.GetValue(0, 20), RandomHelper.GetValue(0, 20), 0);
                        break;
                }
                
                nextMove = Time.Ticks + ObjDelay;
            }
            
            if(MoveItems.Count < 1)//No more items left
            {
                processing = false;
            }
            
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (MoveItems.Count < 1)
                Dispose();

            label.Text = $"Moving {MoveItems.Count} items.";

            return base.Draw(batcher, x, y);
        }
        
        protected enum ProcessType
        {
            Container,
            Ground,
            TradeWindow
        }
    }
}
