using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using KenshiModManager.Commands;
using KenshiModManager.Core;
using KenshiModManager.Properties;
using KenshiLib.Core;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace KenshiModManager.ViewModels
{
    /// <summary>
    /// ViewModel for Settings tab
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _appSettings;
        private string _kenshiPath = string.Empty;
        private string _modsPath = string.Empty;
        private string _workshopPath = string.Empty;
        private bool _isKenshiPathValid;
        private bool _isModsPathValid;
        private bool _isWorkshopPathValid;
        private bool _isKenshiPathCustom;
        private bool _isModsPathCustom;
        private bool _isWorkshopPathCustom;
        private int _modsCount;
        private int _workshopModsCount;
        private string _kenshiPathStatus = string.Empty;
        private string _modsPathStatus = string.Empty;
        private string _workshopPathStatus = string.Empty;
        private string _selectedLanguage = string.Empty;
        private LanguageOption? _selectedLanguageOption;

        public SettingsViewModel(AppSettings appSettings)
        {
            _appSettings = appSettings;

            BrowseKenshiPathCommand = new RelayCommand(BrowseKenshiPath);
            BrowseModsPathCommand = new RelayCommand(BrowseModsPath);
            BrowseWorkshopPathCommand = new RelayCommand(BrowseWorkshopPath);
            ResetToAutoDetectCommand = new RelayCommand(ResetToAutoDetect);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            AvailableLanguages = new ObservableCollection<LanguageOption>
            {
                new() { Code = "en", DisplayName = "English" },
                new() { Code = "ru", DisplayName = "Русский" },
                new() { Code = "pt", DisplayName = "Português" }
            };

            string currentLang = LocalizationManager.GetCurrentLanguage();
            SelectedLanguageOption = AvailableLanguages.FirstOrDefault(l => l.Code == currentLang);

            LoadPaths();
        }

        public string KenshiPath
        {
            get => _kenshiPath;
            set
            {
                if (SetProperty(ref _kenshiPath, value))
                {
                    ValidateAllPaths();
                    PersistSettings();
                }
            }
        }

        public string ModsPath
        {
            get => _modsPath;
            set
            {
                if (SetProperty(ref _modsPath, value))
                {
                    ValidateAllPaths();
                    PersistSettings();
                }
            }
        }

        public string WorkshopPath
        {
            get => _workshopPath;
            set
            {
                if (SetProperty(ref _workshopPath, value))
                {
                    ValidateAllPaths();
                    PersistSettings();
                }
            }
        }

        public bool IsKenshiPathValid
        {
            get => _isKenshiPathValid;
            set => SetProperty(ref _isKenshiPathValid, value);
        }

        public bool IsModsPathValid
        {
            get => _isModsPathValid;
            set => SetProperty(ref _isModsPathValid, value);
        }

        public bool IsWorkshopPathValid
        {
            get => _isWorkshopPathValid;
            set => SetProperty(ref _isWorkshopPathValid, value);
        }

        public bool IsKenshiPathCustom
        {
            get => _isKenshiPathCustom;
            set => SetProperty(ref _isKenshiPathCustom, value);
        }

        public bool IsModsPathCustom
        {
            get => _isModsPathCustom;
            set => SetProperty(ref _isModsPathCustom, value);
        }

        public bool IsWorkshopPathCustom
        {
            get => _isWorkshopPathCustom;
            set => SetProperty(ref _isWorkshopPathCustom, value);
        }

        public int ModsCount
        {
            get => _modsCount;
            set => SetProperty(ref _modsCount, value);
        }

        public int WorkshopModsCount
        {
            get => _workshopModsCount;
            set => SetProperty(ref _workshopModsCount, value);
        }

        public string KenshiPathStatus
        {
            get => _kenshiPathStatus;
            set => SetProperty(ref _kenshiPathStatus, value);
        }

        public string ModsPathStatus
        {
            get => _modsPathStatus;
            set => SetProperty(ref _modsPathStatus, value);
        }

        public string WorkshopPathStatus
        {
            get => _workshopPathStatus;
            set => SetProperty(ref _workshopPathStatus, value);
        }

        public ICommand BrowseKenshiPathCommand { get; }
        public ICommand BrowseModsPathCommand { get; }
        public ICommand BrowseWorkshopPathCommand { get; }
        public ICommand ResetToAutoDetectCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    LocalizationManager.ChangeLanguage(value, _appSettings);
                }
            }
        }

        public LanguageOption? SelectedLanguageOption
        {
            get => _selectedLanguageOption;
            set
            {
                if (SetProperty(ref _selectedLanguageOption, value) && value != null)
                {
                    LocalizationManager.ChangeLanguage(value.Code, _appSettings);
                }
            }
        }

        public ObservableCollection<LanguageOption> AvailableLanguages { get; }

        private void LoadPaths()
        {
            if (!string.IsNullOrEmpty(_appSettings.CustomKenshiPath))
            {
                KenshiPath = _appSettings.CustomKenshiPath;
                IsKenshiPathCustom = true;
            }
            else
            {
                KenshiPath = ModManager.FindKenshiInstallDir() ?? string.Empty;
                IsKenshiPathCustom = false;
            }

            if (!string.IsNullOrEmpty(_appSettings.CustomModsPath))
            {
                ModsPath = _appSettings.CustomModsPath;
                IsModsPathCustom = true;
            }
            else
            {
                ModsPath = !string.IsNullOrEmpty(KenshiPath) ? Path.Combine(KenshiPath, "mods") : string.Empty;
                IsModsPathCustom = false;
            }

            if (!string.IsNullOrEmpty(_appSettings.CustomWorkshopPath))
            {
                WorkshopPath = _appSettings.CustomWorkshopPath;
                IsWorkshopPathCustom = true;
            }
            else
            {
                WorkshopPath = ModManager.FindWorkshopPath() ?? string.Empty;
                IsWorkshopPathCustom = false;
            }

            ValidateAllPaths();
        }

        public void ValidateAllPaths()
        {
            IsKenshiPathValid = ValidateKenshiPath(KenshiPath);
            if (IsKenshiPathValid)
            {
                KenshiPathStatus = Resources.SettingsValidation_KenshiValid;
            }
            else if (string.IsNullOrEmpty(KenshiPath))
            {
                KenshiPathStatus = Resources.SettingsValidation_PathNotSet;
            }
            else
            {
                KenshiPathStatus = Resources.SettingsValidation_KenshiInvalid;
            }

            IsModsPathValid = ValidateModsPath(ModsPath);
            if (IsModsPathValid)
            {
                ModsCount = CountMods(ModsPath);
                if (ModsCount > 0)
                {
                    ModsPathStatus = string.Format(Resources.SettingsValidation_ModsValid, ModsCount);
                }
                else
                {
                    ModsPathStatus = Resources.SettingsValidation_ModsValidNoMods;
                }
            }
            else if (string.IsNullOrEmpty(ModsPath))
            {
                ModsCount = 0;
                ModsPathStatus = Resources.SettingsValidation_PathNotSet;
            }
            else
            {
                ModsCount = 0;
                ModsPathStatus = Resources.SettingsValidation_FolderNotExists;
            }

            IsWorkshopPathValid = ValidateWorkshopPath(WorkshopPath);
            if (IsWorkshopPathValid)
            {
                WorkshopModsCount = CountMods(WorkshopPath);
                if (WorkshopModsCount > 0)
                {
                    WorkshopPathStatus = string.Format(Resources.SettingsValidation_ModsValid, WorkshopModsCount);
                }
                else
                {
                    WorkshopPathStatus = Resources.SettingsValidation_ModsValidNoMods;
                }
            }
            else if (string.IsNullOrEmpty(WorkshopPath))
            {
                WorkshopModsCount = 0;
                WorkshopPathStatus = Resources.SettingsValidation_PathNotSet;
            }
            else
            {
                WorkshopModsCount = 0;
                WorkshopPathStatus = Resources.SettingsValidation_FolderNotExists;
            }

            Console.WriteLine($"[SettingsViewModel] Validated paths: Kenshi={IsKenshiPathValid}, Mods={IsModsPathValid} ({ModsCount}), Workshop={IsWorkshopPathValid} ({WorkshopModsCount})");
        }

        private bool ValidateKenshiPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            try
            {
                static int GetPriority(string fileName)
                {
                    if (fileName.Equals("kenshi_x64.exe", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (fileName.Equals("kenshi_GOG_x64.exe", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (fileName.StartsWith("kenshi", StringComparison.OrdinalIgnoreCase) &&
                        fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return 2;
                    return 3;
                }

                var exe = Directory.EnumerateFiles(path, "kenshi*.exe")
                    .Select(filePath => new FileInfo(filePath))
                    .OrderBy(f => GetPriority(f.Name))
                    .ThenByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (exe == null)
                    return false;

                Console.WriteLine($"[SettingsViewModel] Detected Kenshi executable: {exe.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsViewModel] Error detecting Kenshi executable: {ex.Message}");
                return false;
            }
        }

        private bool ValidateModsPath(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        private bool ValidateWorkshopPath(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        private int CountMods(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return 0;

                return Directory.GetFiles(path, "*.mod", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        private void PersistSettings()
        {
            bool needsSave = false;

            if (IsKenshiPathCustom && IsKenshiPathValid)
            {
                if (!string.Equals(_appSettings.CustomKenshiPath, KenshiPath, StringComparison.OrdinalIgnoreCase))
                {
                    _appSettings.CustomKenshiPath = KenshiPath;
                    needsSave = true;
                    Console.WriteLine($"[SettingsViewModel] Will save CustomKenshiPath: {KenshiPath}");
                }

                if (!string.Equals(ModManager.KenshiPath, KenshiPath, StringComparison.OrdinalIgnoreCase))
                {
                    ModManager.SetKenshiPath(KenshiPath);
                }
            }

            if (IsModsPathCustom && IsModsPathValid)
            {
                if (!string.Equals(_appSettings.CustomModsPath, ModsPath, StringComparison.OrdinalIgnoreCase))
                {
                    _appSettings.CustomModsPath = ModsPath;
                    needsSave = true;
                    Console.WriteLine($"[SettingsViewModel] Will save CustomModsPath: {ModsPath}");
                }

                if (!string.Equals(ModManager.gamedirModsPath, ModsPath, StringComparison.OrdinalIgnoreCase))
                {
                    ModManager.SetModsPath(ModsPath);
                }
            }

            if (IsWorkshopPathCustom && IsWorkshopPathValid)
            {
                if (!string.Equals(_appSettings.CustomWorkshopPath, WorkshopPath, StringComparison.OrdinalIgnoreCase))
                {
                    _appSettings.CustomWorkshopPath = WorkshopPath;
                    needsSave = true;
                    Console.WriteLine($"[SettingsViewModel] Will save CustomWorkshopPath: {WorkshopPath}");
                }

                if (!string.Equals(ModManager.workshopModsPath, WorkshopPath, StringComparison.OrdinalIgnoreCase))
                {
                    ModManager.SetWorkshopPath(WorkshopPath);
                }
            }

            if (needsSave)
            {
                _appSettings.Save();
                Console.WriteLine("[SettingsViewModel] Settings persisted");
            }
        }

        private void BrowseKenshiPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = Resources.FileDialog_SelectKenshiExe,
                Filter = "Kenshi Executable (kenshi*.exe)|kenshi*.exe|All Executables (*.exe)|*.exe",
                FilterIndex = 1,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    IsKenshiPathCustom = true;
                    KenshiPath = selectedPath;

                    if (!IsModsPathCustom)
                    {
                        ModsPath = Path.Combine(selectedPath, "mods");
                    }
                }
            }
        }

        private void BrowseModsPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Resources.FileDialog_SelectModsFolder,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(ModsPath) && Directory.Exists(ModsPath))
                dialog.SelectedPath = ModsPath;

            if (dialog.ShowDialog() == true)
            {
                IsModsPathCustom = true;
                ModsPath = dialog.SelectedPath;
            }
        }

        private void BrowseWorkshopPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Resources.FileDialog_SelectWorkshopFolder,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(WorkshopPath) && Directory.Exists(WorkshopPath))
                dialog.SelectedPath = WorkshopPath;

            if (dialog.ShowDialog() == true)
            {
                IsWorkshopPathCustom = true;
                WorkshopPath = dialog.SelectedPath;
            }
        }

        private void ResetToAutoDetect()
        {
            _appSettings.CustomKenshiPath = null;
            _appSettings.CustomModsPath = null;
            _appSettings.CustomWorkshopPath = null;
            _appSettings.Save();

            LoadPaths();

            Console.WriteLine("[SettingsViewModel] Reset to auto-detect");
        }

        private void SaveSettings()
        {
            if (IsKenshiPathCustom)
            {
                _appSettings.CustomKenshiPath = KenshiPath;
            }
            else
            {
                _appSettings.CustomKenshiPath = null;
            }

            if (IsModsPathCustom)
            {
                _appSettings.CustomModsPath = ModsPath;
            }
            else
            {
                _appSettings.CustomModsPath = null;
            }

            if (IsWorkshopPathCustom)
            {
                _appSettings.CustomWorkshopPath = WorkshopPath;
            }
            else
            {
                _appSettings.CustomWorkshopPath = null;
            }

            _appSettings.Save();

            if (IsKenshiPathValid)
            {
                ModManager.SetKenshiPath(KenshiPath);
            }

            Console.WriteLine("[SettingsViewModel] Settings saved");
        }

        public class LanguageOption
        {
            public string Code { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;

            public override string ToString() => DisplayName;
        }
    }
}
