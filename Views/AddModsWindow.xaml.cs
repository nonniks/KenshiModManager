using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KenshiModManager.ViewModels;

namespace KenshiModManager.Views
{
    /// <summary>
    /// Interaction logic for AddModsWindow.xaml
    /// </summary>
    public partial class AddModsWindow : Window
    {
        public AddModsWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ModsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ModSelectionItem selectedItem)
            {
                selectedItem.IsSelected = !selectedItem.IsSelected;
            }
        }
    }
}
