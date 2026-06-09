using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SteamAutoLauncher.Core.Logging;

namespace SteamAutoLauncher.Core.SteamClient
{
    public class SteamProcessManager
    {
        private readonly string _steamExePath;
        private Process? _steamProcess;

        public SteamProcessManager(string steamExePath)
        {
            _steamExePath = steamExePath;
        }

        public async Task<bool> LaunchSteamAsync(string username, string password)
        {
            try
            {
                // Kill any existing Steam process
                await KillSteamAsync();

                Logger.LogInfo($"Launching Steam with account: {username}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _steamExePath,
                    Arguments = $"-login {username} {password}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _steamProcess = Process.Start(startInfo);
                
                if (_steamProcess == null)
                {
                    Logger.LogError("Failed to start Steam process");
                    return false;
                }

                Logger.LogInfo($"Steam process started (PID: {_steamProcess.Id})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error launching Steam: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> KillSteamAsync()
        {
            try
            {
                // Kill all Steam processes
                var processes = Process.GetProcessesByName("steam");
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        Logger.LogInfo($"Killed Steam process (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error killing Steam process: {ex.Message}");
                    }
                }

                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error killing Steam: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsSteamRunningAsync()
        {
            var processes = Process.GetProcessesByName("steam");
            return processes.Length > 0;
        }

        public async Task WaitForSteamExitAsync(int timeoutMs = 15000)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (!await IsSteamRunningAsync())
                {
                    Logger.LogInfo("Steam has exited");
                    return;
                }
                await Task.Delay(500);
            }
            Logger.LogWarning($"Timeout waiting for Steam to exit (waited {timeoutMs}ms)");
        }

        public bool IsProcessRunning(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
    }
}