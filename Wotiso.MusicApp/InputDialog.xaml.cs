using System.Windows;

namespace Wotiso.MusicApp
{
    /// <summary>
    /// InputDialog - Simple dialog to get text input from user
    /// </summary>
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; }

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            TitleText.Text = title;
            PromptText.Text = prompt;
            InputTextBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
