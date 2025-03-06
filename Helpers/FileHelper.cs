using System.IO;
using System.Text.Json;
using TextHiveGrok.Models;

namespace TextHiveGrok.Helpers
{
    class FileHelper
    {
        private static string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.CONFIG_FILE_PATH);
        private static Dictionary<string, string> fileContents = [];
        private static List<string> folders = [];
        private static List<string> extensions = [];

        static FileHelper()
        {
            LoadConfiguration();
        }
        public static void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var config = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                        File.ReadAllText(configPath));
                    if (config != null)
                    {
                        folders = config.GetValueOrDefault("Folders", []);
                        extensions = config.GetValueOrDefault("Extensions", [".txt"]);
                    }
                }
            }
            catch (Exception)
            {
                folders = [];
                extensions = [];
            }
        }

        public static FileItem? CreateNewFile(string fileName)
        {
            if (folders.Count == 0)
            {
                throw new InvalidOperationException("Please add a folder first.");
            }

            var newFileName = Path.Combine(folders[0], fileName);
            if (!File.Exists(newFileName))
            {
                File.WriteAllText(newFileName, string.Empty);
                fileContents[newFileName] = string.Empty;
                return new FileItem
                {
                    FileName = Path.GetFileName(newFileName),
                    FullPath = newFileName,
                    Size = "0 B",
                    Modified = DateTime.Now
                };
            }
            return null;
        }

        public static List<FileItem> LoadFiles()
        {
            var files = new List<FileItem>();
            fileContents.Clear();

            folders.ForEach(folder =>
            {
                try
                {
                    foreach (var ext in extensions)
                    {
                        foreach (var file in Directory.GetFiles(folder, $"*{ext}", SearchOption.AllDirectories))
                        {

                            var fileInfo = new FileInfo(file);
                            fileContents[file] = File.ReadAllTextAsync(file).Result;
                            files.Add(new FileItem
                            {
                                FileName = Path.GetFileName(file),
                                FullPath = file,
                                Size = FormatFileSize(fileInfo.Length),
                                Modified = fileInfo.LastWriteTime
                            });

                        }
                    }
                }
                catch(Exception)
                {
                    CustomMessageBox.Show($"Unable to read the files from folder: {folder}", "Error");
                }
            });

            return files;
        }

        public static string GetFileContent(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file at '{filePath}' was not found.", filePath);
            }

            return File.ReadAllText(filePath);
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

        public static List<FileItem> SearchFiles(string searchTerm)
        {
            var results = new List<FileItem>();
            foreach (var file in fileContents.Keys)
            {
                var content = fileContents[file];
                var fileInfo = new FileInfo(file);

                if (string.IsNullOrEmpty(searchTerm) ||
                    content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(file).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new FileItem
                    {
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        Size = FormatFileSize(fileInfo.Length),
                        Modified = fileInfo.LastWriteTime
                    });
                }
            }
            return results;
        }

        public static List<RelatedFile> GetRelatedFiles(string searchTerm)
        {
            var relatedFiles = new List<RelatedFile>();
            if (string.IsNullOrEmpty(searchTerm)) return relatedFiles;

            var wordFrequencies = new Dictionary<string, int>();
            foreach (var file in fileContents.Keys)
            {
                var content = fileContents[file];
                if (!string.IsNullOrEmpty(content) && content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 3 && !FileHelper.IsCommonWord(w.ToLower()))
                        .GroupBy(w => w.ToLower())
                        .Select(g => new { Word = g.Key, Count = g.Count() })
                        .OrderBy(w => w.Count);

                    foreach (var word in words.Take(10))
                    {
                        wordFrequencies[word.Word] = wordFrequencies.GetValueOrDefault(word.Word, 0) + 1;
                    }
                }
            }

            var clusters = new Dictionary<string, List<string>>();
            foreach (var file in fileContents.Keys)
            {
                var content = fileContents[file];
                if (!string.IsNullOrEmpty(content) && content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    var leastFrequentWords = FileHelper.GetImportantWords(content, 10, searchTerm);
                    foreach (var word in leastFrequentWords)
                    {
                        if (wordFrequencies.ContainsKey(word) && wordFrequencies[word] > 1)
                        {
                            if (!clusters.ContainsKey(word))
                                clusters[word] = new List<string>();
                            clusters[word].Add(file);
                        }
                    }
                }
            }

            foreach (var cluster in clusters.OrderBy(c => c.Key))
            {
                var relatedWords = cluster.Value
                    .Select(f => new
                    {
                        FileName = Path.GetFileName(f),
                        Words = GetImportantWords(fileContents[f], 6, searchTerm)
                    })
                    .Where(x => !string.IsNullOrEmpty(x.FileName))
                    .Take(6);

                foreach (var item in relatedWords)
                {
                    relatedFiles.Add(new RelatedFile
                    {
                        FileName = item.FileName,
                        RelatedWords = string.Join(", ", item.Words)
                    });
                }
            }

            return relatedFiles;
        }

        public static List<string> GetImportantWords(string content, int count, string excludeWord)
        {
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !IsCommonWord(w.ToLower()) &&
                       !w.Equals(excludeWord, StringComparison.OrdinalIgnoreCase))
                .GroupBy(w => w.ToLower())
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .OrderByDescending(w => w.Count)
                .Take(count)
                .Select(w => w.Word)
                .ToList();

            return words.Count != 0 ? words : ["No related words found"];
        }

        public static bool IsCommonWord(string word)
        {
            var stopwords = new HashSet<string>
            {
                "the",
                "is",
                "and",
                "in",
                "it",
                "of",
                "to",
                "for",
                "with",
                "on",
                "at",
                "by",
                "from",
                "up",
                "about",
                "into",
                "over",
                "after"
            };
            return stopwords.Contains(word);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }

    }
}
