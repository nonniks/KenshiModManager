using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private readonly Action _closeWindowAction;

        private string _searchText = string.Empty;
        private string _sortBy = "Name";
        private ObservableCollection<ModSelectionItem> _availableMods;
        private ObservableCollection<ModSelectionItem> _filteredMods;
        private bool _isLoading;

        public AddModsViewModel(
            ObservableCollection<ModInfo> allMods,
            ObservableCollection<ModInfo> activePlaysetMods,
            Action savePlaysetCallback,
            Action closeWindowAction)
        {
            _allMods = allMods ?? throw new ArgumentNullException(nameof(allMods));
            _activePlaysetMods = activePlaysetMods ?? throw new ArgumentNullException(nameof(activePlaysetMods));
            _savePlaysetCallback = savePlaysetCallback ?? throw new ArgumentNullException(nameof(savePlaysetCallback));
            _closeWindowAction = closeWindowAction ?? throw new ArgumentNullException(nameof(closeWindowAction));

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
        public string SelectedCountText => string.Format(
            Properties.Resources.AddModsWindow_SelectedCount,
            SelectedCount,
            TotalCount);

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

                // Unsubscribe from old items to prevent memory leaks
                foreach (var item in _availableMods)
                {
                    item.PropertyChanged -= OnModSelectionChanged;
                }

                _availableMods.Clear();

                var activeMods = _activePlaysetMods.Select(m => m.Name).ToHashSet();

                var modsToAdd = _allMods.Where(m => !activeMods.Contains(m.Name)).ToList();

                foreach (var mod in modsToAdd)
                {
                    var item = new ModSelectionItem(mod);
                    item.PropertyChanged += OnModSelectionChanged;
                    _availableMods.Add(item);
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

        private void OnModSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModSelectionItem.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(SelectedCountText));
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
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(SelectedCountText));
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
            OnPropertyChanged(nameof(SelectedCountText));
        }

        private void DeselectAll()
        {
            foreach (var mod in FilteredMods)
            {
                mod.IsSelected = false;
            }
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
        }

        private bool CanAddSelectedMods()
        {
            return _availableMods.Any(m => m.IsSelected);
        }

        private void AddSelectedMods()
        {
            try
            {
                Console.WriteLine($"[AddModsViewModel] === AddSelectedMods called ===");
                Console.WriteLine($"[AddModsViewModel] Total available mods: {_availableMods.Count}");
                Console.WriteLine($"[AddModsViewModel] Current active playset mods: {_activePlaysetMods.Count}");

                var selectedMods = _availableMods.Where(m => m.IsSelected).Select(m => m.ModInfo).ToList();

                Console.WriteLine($"[AddModsViewModel] Selected mods count: {selectedMods.Count}");
                Console.WriteLine($"[AddModsViewModel] Selected mod names: {string.Join(", ", selectedMods.Select(m => m.Name))}");

                if (selectedMods.Count == 0)
                {
                    Console.WriteLine("[AddModsViewModel] WARNING: No mods selected! User needs to check checkboxes.");
                    return;
                }

                // Check for duplicates before adding
                var existingModNames = _activePlaysetMods.Select(m => m.Name).ToHashSet();
                var duplicates = selectedMods.Where(m => existingModNames.Contains(m.Name)).ToList();

                if (duplicates.Count > 0)
                {
                    Console.WriteLine($"[AddModsViewModel] WARNING: Found {duplicates.Count} duplicates in playset BEFORE adding:");
                    foreach (var dup in duplicates)
                    {
                        Console.WriteLine($"[AddModsViewModel]   - Duplicate: {dup.Name}");
                    }
                }

                foreach (var mod in selectedMods)
                {
                    // Skip if already in playset
                    if (existingModNames.Contains(mod.Name))
                    {
                        Console.WriteLine($"[AddModsViewModel] SKIPPING duplicate mod: {mod.Name}");
                        continue;
                    }

                    Console.WriteLine($"[AddModsViewModel] Adding mod: {mod.Name}");

                    int newPosition = _activePlaysetMods.Count;

                    // FIRST add to collection, THEN set properties to avoid triggering duplicate add in OnModInfoPropertyChanged
                    _activePlaysetMods.Add(mod);
                    existingModNames.Add(mod.Name); // Track added mods

                    // Set properties AFTER adding (PropertyChanged events will be ignored if mod is already in collection)
                    mod.IsEnabled = true;
                    mod.LoadOrder = newPosition;
                }

                _savePlaysetCallback();

                // Close window after successful add
                _closeWindowAction?.Invoke();

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
