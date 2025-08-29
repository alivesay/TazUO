using ImGuiNET;
using System;

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
    }
}