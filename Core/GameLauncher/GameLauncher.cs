using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SteamAutoLauncher.Config;
using SteamAutoLauncher.Core.Logging;

namespace SteamAutoLauncher.Core.GameLauncher
{
    public class GameLauncher
    {
        private readonly GameConfig _gameConfig;
        private Process? _gameProcess;

        public GameLauncher(GameConfig gameConfig)
        {
            _gameConfig = gameConfig;
        }

        public async Task<bool> LaunchGameAsync()
        {
            try
            {
                Logger.LogInfo("Launching game...");

                bool success = _gameConfig.LaunchMethod switch
                {
                    "appid" => await LaunchByAppIdAsync(),
                    "path" => await LaunchByPathAsync(),
                    _ => throw new InvalidOperationException($"Unknown launch method: {_gameConfig.LaunchMethod}")
                };

                return success;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error launching game: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LaunchByAppIdAsync()
        {
            try
            {
                Logger.LogInfo($"Launching game by AppID: {_gameConfig.AppId}");

                var steamUrl = $"steam://run/{_gameConfig.AppId}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = steamUrl,
                    UseShellExecute = true
                });

                await Task.Delay(2000);
                Logger.LogSuccess("Game launch command sent via Steam protocol");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error launching game by AppID: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LaunchByPathAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_gameConfig.GamePath))
                {
                    Logger.LogError("Game path not configured");
                    return false;
                }

                Logger.LogInfo($"Launching game by path: {_gameConfig.GamePath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = _gameConfig.GamePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _gameProcess = Process.Start(startInfo);

                if (_gameProcess == null)
                {
                    Logger.LogError("Failed to start game process");
                    return false;
                }

                Logger.LogSuccess($"Game process started (PID: {_gameProcess.Id})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error launching game by path: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TerminateGameAsync()
        {
            try
            {
                Logger.LogInfo("Terminating game...");

                if (_gameConfig.LaunchMethod == "path" && _gameProcess != null)
                {
                    try
                    {
                        _gameProcess.Kill();
                        Logger.LogSuccess("Game process killed");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error killing game process: {ex.Message}");
                    }
                }

                // For AppID-based launch, process is typically managed by Steam
                Logger.LogInfo("Game termination complete");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error terminating game: {ex.Message}");
                return false;
            }
        }

        public bool IsGameRunning()
        {
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                return true;
            }

            return false;
        }
    }
}