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
                Logger.LogInfo("[AccountCycler] Starting account cycle");
                UpdateStatus("Cycle started");

                while (!cancellationToken.IsCancellationRequested)
                {
                    _cycleCount++;
                    Logger.LogInfo($"[AccountCycler] --- Cycle #{_cycleCount} started ---");
                    UpdateStatus($"Cycle #{_cycleCount}");

                    foreach (var account in _config.Accounts)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await ProcessAccountAsync(account, cancellationToken);
                    }

                    Logger.LogInfo($"[AccountCycler] --- Cycle #{_cycleCount} completed ---");
                    OnCycleCompleted?.Invoke(this, EventArgs.Empty);
                    
                    // Small delay before next cycle
                    await Task.Delay(2000, cancellationToken);
                }

                Logger.LogInfo("[AccountCycler] Account cycling stopped");
                UpdateStatus("Stopped");
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo("[AccountCycler] Account cycling cancelled");
                UpdateStatus("Cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AccountCycler] Unexpected error in account cycle: {ex.GetType().Name} - {ex.Message}");
                Logger.LogError($"[AccountCycler] Stack trace: {ex.StackTrace}");
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private async Task ProcessAccountAsync(AccountInfo account, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInfo($"[AccountCycler] ========== PROCESSING ACCOUNT START ==========");
                Logger.LogInfo($"[AccountCycler] Processing account: {account.Label} ({account.Login})");
                UpdateStatus($"Processing: {account.Label}");

                // Extract shared secret from MaFile
                Logger.LogInfo($"[AccountCycler] Step 0/5: Extracting shared secret");
                string? sharedSecret = null;
                
                try
                {
                    sharedSecret = MaFileParser.ExtractSharedSecret(account.MaFilePath);
                    Logger.LogSuccess($"[AccountCycler] Shared secret extracted successfully");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[AccountCycler] CRITICAL: Failed to extract shared secret: {ex.Message}");
                    Logger.LogError($"[AccountCycler] Skipping this account due to MaFile error");
                    return;
                }

                // 1. Launch Steam
                Logger.LogInfo($"[AccountCycler] Step 1/5: Launching Steam");
                UpdateStatus($"{account.Label} - Launching Steam");
                
                bool steamLaunched = false;
                try
                {
                    steamLaunched = await _steamManager.LaunchSteamAsync(account.Login, account.Password);
                    if (!steamLaunched)
                    {
                        Logger.LogError("[AccountCycler] CRITICAL: Failed to launch Steam");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[AccountCycler] CRITICAL: Exception launching Steam: {ex.Message}");
                    return;
                }

                await Task.Delay(_config.Delays.WaitSteamStartMs, cancellationToken);
                Logger.LogSuccess($"[AccountCycler] Steam started successfully");

                // 2. Generate and enter Steam Guard code
                Logger.LogInfo($"[AccountCycler] Step 2/5: Entering Steam Guard code");
                UpdateStatus($"{account.Label} - Entering 2FA code");
                
                try
                {
                    var code = SteamGuardGenerator.GenerateCodeNow(sharedSecret);
                    Logger.LogInfo($"[AccountCycler] Generated Steam Guard code: {code}");

                    // Try UI Automation first
                    bool codeEntered = await _uiHelper.EnterSteamGuardCodeAsync(code);
                    if (codeEntered)
                    {
                        Logger.LogSuccess("[AccountCycler] Code entered via UI Automation");
                    }
                    else
                    {
                        Logger.LogWarning("[AccountCycler] UI Automation failed - code may need manual entry or clipboard fallback");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[AccountCycler] Error generating/entering code: {ex.Message}");
                }

                await Task.Delay(_config.Delays.WaitSteamLoginMs, cancellationToken);
                Logger.LogSuccess("[AccountCycler] Steam Guard code processed");

                // 3. Wait for Steam to fully login
                Logger.LogInfo($"[AccountCycler] Step 3/5: Waiting for Steam to fully login");
                UpdateStatus($"{account.Label} - Waiting for login");
                await Task.Delay(2000, cancellationToken);
                Logger.LogSuccess("[AccountCycler] Login wait complete");

                // 4. Launch Game
                Logger.LogInfo($"[AccountCycler] Step 4/5: Launching game");
                UpdateStatus($"{account.Label} - Launching game");
                
                bool gameLaunched = false;
                try
                {
                    gameLaunched = await _gameLauncher.LaunchGameAsync();
                    if (!gameLaunched)
                    {
                        Logger.LogError("[AccountCycler] CRITICAL: Failed to launch game");
                        await _steamManager.KillSteamAsync();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[AccountCycler] CRITICAL: Exception launching game: {ex.Message}");
                    await _steamManager.KillSteamAsync();
                    return;
                }

                Logger.LogSuccess("[AccountCycler] Game launched successfully");

                // Play the game for specified time
                Logger.LogInfo($"[AccountCycler] Playing game for {_config.Delays.GamePlayTimeMs}ms");
                UpdateStatus($"{account.Label} - Playing game");
                await Task.Delay(_config.Delays.GamePlayTimeMs, cancellationToken);
                Logger.LogSuccess("[AccountCycler] Game play time completed");

                // 5. Exit Game and Steam
                Logger.LogInfo($"[AccountCycler] Step 5/5: Exiting game and Steam");
                UpdateStatus($"{account.Label} - Exiting");
                
                try
                {
                    await _gameLauncher.TerminateGameAsync();
                    await Task.Delay(_config.Delays.WaitGameExitMs, cancellationToken);

                    await _steamManager.KillSteamAsync();
                    await Task.Delay(_config.Delays.WaitSteamExitMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[AccountCycler] Error during cleanup: {ex.Message}");
                }

                Logger.LogSuccess($"[AccountCycler] Account {account.Label} cycle completed successfully");
                Logger.LogInfo($"[AccountCycler] ========== PROCESSING ACCOUNT END ==========\n");

                // Wait before next account
                if (_config.Accounts.IndexOf(account) < _config.Accounts.Count - 1)
                {
                    Logger.LogInfo($"[AccountCycler] Waiting {_config.Delays.BetweenAccountsMs}ms before next account");
                    await Task.Delay(_config.Delays.BetweenAccountsMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo("[AccountCycler] Account processing cancelled");
                await _steamManager.KillSteamAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AccountCycler] CRITICAL ERROR processing account {account.Label}: {ex.GetType().Name} - {ex.Message}");
                Logger.LogError($"[AccountCycler] Stack trace: {ex.StackTrace}");
                Logger.LogInfo($"[AccountCycler] ========== PROCESSING ACCOUNT END (ERROR) ==========\n");
                
                try
                {
                    await _steamManager.KillSteamAsync();
                }
                catch { }
            }
        }

        private void UpdateStatus(string status)
        {
            OnStatusChanged?.Invoke(this, status);
        }

        public int GetCycleCount() => _cycleCount;
    }
}
