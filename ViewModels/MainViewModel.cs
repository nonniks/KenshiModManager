using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using KenshiModManager.Commands;
using KenshiModManager.Core;
using KenshiModManager.Properties;
using KenshiLib.Core;

namespace KenshiModManager.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application window
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ModManager _modManager;
        private readonly PlaysetRepository _playsetRepository;
        private readonly GameLauncher _gameLauncher;
        private readonly ReverseEngineer _reverseEngineer;
        private readonly DispatcherTimer _gameMonitorTimer;
        private readonly AppSettings _appSettings;

        private string _kenshiPath;
        private int _activeModsCount;
        private bool _isGameRunning;
        private string _statusMessage;
        private PlaysetInfo? _selectedPlayset;
        private ObservableCollection<PlaysetInfo> _playsets;
        private string? _currentPlaysetFilePath; // Currently loaded playset (NOT necessarily mods.cfg)

        public MainViewModel()
        {
            _appSettings = AppSettings.Load();

            _reverseEngineer = new ReverseEngineer();
            _modManager = new ModManager(_reverseEngineer);

            // Try to get path from ModManager autodetect first
            _kenshiPath = ModManager.KenshiPath ?? string.Empty;

            // If autodetect failed but we have custom settings, validate and use those
            if (string.IsNullOrEmpty(_kenshiPath) && !string.IsNullOrEmpty(_appSettings.CustomKenshiPath))
            {
                var customPath = _appSettings.CustomKenshiPath;

                // Validate custom path before applying
                if (Directory.Exists(customPath) &&
                    Directory.EnumerateFiles(customPath, "kenshi*.exe").Any())
                {
                    Console.WriteLine("[MainViewModel] Autodetect failed, using CustomKenshiPath from AppSettings");
                    _kenshiPath = customPath;
                    ModManager.SetKenshiPath(_kenshiPath);

                    // Also set custom workshop and mods paths if available and valid
                    if (!string.IsNullOrEmpty(_appSettings.CustomWorkshopPath) &&
                        Directory.Exists(_appSettings.CustomWorkshopPath))
                    {
                        ModManager.SetWorkshopPath(_appSettings.CustomWorkshopPath);
                        Console.WriteLine($"[MainViewModel] Set custom workshop path: {_appSettings.CustomWorkshopPath}");
                    }

                    if (!string.IsNullOrEmpty(_appSettings.CustomModsPath) &&
                        Directory.Exists(_appSettings.CustomModsPath))
                    {
                        ModManager.SetModsPath(_appSettings.CustomModsPath);
                        Console.WriteLine($"[MainViewModel] Set custom mods path: {_appSettings.CustomModsPath}");
                    }
                }
                else
                {
                    Console.WriteLine("[MainViewModel] WARNING: CustomKenshiPath from AppSettings is invalid or exe not found");
                }
            }
            else if (!string.IsNullOrEmpty(_kenshiPath))
            {
                Console.WriteLine($"[MainViewModel] Autodetect succeeded: {_kenshiPath}");
            }

            if (!string.IsNullOrEmpty(_kenshiPath))
            {
                _playsetRepository = new PlaysetRepository(_kenshiPath);
                _gameLauncher = new GameLauncher(_kenshiPath);
            }
            else
            {
                _playsetRepository = null!;
                _gameLauncher = null!;
            }

            _playsets = new ObservableCollection<PlaysetInfo>();
            _statusMessage = "Ready";

            LaunchGameCommand = new AsyncRelayCommand(LaunchGameAsync, CanLaunchGame);
            RefreshModsCommand = new AsyncRelayCommand(RefreshModsAsync);
            CheckGameStatusCommand = new RelayCommand(CheckGameStatus);
            CreatePlaysetCommand = new AsyncRelayCommand(CreatePlaysetAsync);
            RenamePlaysetCommand = new AsyncRelayCommand<PlaysetInfo>(RenamePlaysetAsync);
            DuplicatePlaysetCommand = new AsyncRelayCommand<PlaysetInfo>(DuplicatePlaysetAsync);
            DeletePlaysetCommand = new AsyncRelayCommand<PlaysetInfo>(DeletePlaysetAsync);
            ExportPlaysetCommand = new AsyncRelayCommand<PlaysetInfo>(ExportPlaysetAsync);
            ImportPlaysetCommand = new AsyncRelayCommand(ImportPlaysetAsync);
            SwitchPlaysetCommand = new AsyncRelayCommand<PlaysetInfo>(SwitchPlaysetAsync);
            SaveCurrentPlaysetCommand = new AsyncRelayCommand(SaveCurrentPlaysetAsync);

            ModsTabViewModel = new ModsTabViewModel(_modManager);
            SettingsTabViewModel = new SettingsViewModel(_appSettings);

            ModsTabViewModel.SetSavePlaysetCallback(() =>
            {
                _ = SaveCurrentPlaysetAsync();
            });

            _gameMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _gameMonitorTimer.Tick += GameMonitorTimer_Tick;
            _gameMonitorTimer.Start();

            LoadPlaysets();
        }

        private void GameMonitorTimer_Tick(object? sender, EventArgs e)
        {
            CheckGameStatus();
        }

        #region Properties

        public string KenshiPath
        {
            get => _kenshiPath;
            set => SetProperty(ref _kenshiPath, value);
        }

        public int ActiveModsCount
        {
            get => _activeModsCount;
            set => SetProperty(ref _activeModsCount, value);
        }

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set => SetProperty(ref _isGameRunning, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<PlaysetInfo> Playsets
        {
            get => _playsets;
            set => SetProperty(ref _playsets, value);
        }

        public PlaysetInfo? SelectedPlayset
        {
            get => _selectedPlayset;
            set
            {
                if (SetProperty(ref _selectedPlayset, value) && value != null)
                {
                    _appSettings.LastSelectedPlaysetName = value.Name;
                    _appSettings.Save();

                    _ = SwitchToPlaysetAsync(value);
                }
            }
        }

        #endregion

        #region Tab ViewModels

        public ModsTabViewModel ModsTabViewModel { get; }
        public SettingsViewModel SettingsTabViewModel { get; }

        // Will be added later:
        // public TranslatorTabViewModel TranslatorTabViewModel { get; }
        // public ValidatorTabViewModel ValidatorTabViewModel { get; }

        #endregion

        #region Commands

        public ICommand LaunchGameCommand { get; }
        public ICommand RefreshModsCommand { get; }
        public ICommand CheckGameStatusCommand { get; }
        public ICommand CreatePlaysetCommand { get; }
        public ICommand RenamePlaysetCommand { get; }
        public ICommand DuplicatePlaysetCommand { get; }
        public ICommand DeletePlaysetCommand { get; }
        public ICommand ExportPlaysetCommand { get; }
        public ICommand ImportPlaysetCommand { get; }
        public ICommand SwitchPlaysetCommand { get; }
        public ICommand SaveCurrentPlaysetCommand { get; }

        #endregion

        #region Command Implementations

        private bool CanLaunchGame()
        {
            return !string.IsNullOrEmpty(_kenshiPath) &&
                   _gameLauncher != null &&
                   !_isGameRunning &&
                   _gameLauncher.ValidateInstallation();
        }

        private async Task LaunchGameAsync()
        {
            try
            {
                StatusMessage = "Preparing to launch Kenshi...";

                await SaveCurrentPlaysetAsync();

                var modsConfigPath = Path.Combine(_kenshiPath, "data", "mods.cfg");
                var enabledMods = ModsTabViewModel.ActivePlaysetMods
                    .Where(m => m.IsEnabled)
                    .OrderBy(m => m.LoadOrder)
                    .Select(m => m.Name)
                    .ToList();

                File.WriteAllLines(modsConfigPath, enabledMods);
                Console.WriteLine($"[MainViewModel] Wrote {enabledMods.Count} ENABLED mods to mods.cfg for launch (total in playset: {ModsTabViewModel.ActivePlaysetMods.Count})");

                StatusMessage = "Launching Kenshi...";

                await Task.Run(() =>
                {
                    bool success = _gameLauncher.LaunchGame(use64bit: true);

                    if (success)
                    {
                        IsGameRunning = true;
                        StatusMessage = "Kenshi is running...";
                    }
                    else
                    {
                        StatusMessage = "Failed to launch Kenshi";
                    }
                });

                ((AsyncRelayCommand)LaunchGameCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error launching game: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error launching game: {ex}");
            }
        }

        private async Task RefreshModsAsync()
        {
            try
            {
                StatusMessage = "Refreshing mods...";

                await ModsTabViewModel.LoadModsAsync();

                ActiveModsCount = ModsTabViewModel.ActivePlaysetMods.Count;
                StatusMessage = $"Loaded {ModsTabViewModel.AllMods.Count} mods, {ActiveModsCount} active";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing mods: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error refreshing mods: {ex}");
            }
        }

        private void CheckGameStatus()
        {
            if (_gameLauncher != null)
            {
                bool wasRunning = _isGameRunning;
                bool isCurrentlyRunning = _gameLauncher.IsGameRunning();

                if (wasRunning != isCurrentlyRunning)
                {
                    IsGameRunning = isCurrentlyRunning;

                    if (wasRunning && !isCurrentlyRunning)
                    {
                        StatusMessage = "Kenshi has been closed";
                    }

                    ((AsyncRelayCommand)LaunchGameCommand).RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Playset Management

        private void LoadPlaysets()
        {
            if (_playsetRepository == null)
                return;

            try
            {
                var playsets = _playsetRepository.GetAllPlaysets();
                Playsets.Clear();

                // First-run scenario: No playsets exist
                if (playsets.Count == 0)
                {
                    Console.WriteLine("[MainViewModel] No playsets found - first run detected");

                    // Check if mods.cfg exists and has content
                    var modsConfigPath = Path.Combine(_kenshiPath, "data", "mods.cfg");
                    if (File.Exists(modsConfigPath))
                    {
                        var lines = File.ReadAllLines(modsConfigPath);
                        var modNames = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                        if (modNames.Count > 0)
                        {
                            // Import existing mods.cfg as "Initial Playset" - only once!
                            Console.WriteLine($"[MainViewModel] Found {modNames.Count} mods in mods.cfg - importing as 'Initial Playset'");

                            // Create playset file directly to avoid ImportPlayset adding "_1" suffix
                            var initialPlaysetPath = Path.Combine(_kenshiPath, "data", "playsets", "Initial Playset.cfg");
                            if (!File.Exists(initialPlaysetPath))
                            {
                                File.WriteAllLines(initialPlaysetPath, modNames);
                                Console.WriteLine($"[MainViewModel] Created Initial Playset at: {initialPlaysetPath}");
                            }

                            var initialPlayset = _playsetRepository.GetAllPlaysets()
                                .FirstOrDefault(p => p.Name == "Initial Playset");

                            if (initialPlayset != null)
                            {
                                playsets.Add(initialPlayset);
                            }
                        }
                        else
                        {
                            // mods.cfg is empty - create empty Initial Playset
                            Console.WriteLine("[MainViewModel] mods.cfg is empty - creating empty 'Initial Playset'");
                            var initialPlayset = _playsetRepository.CreateNewPlayset("Initial Playset");
                            if (initialPlayset != null)
                            {
                                playsets.Add(initialPlayset);
                            }
                        }
                    }
                    else
                    {
                        // No mods.cfg - create empty Initial Playset
                        Console.WriteLine("[MainViewModel] No mods.cfg found - creating empty 'Initial Playset'");
                        var initialPlayset = _playsetRepository.CreateNewPlayset("Initial Playset");
                        if (initialPlayset != null)
                        {
                            playsets.Add(initialPlayset);
                        }
                    }
                }

                foreach (var playset in playsets)
                {
                    Playsets.Add(playset);
                }

                if (!string.IsNullOrEmpty(_appSettings.LastSelectedPlaysetName))
                {
                    var lastPlayset = Playsets.FirstOrDefault(p => p.Name == _appSettings.LastSelectedPlaysetName);
                    SelectedPlayset = lastPlayset ?? Playsets.FirstOrDefault();
                    Console.WriteLine($"[MainViewModel] Restored last playset: {SelectedPlayset?.Name ?? "none"}");
                }
                else
                {
                    SelectedPlayset = Playsets.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainViewModel] Error loading playsets: {ex}");
            }
        }

        private async Task CreatePlaysetAsync()
        {
            if (_playsetRepository == null)
                return;

            try
            {
                await SaveCurrentPlaysetAsync();

                string newName = GeneratePlaysetName();
                var playset = _playsetRepository.CreateNewPlayset(newName);

                if (playset != null)
                {
                    Playsets.Add(playset);

                    ModsTabViewModel.ActivePlaysetMods.Clear();
                    ModsTabViewModel.FilteredPlaysetMods.Clear();

                    SelectedPlayset = playset;
                    StatusMessage = $"Created empty playset: {playset.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error creating playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private string GeneratePlaysetName()
        {
            int nextNumber = 1;
            while (Playsets.Any(p => p.Name == $"Playset {nextNumber}"))
            {
                nextNumber++;
            }
            return $"Playset {nextNumber}";
        }

        private async Task RenamePlaysetAsync(PlaysetInfo? playset)
        {
            if (playset == null || _playsetRepository == null)
                return;

            try
            {
                var dialog = new Views.InputDialog(
                    "Rename Playset",
                    "Enter new playset name:",
                    playset.Name);

                if (dialog.ShowDialog() == true)
                {
                    string newName = dialog.InputText;

                    if (newName == playset.Name)
                    {
                        StatusMessage = "Name unchanged";
                        return;
                    }

                    var renamedPlayset = _playsetRepository.RenamePlayset(playset.FilePath, newName);

                    if (renamedPlayset != null)
                    {
                        int index = Playsets.IndexOf(playset);
                        if (index >= 0)
                        {
                            Playsets[index] = renamedPlayset;

                            if (SelectedPlayset == playset)
                            {
                                SelectedPlayset = renamedPlayset;
                                _currentPlaysetFilePath = renamedPlayset.FilePath;
                            }
                        }

                        StatusMessage = $"Renamed playset to: {newName}";
                        Console.WriteLine($"[MainViewModel] Successfully renamed playset to: {newName}");
                    }
                    else
                    {
                        StatusMessage = "Failed to rename playset - name may already exist";
                        Console.WriteLine($"[MainViewModel] Failed to rename playset - name may already exist");
                    }
                }
                else
                {
                    Console.WriteLine("[MainViewModel] Rename cancelled by user");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error renaming playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error renaming playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task DuplicatePlaysetAsync(PlaysetInfo? playset)
        {
            if (playset == null || _playsetRepository == null)
                return;

            try
            {
                var duplicate = _playsetRepository.DuplicatePlayset(playset.FilePath, $"{playset.Name} Copy");

                if (duplicate != null)
                {
                    Playsets.Add(duplicate);
                    StatusMessage = $"Duplicated playset: {duplicate.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error duplicating playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error duplicating playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task DeletePlaysetAsync(PlaysetInfo? playset)
        {
            if (playset == null || _playsetRepository == null)
                return;

            try
            {
                string message = playset.IsActive
                    ? $"Playset '{playset.Name}' is currently ACTIVE.\n\nAre you sure you want to delete it? This action cannot be undone."
                    : $"Are you sure you want to delete playset '{playset.Name}'?\n\nThis action cannot be undone.";

                var result = System.Windows.MessageBox.Show(
                    message,
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[MainViewModel] Attempting to delete playset: {playset.FilePath}");

                    if (playset.IsActive || playset.FilePath == _currentPlaysetFilePath)
                    {
                        Console.WriteLine($"[MainViewModel] Clearing active playset selection before deletion");
                        _currentPlaysetFilePath = null;
                        SelectedPlayset = null;
                        ModsTabViewModel.ActivePlaysetMods.Clear();
                    }

                    if (_playsetRepository.DeletePlayset(playset.FilePath))
                    {
                        Playsets.Remove(playset);
                        StatusMessage = $"Deleted playset: {playset.Name}";
                        Console.WriteLine($"[MainViewModel] Successfully deleted playset: {playset.Name}");
                    }
                    else
                    {
                        StatusMessage = $"Failed to delete playset: {playset.Name}";
                        Console.WriteLine($"[MainViewModel] Failed to delete playset file: {playset.FilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error deleting playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task ExportPlaysetAsync(PlaysetInfo? playset)
        {
            if (playset == null || _playsetRepository == null)
                return;

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Playset",
                    Filter = "Configuration Files (*.cfg)|*.cfg|All Files (*.*)|*.*",
                    DefaultExt = ".cfg",
                    FileName = playset.Name,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string destinationPath = saveFileDialog.FileName;

                    if (_playsetRepository.ExportPlayset(playset.FilePath, destinationPath))
                    {
                        StatusMessage = $"Exported playset to: {destinationPath}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error exporting playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task ImportPlaysetAsync()
        {
            if (_playsetRepository == null)
                return;

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import Playset",
                    Filter = "Configuration Files (*.cfg)|*.cfg|All Files (*.*)|*.*",
                    DefaultExt = ".cfg",
                    CheckFileExists = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string sourceFile = openFileDialog.FileName;
                    var imported = _playsetRepository.ImportPlayset(sourceFile);

                    if (imported != null)
                    {
                        Playsets.Add(imported);
                        SelectedPlayset = imported;
                        StatusMessage = $"Imported playset: {imported.Name}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error importing playset: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task SwitchPlaysetAsync(PlaysetInfo? playset)
        {
            await SwitchToPlaysetAsync(playset);
        }

        private async Task SwitchToPlaysetAsync(PlaysetInfo? playset)
        {
            if (playset == null || _playsetRepository == null)
                return;

            try
            {
                StatusMessage = $"Loading playset: {playset.Name}...";

                string playsetFile = playset.FilePath;

                if (string.IsNullOrEmpty(playsetFile))
                {
                    Console.WriteLine($"[MainViewModel] Playset has no file path: {playset.Name}");
                    return;
                }

                if (!string.IsNullOrEmpty(_currentPlaysetFilePath) && _currentPlaysetFilePath != playsetFile)
                {
                    await SaveCurrentPlaysetAsync();
                }

                string? previousPlaysetPath = _currentPlaysetFilePath;
                _currentPlaysetFilePath = null;

                var modEntries = _playsetRepository.LoadPlaysetModsWithState(playsetFile);

                await ModsTabViewModel.LoadModsAsync();

                ModsTabViewModel.SetLoadingPlayset(true);

                try
                {
                    ModsTabViewModel.ActivePlaysetMods.Clear();
                    for (int i = 0; i < modEntries.Count; i++)
                    {
                        var entry = modEntries[i];
                        var mod = ModsTabViewModel.AllMods.FirstOrDefault(m =>
                            m.Name.Equals(entry.ModName, StringComparison.OrdinalIgnoreCase));

                        if (mod != null)
                        {
                            mod.IsEnabled = entry.IsEnabled;
                            mod.LoadOrder = i;
                            ModsTabViewModel.ActivePlaysetMods.Add(mod);
                        }
                    }

                    ModsTabViewModel.ApplyFilter();
                }
                finally
                {
                    ModsTabViewModel.SetLoadingPlayset(false);
                }

                _currentPlaysetFilePath = playsetFile;

                ActiveModsCount = ModsTabViewModel.ActivePlaysetMods.Count;
                StatusMessage = $"Loaded playset: {playset.Name} ({ActiveModsCount} mods)";
                Console.WriteLine($"[MainViewModel] Loaded {ActiveModsCount} mods into playset: {playset.Name}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading playset: {ex.Message}";
                Console.WriteLine($"[MainViewModel] Error loading playset: {ex}");
            }
        }

        private async Task SaveCurrentPlaysetAsync()
        {
            if (string.IsNullOrEmpty(_currentPlaysetFilePath) || _playsetRepository == null)
                return;

            try
            {
                var directory = Path.GetDirectoryName(_currentPlaysetFilePath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    Console.WriteLine($"[MainViewModel] Cannot save playset - directory does not exist: {directory}");
                    _currentPlaysetFilePath = null;
                    return;
                }

                _playsetRepository.SaveModsToPlaysetWithState(_currentPlaysetFilePath, ModsTabViewModel.ActivePlaysetMods);
                Console.WriteLine($"[MainViewModel] Saved playset with state to {Path.GetFileName(_currentPlaysetFilePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainViewModel] Error saving current playset: {ex}");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}
