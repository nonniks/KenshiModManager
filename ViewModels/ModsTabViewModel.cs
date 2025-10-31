using KenshiLib.Core;
using KenshiModManager.Commands;
using KenshiModManager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable
namespace KenshiModManager.ViewModels;

public class ModsTabViewModel : ViewModelBase
{
  private readonly ModManager _modManager;
  private Action? _savePlaysetCallback;
  private ModInfo? _selectedAvailableMod;
  private ModInfo? _selectedPlaysetMod;
  private bool _isLoading;
  private string _searchText = string.Empty;
  private ObservableCollection<ModInfo> _filteredPlaysetMods;
  private bool _isUpdatingLoadOrders = false;
  private bool _isLoadingPlayset = false;

  public ModsTabViewModel(ModManager modManager)
  {
    this._modManager = modManager ?? throw new ArgumentNullException(nameof (modManager));
    this.AllMods = new ObservableCollection<ModInfo>();
    this.ActivePlaysetMods = new ObservableCollection<ModInfo>();
    this._filteredPlaysetMods = new ObservableCollection<ModInfo>();
    this.EnableModCommand = (ICommand) new RelayCommand(new Action(this.EnableSelectedMod), new Func<bool>(this.CanEnableMod));
    this.DisableModCommand = (ICommand) new RelayCommand(new Action(this.DisableSelectedMod), new Func<bool>(this.CanDisableMod));
    this.MoveModUpCommand = (ICommand) new RelayCommand(new Action(this.MoveSelectedModUp), new Func<bool>(this.CanMoveModUp));
    this.MoveModDownCommand = (ICommand) new RelayCommand(new Action(this.MoveSelectedModDown), new Func<bool>(this.CanMoveModDown));
    this.RefreshCommand = (ICommand) new AsyncRelayCommand(new Func<Task>(this.LoadModsAsync));
    this.OpenAddModsWindowCommand = (ICommand) new RelayCommand(new Action(this.OpenAddModsWindow));
    this.RemoveModCommand = (ICommand) new AsyncRelayCommand<ModInfo>(new Func<ModInfo, Task>(this.RemoveModFromPlaysetAsync));
    this.OpenInWorkshopCommand = (ICommand) new AsyncRelayCommand<ModInfo>(new Func<ModInfo, Task>(this.OpenModInWorkshopAsync));
    this.ShowInFolderCommand = (ICommand) new AsyncRelayCommand<ModInfo>(new Func<ModInfo, Task>(this.ShowModInFolderAsync));
  }

  public ObservableCollection<ModInfo> AllMods { get; }

  public ObservableCollection<ModInfo> ActivePlaysetMods { get; }

  public ObservableCollection<ModInfo> FilteredPlaysetMods
  {
    get => this._filteredPlaysetMods;
    set
    {
      this.SetProperty<ObservableCollection<ModInfo>>(ref this._filteredPlaysetMods, value, nameof (FilteredPlaysetMods));
    }
  }

  public string SearchText
  {
    get => this._searchText;
    set
    {
      if (!this.SetProperty<string>(ref this._searchText, value, nameof (SearchText)))
        return;
      this.ApplyFilter();
    }
  }

  public ModInfo? SelectedAvailableMod
  {
    get => this._selectedAvailableMod;
    set
    {
      if (!this.SetProperty<ModInfo>(ref this._selectedAvailableMod, value, nameof (SelectedAvailableMod)))
        return;
      ((RelayCommand) this.EnableModCommand).RaiseCanExecuteChanged();
    }
  }

  public ModInfo? SelectedPlaysetMod
  {
    get => this._selectedPlaysetMod;
    set
    {
      if (!this.SetProperty<ModInfo>(ref this._selectedPlaysetMod, value, nameof (SelectedPlaysetMod)))
        return;
      ((RelayCommand) this.DisableModCommand).RaiseCanExecuteChanged();
      ((RelayCommand) this.MoveModUpCommand).RaiseCanExecuteChanged();
      ((RelayCommand) this.MoveModDownCommand).RaiseCanExecuteChanged();
    }
  }

  public bool IsLoading
  {
    get => this._isLoading;
    set => this.SetProperty<bool>(ref this._isLoading, value, nameof (IsLoading));
  }

  public ICommand EnableModCommand { get; }

  public ICommand DisableModCommand { get; }

  public ICommand MoveModUpCommand { get; }

  public ICommand MoveModDownCommand { get; }

  public ICommand RefreshCommand { get; }

  public ICommand OpenAddModsWindowCommand { get; }

  public ICommand RemoveModCommand { get; }

  public ICommand OpenInWorkshopCommand { get; }

