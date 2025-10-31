using System.Configuration;
using System.Data;
using System.Windows;
using AutoUpdaterDotNET;
using KenshiModManager.Core;

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

        AutoUpdater.Mandatory = false;
        AutoUpdater.UpdateMode = Mode.ForcedDownload;
        AutoUpdater.ShowSkipButton = true;
        AutoUpdater.ShowRemindLaterButton = true;
        AutoUpdater.ReportErrors = true;

        string updateXmlUrl = "https://raw.githubusercontent.com/nonniks/KenshiModManager/master/update.xml";

        AutoUpdater.Start(updateXmlUrl);
    }
}

