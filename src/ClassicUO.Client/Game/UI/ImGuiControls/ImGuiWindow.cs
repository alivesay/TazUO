using ImGuiNET;
using System;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public abstract class ImGuiWindow : IDisposable
    {
        private bool _isOpen = true;
        private bool _isVisible = true;
        private ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.None;

        protected ImGuiWindow(string title)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        public string Title { get; protected set; }

        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        protected ImGuiWindowFlags WindowFlags
        {
            get => _windowFlags;
            set => _windowFlags = value;
        }

        public void Draw()
        {
            if (!_isVisible || !_isOpen)
                return;

            try
            {
                if (ImGui.Begin(Title, ref _isOpen, _windowFlags))
                {
                    DrawContent();
                }
            }
            catch (Exception ex)
            {
                ImGui.Text($"Error in window '{Title}': {ex.Message}");
            }
            finally
            {
                ImGui.End();
            }
        }

        protected abstract void DrawContent();

        protected virtual void OnWindowClosed()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Dispose()
        {
            OnWindowClosed();
        }

        protected void SetTooltip(string tooltip)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        protected bool DrawArt(ushort graphic, Vector2 size, bool useSmallerIfGfxSmaller = true)
        {
            var artInfo = Client.Game.UO.Arts.GetArt(graphic);

            if(artInfo.Texture != null)
            {
                var uv0 = new Vector2(artInfo.UV.X / (float)artInfo.Texture.Width, artInfo.UV.Y / (float)artInfo.Texture.Height);
                var uv1 = new Vector2((artInfo.UV.X + artInfo.UV.Width) / (float)artInfo.Texture.Width, (artInfo.UV.Y + artInfo.UV.Height) / (float)artInfo.Texture.Height);

                if(useSmallerIfGfxSmaller && artInfo.UV.Width < size.X && artInfo.UV.Height < size.Y)
                    size = new Vector2(artInfo.UV.Width, artInfo.UV.Height);

                ImGui.Image(ImGuiManager.Renderer.BindTexture(artInfo.Texture), size, uv0, uv1);
                return true;
            }

            return false;
        }
    }
}
