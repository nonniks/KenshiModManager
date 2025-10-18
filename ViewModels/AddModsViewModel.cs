using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KenshiModManager.Commands;
using KenshiLib.Core;

namespace KenshiModManager.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Mods window
    /// </summary>
    public class AddModsViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ModInfo> _allMods;
        private readonly ObservableCollection<ModInfo> _activePlaysetMods;
        private readonly Action _savePlaysetCallback;

        private string _searchText = string.Empty;
        private string _sortBy = "Name";
        private ObservableCollection<ModSelectionItem> _availableMods;
        private ObservableCollection<ModSelectionItem> _filteredMods;
        private bool _isLoading;

        public AddModsViewModel(
            ObservableCollection<ModInfo> allMods,
            ObservableCollection<ModInfo> activePlaysetMods,
            Action savePlaysetCallback)
        {
            _allMods = allMods ?? throw new ArgumentNullException(nameof(allMods));
            _activePlaysetMods = activePlaysetMods ?? throw new ArgumentNullException(nameof(activePlaysetMods));
            _savePlaysetCallback = savePlaysetCallback ?? throw new ArgumentNullException(nameof(savePlaysetCallback));

            _availableMods = new ObservableCollection<ModSelectionItem>();
            _filteredMods = new ObservableCollection<ModSelectionItem>();

            AddSelectedModsCommand = new RelayCommand(AddSelectedMods, CanAddSelectedMods);
            CancelCommand = new RelayCommand(() => { });
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            SearchCommand = new RelayCommand(ApplyFilter);

            _ = LoadAvailableModsAsync();
        }

        #region Properties

        public ObservableCollection<ModSelectionItem> FilteredMods
        {
            get => _filteredMods;
            set => SetProperty(ref _filteredMods, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (SetProperty(ref _sortBy, value))
                {
                    ApplySort();
                }
            }
        }

        public int SelectedCount => _availableMods.Count(m => m.IsSelected);
        public int TotalCount => _availableMods.Count;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand AddSelectedModsCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand SearchCommand { get; }

        #endregion

        #region Private Methods

        private async System.Threading.Tasks.Task LoadAvailableModsAsync()
        {
            IsLoading = true;

            try
            {
                await System.Threading.Tasks.Task.Delay(10);

                _availableMods.Clear();

                var activeMods = _activePlaysetMods.Select(m => m.Name).ToHashSet();

                var modsToAdd = _allMods.Where(m => !activeMods.Contains(m.Name)).ToList();

                foreach (var mod in modsToAdd)
                {
                    _availableMods.Add(new ModSelectionItem(mod));
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddModsViewModel] Error loading mods: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var filtered = _availableMods.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchLower = _searchText.ToLower();
                filtered = filtered.Where(m =>
                    m.ModInfo.Name.ToLower().Contains(searchLower) ||
                    (m.ModInfo.Author?.ToLower().Contains(searchLower) ?? false) ||
                    (m.ModInfo.Description?.ToLower().Contains(searchLower) ?? false));
            }

            var resultList = filtered.ToList();

            FilteredMods.Clear();
            foreach (var item in resultList)
            {
                FilteredMods.Add(item);
            }

            ApplySort();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        private void ApplySort()
        {
            var sorted = FilteredMods.ToList();

            switch (_sortBy)
            {
                case "Name":
                    sorted = sorted.OrderBy(m => m.ModInfo.Name).ToList();
                    break;
                case "Author":
                    sorted = sorted.OrderBy(m => m.ModInfo.Author).ToList();
                    break;
                case "Size":
                    sorted = sorted.OrderByDescending(m => m.ModInfo.FileSize).ToList();
                    break;
                case "Date":
                    sorted = sorted.OrderByDescending(m => m.ModInfo.LastModified).ToList();
                    break;
            }

            FilteredMods.Clear();
            foreach (var item in sorted)
            {
                FilteredMods.Add(item);
            }
        }

        private void SelectAll()
        {
            foreach (var mod in FilteredMods)
            {
                mod.IsSelected = true;
            }
            OnPropertyChanged(nameof(SelectedCount));
        }

        private void DeselectAll()
        {
            foreach (var mod in FilteredMods)
            {
                mod.IsSelected = false;
            }
            OnPropertyChanged(nameof(SelectedCount));
        }

        private bool CanAddSelectedMods()
        {
            return _availableMods.Any(m => m.IsSelected);
        }

        private void AddSelectedMods()
        {
            try
            {
                Console.WriteLine($"[AddModsViewModel] AddSelectedMods called - checking selection...");
                Console.WriteLine($"[AddModsViewModel] Total available mods: {_availableMods.Count}");

                var selectedMods = _availableMods.Where(m => m.IsSelected).Select(m => m.ModInfo).ToList();

                Console.WriteLine($"[AddModsViewModel] Selected mods count: {selectedMods.Count}");

                if (selectedMods.Count == 0)
                {
                    Console.WriteLine("[AddModsViewModel] WARNING: No mods selected! User needs to check checkboxes.");
                    return;
                }

                foreach (var mod in selectedMods)
                {
                    Console.WriteLine($"[AddModsViewModel] Adding mod: {mod.Name}");

                    int newPosition = _activePlaysetMods.Count;

                    mod.IsEnabled = true;
                    mod.LoadOrder = newPosition;

                    _activePlaysetMods.Add(mod);
                }

                _savePlaysetCallback();

                Console.WriteLine($"[AddModsViewModel] Successfully added {selectedMods.Count} mods to playset");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddModsViewModel] Error adding mods: {ex}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Wrapper class for mod selection in the Add Mods window
    /// </summary>
    public class ModSelectionItem : ViewModelBase
    {
        private bool _isSelected;

        public ModSelectionItem(ModInfo modInfo)
        {
            ModInfo = modInfo ?? throw new ArgumentNullException(nameof(modInfo));
        }

        public ModInfo ModInfo { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
