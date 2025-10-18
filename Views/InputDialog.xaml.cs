using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KenshiModManager.Views
{
    /// <summary>
    /// Simple input dialog for getting text from user
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// The text entered by the user
        /// </summary>
        public string InputText { get; private set; } = string.Empty;

        /// <summary>
        /// Create a new input dialog
        /// </summary>
        /// <param name="title">Window title</param>
        /// <param name="prompt">Prompt text shown to user</param>
        /// <param name="defaultValue">Default value in text box</param>
        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            Title = title;
            PromptTextBlock.Text = prompt;
            InputTextBox.Text = defaultValue;

            // Select all text when dialog opens
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                InputText = InputTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }

        private bool ValidateInput()
        {
            string input = InputTextBox.Text.Trim();

            // Check if empty
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show(
                    "Name cannot be empty.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // Check for invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            if (input.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show(
                    $"Name contains invalid characters.\nInvalid characters: {string.Join(" ", invalidChars.Select(c => $"'{c}'"))}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}
