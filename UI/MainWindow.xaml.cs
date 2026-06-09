using System;
using System.Collections.ObjectModel;
using System.Windows;
using SteamAutoLauncher.Config;
using SteamAutoLauncher.Core;
using SteamAutoLauncher.Core.Logging;

namespace SteamAutoLauncher.UI
{
    public partial class MainWindow : Window
    {
        private AccountCycler? _cycler;
        private AppConfig? _config;
        private ObservableCollection<AccountInfo> _accountsCollection = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load configuration
                _config = ConfigManager.LoadConfig();
                
                // Setup UI
                AccountsList.ItemsSource = _accountsCollection;
                foreach (var account in _config.Accounts)
                {
                    _accountsCollection.Add(account);
                }

                // Display game info
                GameSettingLabel.Text = _config.Game.LaunchMethod == "appid" 
                    ? $"AppID: {_config.Game.AppId}"
                    : $"Path: {_config.Game.GamePath}";

                // Initialize cycler
                _cycler = new AccountCycler(_config);
                _cycler.OnStatusChanged += Cycler_OnStatusChanged;
                _cycler.OnCycleCompleted += Cycler_OnCycleCompleted;

                // Redirect logs to UI
                var originalAction = LogToUI;
                Logger.LogInfo("Application started - ready to begin cycles");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cycler == null)
                {
                    MessageBox.Show("Cycler not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Logger.LogInfo("Starting automation cycle");
                _cycler.Start();
                
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                StatusLabel.Text = "Running...";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Yellow;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error starting cycler: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cycler == null) return;

                Logger.LogInfo("Stopping automation cycle");
                _cycler.Stop();
                
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                StatusLabel.Text = "Stopped";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping cycler: {ex.Message}");
            }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Text = "";
        }

        private void Cycler_OnStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = status;
                LogToUI($"[STATUS] {status}");
            });
        }

        private void Cycler_OnCycleCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_cycler != null)
                {
                    CycleLabel.Text = $"Cycles: {_cycler.GetCycleCount()}";
                }
                LogToUI("[INFO] Cycle completed, restarting...");
            });
        }

        private void LogToUI(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.Text += message + Environment.NewLine;
                
                // Auto-scroll to bottom
                if (LogBox.Parent is ScrollViewer sv)
                {
                    sv.ScrollToEnd();
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _cycler?.Stop();
        }
    }
}