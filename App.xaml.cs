using System.Configuration;
using System.Data;
using System.Windows;
using AutoUpdaterDotNET;

namespace KenshiModManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AutoUpdater.Mandatory = false;
        AutoUpdater.UpdateMode = Mode.ForcedDownload;
        AutoUpdater.ShowSkipButton = true;
        AutoUpdater.ShowRemindLaterButton = true;
        AutoUpdater.ReportErrors = true;

        string updateXmlUrl = "https://raw.githubusercontent.com/nonniks/KenshiModManager/main/update.xml";

        AutoUpdater.Start(updateXmlUrl);
    }
}

