using System.Collections.Generic;

namespace SteamAutoLauncher.Config
{
    public class AccountInfo
    {
        public string Label { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string MaFilePath { get; set; } = string.Empty;
    }

    public class GameConfig
    {
        public int AppId { get; set; }
        public string? GamePath { get; set; }
        public string LaunchMethod { get; set; } = "appid"; // "appid" или "path"
    }

    public class DelaysConfig
    {
        public int WaitSteamStartMs { get; set; } = 5000;
        public int WaitSteamLoginMs { get; set; } = 15000;
        public int GamePlayTimeMs { get; set; } = 20000;
        public int WaitGameStartMs { get; set; } = 3000;
        public int WaitGameExitMs { get; set; } = 2000;
        public int WaitSteamExitMs { get; set; } = 5000;
        public int BetweenAccountsMs { get; set; } = 3000;
    }

    public class SettingsConfig
    {
        public string SteamExePath { get; set; } = string.Empty;
        public int UIAutomationTimeoutMs { get; set; } = 10000;
        public int MaxRetries { get; set; } = 3;
    }

    public class AppConfig
    {
        public List<AccountInfo> Accounts { get; set; } = new();
        public GameConfig Game { get; set; } = new();
        public DelaysConfig Delays { get; set; } = new();
        public SettingsConfig Settings { get; set; } = new();
    }
}