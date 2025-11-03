using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            ModsListBox.PreviewMouseWheel += ModsListBox_PreviewMouseWheel;
        }

        private void ModsListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var scrollViewer = FindParentScrollViewer(listBox);
                if (scrollViewer != null)
                {
                    if (e.Delta > 0)
                    {
                        scrollViewer.LineUp();
                        scrollViewer.LineUp();
                        scrollViewer.LineUp();
                    }
                    else
                    {
                        scrollViewer.LineDown();
                        scrollViewer.LineDown();
                        scrollViewer.LineDown();
                    }

                    e.Handled = true;
                }
            }
        }

        private ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
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
