using System;
using System.IO;
using System.Windows.Input;
using KenshiModManager.Commands;
using KenshiModManager.Core;
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

        public SettingsViewModel(AppSettings appSettings)
        {
            _appSettings = appSettings;

            BrowseKenshiPathCommand = new RelayCommand(BrowseKenshiPath);
            BrowseModsPathCommand = new RelayCommand(BrowseModsPath);
            BrowseWorkshopPathCommand = new RelayCommand(BrowseWorkshopPath);
            ResetToAutoDetectCommand = new RelayCommand(ResetToAutoDetect);
            SaveSettingsCommand = new RelayCommand(SaveSettings);

            LoadPaths();
        }

        public string KenshiPath
        {
            get => _kenshiPath;
            set => SetProperty(ref _kenshiPath, value);
        }

        public string ModsPath
        {
            get => _modsPath;
            set => SetProperty(ref _modsPath, value);
        }

        public string WorkshopPath
        {
            get => _workshopPath;
            set => SetProperty(ref _workshopPath, value);
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

        private void ValidateAllPaths()
        {
            IsKenshiPathValid = ValidateKenshiPath(KenshiPath);
            if (IsKenshiPathValid)
            {
                KenshiPathStatus = "✓ Valid - Kenshi installation found";
            }
            else if (string.IsNullOrEmpty(KenshiPath))
            {
                KenshiPathStatus = "⚠ Path not set";
            }
            else
            {
                KenshiPathStatus = "✗ Invalid - kenshi_x64.exe or kenshi.exe not found";
            }

            IsModsPathValid = ValidateModsPath(ModsPath);
            if (IsModsPathValid)
            {
                ModsCount = CountMods(ModsPath);
                if (ModsCount > 0)
                {
                    ModsPathStatus = $"✓ Valid - Found {ModsCount} mod(s)";
                }
                else
                {
                    ModsPathStatus = "⚠ Valid folder, but no .mod files found";
                }
            }
            else if (string.IsNullOrEmpty(ModsPath))
            {
                ModsCount = 0;
                ModsPathStatus = "⚠ Path not set";
            }
            else
            {
                ModsCount = 0;
                ModsPathStatus = "✗ Invalid - Folder does not exist";
            }

            IsWorkshopPathValid = ValidateWorkshopPath(WorkshopPath);
            if (IsWorkshopPathValid)
            {
                WorkshopModsCount = CountMods(WorkshopPath);
                if (WorkshopModsCount > 0)
                {
                    WorkshopPathStatus = $"✓ Valid - Found {WorkshopModsCount} mod(s)";
                }
                else
                {
                    WorkshopPathStatus = "⚠ Valid folder, but no .mod files found";
                }
            }
            else if (string.IsNullOrEmpty(WorkshopPath))
            {
                WorkshopModsCount = 0;
                WorkshopPathStatus = "⚠ Path not set";
            }
            else
            {
                WorkshopModsCount = 0;
                WorkshopPathStatus = "✗ Invalid - Folder does not exist";
            }

            Console.WriteLine($"[SettingsViewModel] Validated paths: Kenshi={IsKenshiPathValid}, Mods={IsModsPathValid} ({ModsCount}), Workshop={IsWorkshopPathValid} ({WorkshopModsCount})");
        }

        private bool ValidateKenshiPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            return File.Exists(Path.Combine(path, "kenshi_x64.exe")) ||
                   File.Exists(Path.Combine(path, "kenshi.exe"));
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

        private void BrowseKenshiPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Kenshi executable (any .exe file in Kenshi folder)",
                Filter = "All Executables (*.exe)|*.exe|Kenshi Executable|kenshi_x64.exe;kenshi.exe",
                FilterIndex = 1,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    KenshiPath = selectedPath;
                    IsKenshiPathCustom = true;

                    if (!IsModsPathCustom)
                    {
                        ModsPath = Path.Combine(selectedPath, "mods");
                    }

                    ValidateAllPaths();
                }
            }
        }

        private void BrowseModsPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select Mods Folder",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(ModsPath) && Directory.Exists(ModsPath))
                dialog.SelectedPath = ModsPath;

            if (dialog.ShowDialog() == true)
            {
                ModsPath = dialog.SelectedPath;
                IsModsPathCustom = true;
                ValidateAllPaths();
            }
        }

        private void BrowseWorkshopPath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select Workshop Folder",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(WorkshopPath) && Directory.Exists(WorkshopPath))
                dialog.SelectedPath = WorkshopPath;

            if (dialog.ShowDialog() == true)
            {
                WorkshopPath = dialog.SelectedPath;
                IsWorkshopPathCustom = true;
                ValidateAllPaths();
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
    }
}
