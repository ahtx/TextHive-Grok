using System.Collections.ObjectModel;
using System.Windows.Input;
using HandyControl.Tools.Command;
using System.IO;
using Microsoft.Win32;
using System.Text.Json;

namespace TextHiveGrok.ViewModels
{
    public class ConfigureViewModel : ViewModelBase
    {
        private static string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.CONFIG_FILE_PATH);

        private ObservableCollection<string> _folders;
        private ObservableCollection<string> _extensions;
        private string? _selectedFolder;
        private string? _selectedExtension;

        public ICommand AddFolderCommand { get; }
        public ICommand RemoveFolderCommand { get; }
        public ICommand AddExtensionCommand { get; }
        public ICommand RemoveExtensionCommand { get; }

        public ConfigureViewModel()
        {
            _folders = [];
            _extensions = [];

            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(configPath));
                if (config != null)
                {
                    _folders = [.. config.GetValueOrDefault("Folders", [])];
                    _extensions = [.. config.GetValueOrDefault("Extensions", [".txt"])];
                }
            }

            AddFolderCommand = new RelayCommand(_ => AddFolder());
            RemoveFolderCommand = new RelayCommand(_ => RemoveFolder(), _ => SelectedFolder != null);
            AddExtensionCommand = new RelayCommand(_ => AddExtension());
            RemoveExtensionCommand = new RelayCommand(_ => RemoveExtension(), _ => SelectedExtension != null);
        }

        public ObservableCollection<string> Folders
        {
            get => _folders;
            set
            {
                _folders = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Extensions
        {
            get => _extensions;
            set
            {
                _extensions = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedExtension
        {
            get => _selectedExtension;
            set
            {
                _selectedExtension = value;
                OnPropertyChanged();
            }
        }



        private void AddFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select a folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FolderName;
                Folders.Add(selectedPath);
            }
        }

        private void RemoveFolder()
        {
            if (SelectedFolder != null)
            {
                Folders.Remove(SelectedFolder);
            }
        }

        private void AddExtension()
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter file extension (e.g., .md):", "Add Extension");
            if (!string.IsNullOrEmpty(input) && input.StartsWith(".") && !Extensions.Contains(input))
            {
                Extensions.Add(input);
            }
        }

        private void RemoveExtension()
        {
            if (SelectedExtension != null)
            {
                Extensions.Remove(SelectedExtension);
            }
        }

        public void SaveConfiguration()
        {
            var config = new { Folders, Extensions };
            File.WriteAllText(configPath, JsonSerializer.Serialize(config));
        }
    }
}