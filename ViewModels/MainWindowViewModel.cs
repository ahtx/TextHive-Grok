using System.Collections.ObjectModel;
using System.Diagnostics;
using TextHiveGrok.Models;
using System.IO;
using TextHiveGrok.Helpers;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace TextHiveGrok.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private FileItem? _selectedFile;
        private TextDocument _previewDocument;
        private string _statusText = "Ready";
        private string _currentFilePath = string.Empty;
        private IHighlightingDefinition? _currentSyntaxHighlighting;

        public ObservableCollection<FileItem> Files { get; } = [];
        public ObservableCollection<RelatedFile> RelatedFiles { get; } = [];


        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
            }
        }

        public TextDocument PreviewDocument
        {
            get => _previewDocument;
            private set => SetProperty(ref _previewDocument, value);
        }

        public IHighlightingDefinition? CurrentSyntaxHighlighting
        {
            get => _currentSyntaxHighlighting;
            private set => SetProperty(ref _currentSyntaxHighlighting, value);
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
            _previewDocument = new TextDocument();
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

        private void LoadFilePreview(FileItem? file)
        {
            if (file == null)
            {
                return;
            }
            var content = FileHelper.GetFileContent(file.FullPath);
            _previewDocument.Text = content;
            CurrentFilePath = file.FullPath;
            UpdateSyntaxHighlighting(file.FullPath);
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
            var currentFileContent = FileHelper.GetFileContent(SelectedFile!.FullPath);
            if (currentFileContent == PreviewDocument.Text)
            {
                return false;
            }
            if (SelectedFile != null && _previewDocument != null)
            {
                FileHelper.SaveFile(SelectedFile.FullPath, _previewDocument.Text);
                return true;
            }
            return false;
        }

        private void UpdateSyntaxHighlighting(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            CurrentSyntaxHighlighting = extension switch
            {
                ".cs" => HighlightingManager.Instance.GetDefinition("C#"),
                ".cpp" => HighlightingManager.Instance.GetDefinition("C++"),
                ".xml" or ".xaml" => HighlightingManager.Instance.GetDefinition("XML"),
                ".json" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".js" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".java" => HighlightingManager.Instance.GetDefinition("Java"),
                ".html" or ".htm" => HighlightingManager.Instance.GetDefinition("HTML"),
                ".css" => HighlightingManager.Instance.GetDefinition("CSS"),
                ".py" => HighlightingManager.Instance.GetDefinition("Python"),
                ".vb" => HighlightingManager.Instance.GetDefinition("Visual Basic"),
                ".php" => HighlightingManager.Instance.GetDefinition("PHP"),
                _ => HighlightingManager.Instance.GetDefinition("Text")
            };
        }
    }
}