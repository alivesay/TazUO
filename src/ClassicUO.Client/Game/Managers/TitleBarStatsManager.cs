using ClassicUO.Configuration;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game.Managers
{
    public static class TitleBarStatsManager
    {
        private static uint _lastUpdate = 0;
        private static ushort _lastHits = 0;
        private static ushort _lastHitsMax = 0;
        private static ushort _lastMana = 0;
        private static ushort _lastManaMax = 0;
        private static ushort _lastStamina = 0;
        private static ushort _lastStaminaMax = 0;

        public static void UpdateTitleBar()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return;
            }

            if (!ProfileManager.CurrentProfile.EnableTitleBarStats || World.Instance.Player == null)
            {
                return;
            }

            uint currentTime = (uint)Time.Ticks;
            uint delta = currentTime - _lastUpdate;
            if (delta < (uint)ProfileManager.CurrentProfile.TitleBarUpdateInterval)
            {
                return;
            }

            // Check if stats have changed
            if (World.Instance.Player.Hits == _lastHits &&
                World.Instance.Player.HitsMax == _lastHitsMax &&
                World.Instance.Player.Mana == _lastMana &&
                World.Instance.Player.ManaMax == _lastManaMax &&
                World.Instance.Player.Stamina == _lastStamina &&
                World.Instance.Player.StaminaMax == _lastStaminaMax)
            {
                return;
            }

            // Update cached values
            _lastHits = World.Instance.Player.Hits;
            _lastHitsMax = World.Instance.Player.HitsMax;
            _lastMana = World.Instance.Player.Mana;
            _lastManaMax = World.Instance.Player.ManaMax;
            _lastStamina = World.Instance.Player.Stamina;
            _lastStaminaMax = World.Instance.Player.StaminaMax;
            _lastUpdate = currentTime;

            string statsText = GenerateStatsText();
            string title = string.IsNullOrEmpty(World.Instance.Player.Name) ?
                statsText :
                $"{World.Instance.Player.Name} - {statsText}";

            Client.Game.SetWindowTitle(title);
        }

        private static string GenerateStatsText()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return string.Empty;
            }

            switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
            {
                case TitleBarStatsMode.Text:
                    return $"HP {World.Instance.Player.Hits}/{World.Instance.Player.HitsMax}, MP {World.Instance.Player.Mana}/{World.Instance.Player.ManaMax}, SP {World.Instance.Player.Stamina}/{World.Instance.Player.StaminaMax}";

                case TitleBarStatsMode.Percent:
                    int hpPercent = World.Instance.Player.HitsMax > 0 ? (World.Instance.Player.Hits * 100) / World.Instance.Player.HitsMax : 0;
                    int mpPercent = World.Instance.Player.ManaMax > 0 ? (World.Instance.Player.Mana * 100) / World.Instance.Player.ManaMax : 0;
                    int spPercent = World.Instance.Player.StaminaMax > 0 ? (World.Instance.Player.Stamina * 100) / World.Instance.Player.StaminaMax : 0;
                    return $"HP {hpPercent}%, MP {mpPercent}%, SP {spPercent}%";

                case TitleBarStatsMode.ProgressBar:
                    string hpBar = GenerateProgressBar(World.Instance.Player.Hits, World.Instance.Player.HitsMax);
                    string mpBar = GenerateProgressBar(World.Instance.Player.Mana, World.Instance.Player.ManaMax);
                    string spBar = GenerateProgressBar(World.Instance.Player.Stamina, World.Instance.Player.StaminaMax);
                    return $"HP {hpBar} MP {mpBar} SP {spBar}";

                default:
                    return $"HP {World.Instance.Player.Hits}/{World.Instance.Player.HitsMax}, MP {World.Instance.Player.Mana}/{World.Instance.Player.ManaMax}, SP {World.Instance.Player.Stamina}/{World.Instance.Player.StaminaMax}"; // Fallback to text mode
            }
        }

        private static string GenerateProgressBar(ushort current, ushort max)
        {
            const int barLength = 8;
            const char fullBlock = '█';
            const char partialBlock = '▓';
            const char emptyBlock = '░';

            if (max == 0)
                return new string(emptyBlock, barLength);

            float percentage = (float)current / max;
            int filledBlocks = (int)Math.Floor(percentage * barLength);
            bool hasPartial = (percentage * barLength) - filledBlocks > 0.5f;

            string result = "";

            // Add full blocks
            for (int i = 0; i < filledBlocks; i++)
            {
                result += fullBlock;
            }

            // Add partial block if needed
            if (hasPartial && filledBlocks < barLength)
            {
                result += partialBlock;
                filledBlocks++;
            }

            // Fill remaining with empty blocks
            while (result.Length < barLength)
            {
                result += emptyBlock;
            }

            return result;
        }

        public static void ForceUpdate()
        {
            _lastUpdate = 0;
            _lastHits = 0;
            _lastHitsMax = 0;
            _lastMana = 0;
            _lastManaMax = 0;
            _lastStamina = 0;
            _lastStaminaMax = 0;
            UpdateTitleBar();
        }

        public static string GetPreviewText()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return string.Empty;
            }

            if (World.Instance.Player == null)
            {
                // Use sample values for preview
                switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
                {
                    case TitleBarStatsMode.Text:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                    case TitleBarStatsMode.Percent:
                        return "PlayerName - HP 85%, MP 84%, SP 95%";
                    case TitleBarStatsMode.ProgressBar:
                        return "PlayerName - HP ██████▓░ MP ██████▓░ SP ███████▓";
                    default:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                }
            }

            string statsText = GenerateStatsText();
            return string.IsNullOrEmpty(World.Instance.Player.Name) ?
                statsText :
                $"{World.Instance.Player.Name} - {statsText}";
        }
    }

    public enum TitleBarStatsMode
    {
        Text = 0,
        Percent = 1,
        ProgressBar = 2
    }
}
