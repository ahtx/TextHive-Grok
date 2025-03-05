using System.Collections.ObjectModel;
using System.Diagnostics;
using TextHiveGrok.Models;
using System.IO;
using TextHiveGrok.Helpers;

namespace TextHiveGrok.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private FileItem? _selectedFile;
        private string _previewText = string.Empty;
        private string _statusText = "Ready";
        private string _currentFilePath = string.Empty;

        public ObservableCollection<FileItem> Files { get; } = new();
        public ObservableCollection<RelatedFile> RelatedFiles { get; } = new();


        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
            }
        }

        public string PreviewText
        {
            get => _previewText;
            set
            {
                SetProperty(ref _previewText, value);
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public FileItem? SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    LoadFilePreview(value);
                }
            }
        }

        public MainWindowViewModel()
        {
            LoadFiles();

        }

        public void LoadFiles()
        {
            StatusText = "Loading files...";
            FileHelper.LoadConfiguration();
            var files = FileHelper.LoadFiles();
            Files.Clear();
            foreach (var file in files)
            {
                Files.Add(file);
            }
            StatusText = $"Loaded {files.Count} files";
        }

        public void PerformSearch()
        {
            var stopwatch = Stopwatch.StartNew();
            var searchResults = FileHelper.SearchFiles(SearchText);
            Files.Clear();
            foreach (var file in searchResults)
            {
                Files.Add(file);
            }

            stopwatch.Stop();
            StatusText = $"Found {searchResults.Count} files in {stopwatch.ElapsedMilliseconds}ms";
            UpdateRelatedFiles();
        }

        private async void LoadFilePreview(FileItem? file)
        {
            if (file == null)
            {
                return;
            }
            PreviewText = await FileHelper.GetFileContentAsync(file.FullPath);
            CurrentFilePath = file.FullPath;
            UpdateRelatedFiles();
        }

        private void UpdateRelatedFiles()
        {
            var related = FileHelper.GetRelatedFiles(SearchText);
            RelatedFiles.Clear();
            foreach (var file in related)
            {
                RelatedFiles.Add(file);
            }
        }

        public static void SaveFile(string path, string content)
        {
            try
            {
                using (StreamWriter writer = new(path, false))
                {
                    writer.Write(content);
                }
                FileHelper.LoadFiles();
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save file: {path}", ex);
            }
        }

        public static FileItem? CreateFile(string fileName)
        {
            var file = FileHelper.CreateNewFile(fileName);
            return file;
        }

        public static FileItem? CreateTodaysNoteFile()
        {
            var fileName = $"{DateTime.Now:MM-dd-yy}Notes.txt";
            return FileHelper.CreateNewFile(fileName);
        }

        public bool SaveCurrentFile()
        {
            if (SelectedFile != null && !string.IsNullOrEmpty(PreviewText))
            {
                SaveFile(SelectedFile.FullPath, PreviewText);
                return true;
            }
            return false;
        }
    }
}