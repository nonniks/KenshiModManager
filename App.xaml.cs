using System.Configuration;
using System.Data;
using System.Windows;
using AutoUpdaterDotNET;
using KenshiModManager.Core;
using KenshiModManager.ViewModels;
using KenshiModManager.Views;

namespace KenshiModManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize localization before any UI is shown
        var settings = AppSettings.Load();
        LocalizationManager.Initialize(settings);

        // Configure AutoUpdater for silent background checks with custom UI
        AutoUpdater.Mandatory = false;
        AutoUpdater.UpdateMode = Mode.ForcedDownload;
        AutoUpdater.ShowSkipButton = false;
        AutoUpdater.ShowRemindLaterButton = false;
        AutoUpdater.ReportErrors = false;

        // Handle update check results - show custom dialog if update is available
        AutoUpdater.CheckForUpdateEventHandler silentCheckHandler = (args) =>
        {
            if (args.Error != null)
            {
                Console.WriteLine($"[AutoUpdater] Error checking for updates: {args.Error.Message}");
                return;
            }

            if (args.IsUpdateAvailable)
            {
                // Update found - show our custom dialog
                Console.WriteLine($"[AutoUpdater] Update available: {args.CurrentVersion} -> {args.InstalledVersion}");

                // Show custom update dialog on UI thread
                Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var dialog = new UpdateDialogWindow();
                        dialog.DataContext = new UpdateDialogViewModel(
                            args.CurrentVersion?.ToString() ?? "Unknown",
                            args.InstalledVersion?.ToString() ?? "Unknown",
                            args.DownloadURL
                        );

                        if (Current.MainWindow != null && Current.MainWindow.IsLoaded)
                        {
                            dialog.Owner = Current.MainWindow;
                        }

                        dialog.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AutoUpdater] Error showing update dialog: {ex.Message}");
                    }
                });
            }
            else
            {
                Console.WriteLine("[AutoUpdater] No update available (silent check)");
            }
        };

        AutoUpdater.CheckForUpdateEvent += silentCheckHandler;

        string updateXmlUrl = "https://raw.githubusercontent.com/nonniks/KenshiModManager/master/update.xml";

        AutoUpdater.Start(updateXmlUrl);
    }
}