  public ICommand ShowInFolderCommand { get; }

  public async Task LoadModsAsync()
  {
    try
    {
      this.IsLoading = true;
      List<ModInfo> modInfoList = await this._modManager.LoadAllModsWithDetailsAsync();
      ((Collection<ModInfo>) this.AllMods).Clear();
      ((Collection<ModInfo>) this.ActivePlaysetMods).Clear();
      foreach (ModInfo modInfo in modInfoList)
      {
        modInfo.PropertyChanged += OnModInfoPropertyChanged;

        ((Collection<ModInfo>) this.AllMods).Add(modInfo);
        if (modInfo.IsEnabled)
          ((Collection<ModInfo>) this.ActivePlaysetMods).Add(modInfo);
      }
      List<ModInfo> list = Enumerable.ToList<ModInfo>((IEnumerable<ModInfo>) Enumerable.OrderBy<ModInfo, int>((IEnumerable<ModInfo>) this.ActivePlaysetMods, (Func<ModInfo, int>) (m => m.LoadOrder)));
      ((Collection<ModInfo>) this.ActivePlaysetMods).Clear();
      foreach (ModInfo modInfo in list)
        ((Collection<ModInfo>) this.ActivePlaysetMods).Add(modInfo);
      this.ApplyFilter();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error loading mods: {ex}");
    }
    finally
    {
      this.IsLoading = false;
    }
  }

  private void OnModInfoPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    // Ignore property changes while we're updating load orders programmatically
    if (_isUpdatingLoadOrders)
      return;

    // Ignore property changes while loading playset from file
    if (_isLoadingPlayset)
      return;

