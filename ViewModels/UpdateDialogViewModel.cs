using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        public UpdateDialogViewModel(string currentVersion, string installedVersion, string downloadUrl)
        {
            _currentVersion = currentVersion ?? string.Empty;
            _installedVersion = installedVersion ?? string.Empty;
            _changelog = "Loading changelog from GitHub...";
            _downloadUrl = downloadUrl ?? string.Empty;

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
                // Convert version to GitHub tag format (e.g., "1.1.0" -> "v1.1.0")
                string tag = GitHubReleaseService.VersionToTag(_currentVersion);
                Console.WriteLine($"[UpdateDialogViewModel] Fetching changelog for tag: {tag}");

                // Fetch release info from GitHub API
                var release = await GitHubReleaseService.GetReleaseByTagAsync(tag);

                if (release != null && !string.IsNullOrWhiteSpace(release.Body))
                {
                    // Extract only the Changes section from release body
                    Changelog = ExtractChangesSection(release.Body);
                    Console.WriteLine($"[UpdateDialogViewModel] Successfully loaded changelog ({Changelog.Length} chars)");
                }
                else
                {
                    // Fallback message if GitHub API fails
                    Changelog = "No changelog available.\n\nVisit the GitHub releases page for more information:\nhttps://github.com/nonniks/KenshiModManager/releases";
                    Console.WriteLine("[UpdateDialogViewModel] No changelog found, using fallback");
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
                Console.WriteLine($"[UpdateDialogViewModel] Opening download URL: {_downloadUrl}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = _downloadUrl,
                    UseShellExecute = true
                });

                CloseDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateDialogViewModel] Error opening download URL: {ex.Message}");
                MessageBox.Show(
                    $"Unable to open download link: {ex.Message}",
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
