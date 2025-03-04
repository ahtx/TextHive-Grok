using System.Windows;

namespace TextHiveGrok
{
    public partial class CustomMessageBox : Window
    {
        public string Message { get; set; }

        private CustomMessageBox(string message, string title, Window owner)
        {
            InitializeComponent();
            DataContext = this;
            Message = message;
            Title = title;
            Owner = owner;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public static void Show(string message, string title, Window window)
        {
            var messageBox = new CustomMessageBox(message, title, window);
            messageBox.ShowDialog();
        }
    }
}