    if (sender is ModInfo modInfo)
    {
      if (e.PropertyName == nameof(ModInfo.IsEnabled))
      {
        Console.WriteLine($"[ModsTabViewModel] Mod '{modInfo.Name}' IsEnabled changed to {modInfo.IsEnabled}");

        _isUpdatingLoadOrders = true;
        try
        {
          if (modInfo.IsEnabled && !((Collection<ModInfo>)this.ActivePlaysetMods).Contains(modInfo))
          {
            modInfo.LoadOrder = ((Collection<ModInfo>)this.ActivePlaysetMods).Count;
            ((Collection<ModInfo>)this.ActivePlaysetMods).Add(modInfo);
            this.ApplyFilter();
          }
          // IMPORTANT: Do NOT remove mod from list when disabled!
          // Mod stays in ActivePlaysetMods with IsEnabled = false
          // On save it will be commented (#modName) in playset file

          this.SavePlaysetOrder();
        }
        finally
        {
          _isUpdatingLoadOrders = false;
        }
      }
      else if (e.PropertyName == nameof(ModInfo.LoadOrder))
      {
        Console.WriteLine($"[ModsTabViewModel] Mod '{modInfo.Name}' LoadOrder manually changed to {modInfo.LoadOrder}");

        _isUpdatingLoadOrders = true;
        try
        {
          if (((Collection<ModInfo>)this.ActivePlaysetMods).Contains(modInfo))
          {
            int currentIndex = ((Collection<ModInfo>)this.ActivePlaysetMods).IndexOf(modInfo);
            int newIndex = modInfo.LoadOrder;

            if (newIndex < 0) newIndex = 0;
            if (newIndex >= ((Collection<ModInfo>)this.ActivePlaysetMods).Count)
              newIndex = ((Collection<ModInfo>)this.ActivePlaysetMods).Count - 1;

            if (currentIndex != newIndex)
            {
              ((Collection<ModInfo>)this.ActivePlaysetMods).RemoveAt(currentIndex);

              ((Collection<ModInfo>)this.ActivePlaysetMods).Insert(newIndex, modInfo);

              for (int i = 0; i < ((Collection<ModInfo>)this.ActivePlaysetMods).Count; i++)
              {
                ((Collection<ModInfo>)this.ActivePlaysetMods)[i].LoadOrder = i;
              }

              this.ApplyFilter();
              Console.WriteLine($"[ModsTabViewModel] Moved mod '{modInfo.Name}' from position {currentIndex} to {newIndex}");
            }

            this.SavePlaysetOrder();
          }
        }
        finally
        {
          _isUpdatingLoadOrders = false;
        }
      }
    }
  }

  public void SetSavePlaysetCallback(Action saveCallback)
  {
    this._savePlaysetCallback = saveCallback;
  }

  public void SetLoadingPlayset(bool isLoading)
  {
    _isLoadingPlayset = isLoading;
    Console.WriteLine($"[ModsTabViewModel] SetLoadingPlayset: {isLoading}");
  }

  public void SavePlaysetOrder()
  {
    try
    {
      Action savePlaysetCallback = this._savePlaysetCallback;
      if (savePlaysetCallback != null)
        savePlaysetCallback();
      Console.WriteLine($"[ModsTabViewModel] Saved playset with {((Collection<ModInfo>) this.ActivePlaysetMods).Count} mods");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error saving playset order: {ex}");
    }
  }

  public void ApplyFilter()
  {
    ((Collection<ModInfo>) this.FilteredPlaysetMods).Clear();
    IEnumerable<ModInfo> modInfos = Enumerable.AsEnumerable<ModInfo>((IEnumerable<ModInfo>) this.ActivePlaysetMods);
    if (!string.IsNullOrWhiteSpace(this._searchText))
    {
      string searchLower = this._searchText.ToLower();
      modInfos = Enumerable.Where<ModInfo>(modInfos, (Func<ModInfo, bool>) (m =>
      {
        if (m.Name.ToLower().Contains(searchLower))
          return true;
        string author = m.Author;
        return author != null && author.ToLower().Contains(searchLower);
      }));
    }
    foreach (ModInfo modInfo in (IEnumerable<ModInfo>) Enumerable.OrderBy<ModInfo, int>(modInfos, (Func<ModInfo, int>) (m => m.LoadOrder)))
      ((Collection<ModInfo>) this.FilteredPlaysetMods).Add(modInfo);
  }

  private bool CanEnableMod()
  {
    return this._selectedAvailableMod != null && !this._selectedAvailableMod.IsEnabled;
  }

  private void EnableSelectedMod()
  {
    if (this._selectedAvailableMod == null)
      return;
    try
    {
      int count = ((Collection<ModInfo>) this.ActivePlaysetMods).Count;
      this._selectedAvailableMod.IsEnabled = true;
      this._selectedAvailableMod.LoadOrder = count;
      ((Collection<ModInfo>) this.ActivePlaysetMods).Add(this._selectedAvailableMod);
      this.SavePlaysetOrder();
      ((RelayCommand) this.EnableModCommand).RaiseCanExecuteChanged();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error enabling mod: {ex}");
    }
  }

  private bool CanDisableMod()
  {
    return this._selectedPlaysetMod != null && this._selectedPlaysetMod.IsEnabled;
  }

  private void DisableSelectedMod()
  {
    if (this._selectedPlaysetMod == null)
      return;
    try
    {
      this._selectedPlaysetMod.IsEnabled = false;
      this._selectedPlaysetMod.LoadOrder = -1;
      ((Collection<ModInfo>) this.ActivePlaysetMods).Remove(this._selectedPlaysetMod);
      for (int index = 0; index < ((Collection<ModInfo>) this.ActivePlaysetMods).Count; ++index)
        ((Collection<ModInfo>) this.ActivePlaysetMods)[index].LoadOrder = index;
      this.SavePlaysetOrder();
      ((RelayCommand) this.DisableModCommand).RaiseCanExecuteChanged();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error disabling mod: {ex}");
    }
  }

  private bool CanMoveModUp()
  {
    return this._selectedPlaysetMod != null && this._selectedPlaysetMod.IsEnabled && this._selectedPlaysetMod.LoadOrder > 0;
  }

  private void MoveSelectedModUp()
  {
    if (this._selectedPlaysetMod == null)
      return;
    try
    {
      int num = ((Collection<ModInfo>) this.ActivePlaysetMods).IndexOf(this._selectedPlaysetMod);
      if (num > 0)
      {
        this.ActivePlaysetMods.Move(num, num - 1);
        ((Collection<ModInfo>) this.ActivePlaysetMods)[num - 1].LoadOrder = num - 1;
        ((Collection<ModInfo>) this.ActivePlaysetMods)[num].LoadOrder = num;
        this.SavePlaysetOrder();
      }
      ((RelayCommand) this.MoveModUpCommand).RaiseCanExecuteChanged();
      ((RelayCommand) this.MoveModDownCommand).RaiseCanExecuteChanged();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error moving mod up: {ex}");
    }
  }

  private bool CanMoveModDown()
  {
    return this._selectedPlaysetMod != null && this._selectedPlaysetMod.IsEnabled && this._selectedPlaysetMod.LoadOrder < ((Collection<ModInfo>) this.ActivePlaysetMods).Count - 1;
  }

  private void MoveSelectedModDown()
  {
    if (this._selectedPlaysetMod == null)
      return;
    try
    {
      int num = ((Collection<ModInfo>) this.ActivePlaysetMods).IndexOf(this._selectedPlaysetMod);
      if (num < ((Collection<ModInfo>) this.ActivePlaysetMods).Count - 1)
      {
        this.ActivePlaysetMods.Move(num, num + 1);
        ((Collection<ModInfo>) this.ActivePlaysetMods)[num].LoadOrder = num;
        ((Collection<ModInfo>) this.ActivePlaysetMods)[num + 1].LoadOrder = num + 1;
        this.SavePlaysetOrder();
      }
      ((RelayCommand) this.MoveModUpCommand).RaiseCanExecuteChanged();
      ((RelayCommand) this.MoveModDownCommand).RaiseCanExecuteChanged();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ModsTabViewModel] Error moving mod down: {ex}");
    }
  }

  private void OpenAddModsWindow()
  {
    AddModsWindow addModsWindow = new AddModsWindow();
    addModsWindow.DataContext = (object) new AddModsViewModel(this.AllMods, this.ActivePlaysetMods, new Action(this.SavePlaysetOrder));
    if (!addModsWindow.ShowDialog().GetValueOrDefault())
      return;
    this.ApplyFilter();
  }

  private async Task RemoveModFromPlaysetAsync(ModInfo? mod)
  {
    if (mod == null)
      return;
    try
    {
      ((Collection<ModInfo>) this.ActivePlaysetMods).Remove(mod);
      for (int index = 0; index < ((Collection<ModInfo>) this.ActivePlaysetMods).Count; ++index)
        ((Collection<ModInfo>) this.ActivePlaysetMods)[index].LoadOrder = index;
      this.ApplyFilter();
      mod.IsEnabled = false;
      mod.LoadOrder = -1;
      this.SavePlaysetOrder();
      Console.WriteLine("[ModsTabViewModel] Removed mod from playset: " + mod.Name);
    }
    catch (Exception ex)
    {
      Console.WriteLine("[ModsTabViewModel] Error removing mod: " + ex.Message);
    }
    await Task.CompletedTask;
  }

  private async Task OpenModInWorkshopAsync(ModInfo? mod)
  {
    if (mod == null)
      return;
    try
    {
      Console.WriteLine("[ModsTabViewModel] Attempting to open workshop page for mod: " + mod.Name);
      Console.WriteLine($"[ModsTabViewModel] WorkshopId: {mod.WorkshopId}, InWorkshop: {mod.InWorkshop}");
      if (mod.WorkshopId > 0L)
      {
        string str = $"https://steamcommunity.com/sharedfiles/filedetails/?id={mod.WorkshopId}";
        Process.Start(new ProcessStartInfo()
        {
          FileName = str,
          UseShellExecute = true
        });
        Console.WriteLine("[ModsTabViewModel] Successfully opened workshop page: " + str);
      }
      else
        Console.WriteLine($"[ModsTabViewModel] Mod '{mod.Name}' is not from Steam Workshop (WorkshopId: {mod.WorkshopId})");
    }
    catch (Exception ex)
    {
      Console.WriteLine("[ModsTabViewModel] Error opening workshop page: " + ex.Message);
    }
    await Task.CompletedTask;
  }

  private async Task ShowModInFolderAsync(ModInfo? mod)
  {
    if (mod == null)
      Console.WriteLine("[ModsTabViewModel] ShowModInFolder: mod is null");
    else if (string.IsNullOrEmpty(mod.FilePath))
    {
      Console.WriteLine($"[ModsTabViewModel] ShowModInFolder: FilePath is empty for mod '{mod.Name}'");
    }
    else
    {
      try
      {
        Console.WriteLine("[ModsTabViewModel] Attempting to show mod in folder: " + mod.Name);
        Console.WriteLine("[ModsTabViewModel] FilePath: " + mod.FilePath);
        if (!File.Exists(mod.FilePath))
        {
          Console.WriteLine("[ModsTabViewModel] ERROR: File does not exist: " + mod.FilePath);
          return;
        }
        string directoryName = Path.GetDirectoryName(mod.FilePath);
        Console.WriteLine("[ModsTabViewModel] Directory: " + directoryName);
        if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
        {
          Process.Start(new ProcessStartInfo()
          {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{mod.FilePath}\"",
            UseShellExecute = true
          });
          Console.WriteLine("[ModsTabViewModel] Successfully opened folder for mod: " + mod.Name);
        }
        else
          Console.WriteLine("[ModsTabViewModel] ERROR: Directory does not exist or is empty: " + directoryName);
      }
      catch (Exception ex)
      {
        Console.WriteLine("[ModsTabViewModel] Error showing mod in folder: " + ex.Message);
        Console.WriteLine("[ModsTabViewModel] Stack trace: " + ex.StackTrace);
      }
    }
    await Task.CompletedTask;
  }

}
