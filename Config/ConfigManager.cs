using System;
using System.IO;
using Newtonsoft.Json;

namespace SteamAutoLauncher.Config
{
    public static class ConfigManager
    {
        private const string ConfigFileName = "config.json";

        public static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigFileName))
            {
                throw new FileNotFoundException($"Configuration file '{ConfigFileName}' not found. Please create it first.");
            }

            try
            {
                var json = File.ReadAllText(ConfigFileName);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                return config ?? new AppConfig();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse configuration file: {ex.Message}", ex);
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFileName, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration file: {ex.Message}", ex);
            }
        }

        public static void CreateDefaultConfig()
        {
            var defaultConfig = new AppConfig
            {
                Accounts = new()
                {
                    new AccountInfo
                    {
                        Label = "Account 1",
                        Login = "username1",
                        Password = "password1",
                        MaFilePath = "./mafiles/username1.maFile"
                    },
                    new AccountInfo
                    {
                        Label = "Account 2",
                        Login = "username2",
                        Password = "password2",
                        MaFilePath = "./mafiles/username2.maFile"
                    }
                },
                Game = new GameConfig
                {
                    AppId = 3419430,
                    GamePath = "C:\\Games\\BongoCat\\BongoCat.exe",
                    LaunchMethod = "appid"
                },
                Delays = new DelaysConfig
                {
                    WaitSteamStartMs = 5000,
                    WaitSteamLoginMs = 15000,
                    GamePlayTimeMs = 20000,
                    WaitGameStartMs = 3000,
                    WaitGameExitMs = 2000,
                    WaitSteamExitMs = 5000,
                    BetweenAccountsMs = 3000
                },
                Settings = new SettingsConfig
                {
                    SteamExePath = "C:\\Program Files (x86)\\Steam\\steam.exe",
                    UIAutomationTimeoutMs = 10000,
                    MaxRetries = 3
                }
            };

            SaveConfig(defaultConfig);
        }
    }
}