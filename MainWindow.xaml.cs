using System.Windows;
using KenshiModManager.ViewModels;

namespace KenshiModManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SettingsTabViewModel.ValidateAllPaths();

            // DON'T call RefreshModsCommand here - it will reset ActivePlaysetMods!
            // The playset is already loaded in MainViewModel constructor -> LoadPlaysets() -> SwitchToPlaysetAsync()
            // RefreshModsCommand should only be called manually by user clicking "Refresh" button

            System.Console.WriteLine("[MainWindow] Skipping automatic RefreshModsCommand - playset already loaded");
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // Save current playset before closing (but NOT to mods.cfg - only to playset file)
            if (DataContext is MainViewModel viewModel)
            {
                // This will save to the playset file, not mods.cfg
                System.Console.WriteLine("[MainWindow] Saving current playset before closing");
                viewModel.SaveCurrentPlaysetCommand?.Execute(null);
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[MainWindow] Error saving playset on close: {ex.Message}");
        }
    }

    private void PlaysetOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[MainWindow] Error opening playset options menu: {ex.Message}");
        }
    }
}