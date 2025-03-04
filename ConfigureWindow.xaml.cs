using System.Windows;
using TextHiveGrok.ViewModels;

namespace TextHiveGrok.Views
{
    public partial class ConfigureWindow : Window
    {
        private ConfigureViewModel _vm = new ConfigureViewModel();
        public ConfigureWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.SaveConfiguration();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error saving configuration: {ex.Message}", "Error", this);
            }
        }
    }
}