using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoUpdaterDotNET;
using KenshiModManager.Commands;
using KenshiModManager.Services;

namespace KenshiModManager.ViewModels
{
    /// <summary>
    /// ViewModel for the Update Dialog window
    /// </summary>
    public class UpdateDialogViewModel : ViewModelBase
    {
        private string _currentVersion = string.Empty;
        private string _installedVersion = string.Empty;
        private string _changelog = string.Empty;
        private string _downloadUrl = string.Empty;
        private bool _isLoadingChangelog = false;
        private readonly UpdateInfoEventArgs? _updateArgs;

        public UpdateDialogViewModel(string currentVersion, string installedVersion, string downloadUrl, UpdateInfoEventArgs? updateArgs = null)
        {
            _currentVersion = currentVersion ?? string.Empty;
            _installedVersion = installedVersion ?? string.Empty;
            _changelog = "Loading changelog from GitHub...";
            _downloadUrl = downloadUrl ?? string.Empty;
            _updateArgs = updateArgs;

            DownloadCommand = new RelayCommand(Download, CanDownload);
            SkipCommand = new RelayCommand(Skip);
            RemindLaterCommand = new RelayCommand(RemindLater);

            Console.WriteLine($"[UpdateDialogViewModel] Initialized with version {_currentVersion}");

            // Load changelog asynchronously from GitHub API
            _ = LoadChangelogAsync();
        }

        public bool IsLoadingChangelog
        {
            get => _isLoadingChangelog;
            set => SetProperty(ref _isLoadingChangelog, value);
        }

        private async Task LoadChangelogAsync()
        {
            IsLoadingChangelog = true;

            try
            {
                // Fetch last 5 releases from GitHub API
                Console.WriteLine($"[UpdateDialogViewModel] Fetching last 5 releases from GitHub...");
                var releases = await GitHubReleaseService.GetReleasesAsync(count: 5);

                if (releases != null && releases.Count > 0)
                {
                    // Build combined changelog: Latest + History
                    var changelogBuilder = new StringBuilder();

                    // Latest release section (first in the list)
                    var latestRelease = releases[0];
                    changelogBuilder.AppendLine("╔══════════════════════════════════════════╗");
                    changelogBuilder.AppendLine($"║   📋 What's New in {latestRelease.TagName}");
                    changelogBuilder.AppendLine("╚══════════════════════════════════════════╝");
                    changelogBuilder.AppendLine();
                    changelogBuilder.AppendLine(ExtractChangesSection(latestRelease.Body ?? ""));
                    changelogBuilder.AppendLine();

                    // History section (if there are more releases)
                    if (releases.Count > 1)
                    {
                        changelogBuilder.AppendLine("─────────────────────────────────────────────");
                        changelogBuilder.AppendLine();
                        changelogBuilder.AppendLine("╔══════════════════════════════════════════╗");
                        changelogBuilder.AppendLine("║   📜 Release History");
                        changelogBuilder.AppendLine("╚══════════════════════════════════════════╝");
                        changelogBuilder.AppendLine();

                        // Add other releases
                        foreach (var release in releases.Skip(1))
                        {
                            if (!string.IsNullOrWhiteSpace(release.Body))
                            {
                                var titleLine = release.Body.Split('\n').FirstOrDefault(l => l.StartsWith("###"));
                                if (titleLine != null)
                                {
                                    titleLine = titleLine.Replace("###", "").Trim();
                                }
                                else
                                {
                                    titleLine = $"{release.TagName} - {release.Name}";
                                }

                                changelogBuilder.AppendLine($"● {titleLine}");
                                changelogBuilder.AppendLine(ExtractChangesSection(release.Body));
                                changelogBuilder.AppendLine();
                            }
                        }
                    }

                    Changelog = changelogBuilder.ToString().TrimEnd();
                    Console.WriteLine($"[UpdateDialogViewModel] Successfully loaded changelog ({Changelog.Length} chars) from {releases.Count} releases");
                }
                else
                {
                    // Fallback message if GitHub API fails
                    Changelog = "No changelog available.\n\nVisit the GitHub releases page for more information:\nhttps://github.com/nonniks/KenshiModManager/releases";
                    Console.WriteLine("[UpdateDialogViewModel] No releases found, using fallback");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateDialogViewModel] Error loading changelog: {ex.Message}");
                Changelog = "Unable to load changelog from GitHub.\n\nPlease check your internet connection or visit:\nhttps://github.com/nonniks/KenshiModManager/releases";
            }
            finally
            {
                IsLoadingChangelog = false;
            }
        }

        /// <summary>
        /// Extracts only the "#### Changes:" section from the full release body.
        /// </summary>
        private static string ExtractChangesSection(string releaseBody)
        {
            int changesStart = releaseBody.IndexOf("#### Changes:", StringComparison.OrdinalIgnoreCase);

            if (changesStart < 0)
            {
                Console.WriteLine("[UpdateDialogViewModel] No '#### Changes:' section found, using full body");
                return releaseBody;
            }

            int nextSectionStart = releaseBody.IndexOf("####", changesStart + 14, StringComparison.Ordinal);

            string changesSection;
            if (nextSectionStart > changesStart)
            {
                changesSection = releaseBody[changesStart..nextSectionStart].Trim();
            }
            else
            {
                changesSection = releaseBody[changesStart..].Trim();
            }

            Console.WriteLine($"[UpdateDialogViewModel] Extracted Changes section: {changesSection.Length} chars");
            return changesSection;
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public string InstalledVersion
        {
            get => _installedVersion;
            set => SetProperty(ref _installedVersion, value);
        }

        public string Changelog
        {
            get => _changelog;
            set => SetProperty(ref _changelog, value);
        }

        public string DownloadUrl
        {
            get => _downloadUrl;
            set => SetProperty(ref _downloadUrl, value);
        }

        public ICommand DownloadCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand RemindLaterCommand { get; }

        private bool CanDownload()
        {
            return !string.IsNullOrEmpty(_downloadUrl);
        }

        private void Download()
        {
            try
            {
                Console.WriteLine($"[UpdateDialogViewModel] Starting automatic update...");

                if (_updateArgs != null)
                {
                    Console.WriteLine("[UpdateDialogViewModel] Calling AutoUpdater.DownloadUpdate()");

                    if (AutoUpdater.DownloadUpdate(_updateArgs))
                    {
                        Console.WriteLine("[UpdateDialogViewModel] Update download started successfully");
                        CloseDialog();
                    }
                    else
                    {
                        Console.WriteLine("[UpdateDialogViewModel] AutoUpdater.DownloadUpdate() returned false");
                        MessageBox.Show(
                            "Unable to start automatic update. Please download manually.",
                            "Update Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    Console.WriteLine($"[UpdateDialogViewModel] UpdateArgs not available, opening browser: {_downloadUrl}");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _downloadUrl,
                        UseShellExecute = true
                    });
                    CloseDialog();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateDialogViewModel] Error starting update: {ex.Message}");
                MessageBox.Show(
                    $"Unable to start update: {ex.Message}\n\nPlease download manually from:\n{_downloadUrl}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Skip()
        {
            Console.WriteLine("[UpdateDialogViewModel] User skipped this version");
            CloseDialog();
        }

        private void RemindLater()
        {
            Console.WriteLine("[UpdateDialogViewModel] User chose to be reminded later");
            CloseDialog();
        }

        private void CloseDialog()
        {
            Application.Current.Windows[Application.Current.Windows.Count - 1]?.Close();
        }
    }
}
