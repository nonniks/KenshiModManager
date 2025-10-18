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

            IsModsPathValid = ValidateModsPath(ModsPath);
            if (IsModsPathValid)
            {
                ModsCount = CountMods(ModsPath);
            }
            else
            {
                ModsCount = 0;
            }

            IsWorkshopPathValid = ValidateWorkshopPath(WorkshopPath);
            if (IsWorkshopPathValid)
            {
                WorkshopModsCount = CountMods(WorkshopPath);
            }
            else
            {
                WorkshopModsCount = 0;
            }

            Console.WriteLine($"[SettingsViewModel] Validated paths: Kenshi={IsKenshiPathValid}, Mods={IsModsPathValid}, Workshop={IsWorkshopPathValid}");
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

                return Directory.GetFiles(path, "*.mod", SearchOption.TopDirectoryOnly).Length;
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
                Title = "Select Kenshi executable (kenshi_x64.exe or kenshi.exe)",
                Filter = "Kenshi Executable|kenshi_x64.exe;kenshi.exe",
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
