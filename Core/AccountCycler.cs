using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamAutoLauncher.Config;
using SteamAutoLauncher.Core.GameLauncher;
using SteamAutoLauncher.Core.Logging;
using SteamAutoLauncher.Core.SteamClient;
using SteamAutoLauncher.Core.SteamGuard;

namespace SteamAutoLauncher.Core
{
    public class AccountCycler
    {
        private readonly AppConfig _config;
        private readonly SteamProcessManager _steamManager;
        private readonly GameLauncher.GameLauncher _gameLauncher;
        private readonly UIAutomationHelper _uiHelper;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _cycleCount = 0;

        public event EventHandler<string>? OnStatusChanged;
        public event EventHandler? OnCycleCompleted;

        public AccountCycler(AppConfig config)
        {
            _config = config;
            _steamManager = new SteamProcessManager(config.Settings.SteamExePath);
            _gameLauncher = new GameLauncher.GameLauncher(config.Game);
            _uiHelper = new UIAutomationHelper();
        }

        public void Start()
        {
            if (_cancellationTokenSource != null)
            {
                Logger.LogWarning("Cycler is already running");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _ = RunCycleAsync(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                Logger.LogInfo("Stopping account cycler");
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }

        private async Task RunCycleAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInfo("Starting account cycle");
                UpdateStatus("Cycle started");

                while (!cancellationToken.IsCancellationRequested)
                {
                    _cycleCount++;
                    Logger.LogInfo($"--- Cycle #{_cycleCount} started ---");
                    UpdateStatus($"Cycle #{_cycleCount}");

                    foreach (var account in _config.Accounts)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await ProcessAccountAsync(account, cancellationToken);
                    }

                    Logger.LogInfo($"--- Cycle #{_cycleCount} completed ---");
                    OnCycleCompleted?.Invoke(this, EventArgs.Empty);
                }

                Logger.LogInfo("Account cycling stopped");
                UpdateStatus("Stopped");
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo("Account cycling cancelled");
                UpdateStatus("Cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error in account cycle: {ex.Message}");
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private async Task ProcessAccountAsync(AccountInfo account, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInfo($"Processing account: {account.Label} ({account.Login})");
                UpdateStatus($"Processing: {account.Label}");

                // Extract shared secret from MaFile
                Logger.LogInfo($"Extracting shared secret from: {account.MaFilePath}");
                var sharedSecret = MaFileParser.ExtractSharedSecret(account.MaFilePath);
                Logger.LogSuccess("Shared secret extracted");

                // 1. Launch Steam
                Logger.LogInfo("Step 1/5: Launching Steam");
                UpdateStatus($"{account.Label} - Launching Steam");
                
                if (!await _steamManager.LaunchSteamAsync(account.Login, account.Password))
                {
                    Logger.LogError("Failed to launch Steam");
                    return;
                }

                await Task.Delay(_config.Delays.WaitSteamStartMs, cancellationToken);
                Logger.LogSuccess("Steam started");

                // 2. Generate and enter Steam Guard code
                Logger.LogInfo("Step 2/5: Entering Steam Guard code");
                UpdateStatus($"{account.Label} - Entering 2FA code");
                
                var code = SteamGuardGenerator.GenerateCodeNow(sharedSecret);
                Logger.LogInfo($"Generated Steam Guard code: {code}");

                // Try UI Automation first
                bool codeEntered = await _uiHelper.EnterSteamGuardCodeAsync(code);
                if (!codeEntered)
                {
                    Logger.LogWarning("UI Automation failed, code will need to be entered manually or will timeout");
                }

                await Task.Delay(_config.Delays.WaitSteamLoginMs, cancellationToken);
                Logger.LogSuccess("Steam Guard code processed");

                // 3. Wait for Steam to fully login
                Logger.LogInfo("Step 3/5: Waiting for Steam to fully login");
                UpdateStatus($"{account.Label} - Waiting for login");
                await Task.Delay(2000, cancellationToken);

                // 4. Launch Game
                Logger.LogInfo("Step 4/5: Launching game");
                UpdateStatus($"{account.Label} - Launching game");
                
                if (!await _gameLauncher.LaunchGameAsync())
                {
                    Logger.LogError("Failed to launch game");
                    await _steamManager.KillSteamAsync();
                    return;
                }

                // Play the game for specified time
                Logger.LogInfo($"Playing game for {_config.Delays.GamePlayTimeMs}ms");
                UpdateStatus($"{account.Label} - Playing game");
                await Task.Delay(_config.Delays.GamePlayTimeMs, cancellationToken);
                Logger.LogSuccess("Game play time completed");

                // 5. Exit Game and Steam
                Logger.LogInfo("Step 5/5: Exiting game and Steam");
                UpdateStatus($"{account.Label} - Exiting");
                
                await _gameLauncher.TerminateGameAsync();
                await Task.Delay(_config.Delays.WaitGameExitMs, cancellationToken);

                await _steamManager.KillSteamAsync();
                await Task.Delay(_config.Delays.WaitSteamExitMs, cancellationToken);

                Logger.LogSuccess($"Account {account.Label} cycle completed");

                // Wait before next account
                if (_config.Accounts.IndexOf(account) < _config.Accounts.Count - 1)
                {
                    Logger.LogInfo($"Waiting {_config.Delays.BetweenAccountsMs}ms before next account");
                    await Task.Delay(_config.Delays.BetweenAccountsMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo("Account processing cancelled");
                await _steamManager.KillSteamAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing account {account.Label}: {ex.Message}");
                await _steamManager.KillSteamAsync();
            }
        }

        private void UpdateStatus(string status)
        {
            OnStatusChanged?.Invoke(this, status);
        }

        public int GetCycleCount() => _cycleCount;
    }
}