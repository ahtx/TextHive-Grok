using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration; // For app settings (though we’ll switch to JSON)
using System.Xml;
using System.Text.Json; // For JSON serialization

namespace TextHiveGrok
{
    public class MainForm : Form
    {
        private TextBox? searchBox;
        private Button? searchButton;
        private Button? clearButton;
        private ListView? fileList;
        private RichTextBox? previewBox;
        private ListView? relatedFilesView;
        private Label? currentFileLabel;
        internal List<string> folders = new List<string>();
        internal List<string> extensions = new List<string> { ".txt" }; // Default to .txt
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();
        private MenuStrip? menuStrip;
        private ToolStripMenuItem? configureMenu;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;
        private string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextFileOrganizer.json"); // JSON config path

        public MainForm()
        {
            // Form setup
            Text = "TextOrganizer by Amir Husain";
            Size = new Size(1000, 700);
            MinimumSize = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            // Default Windows look and feel (no custom colors, using system theme)
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = SystemColors.Control; // Default Windows background

            // Initialize controls before loading configuration or files
            InitializeComponents();

            // Load saved configuration (folders and extensions)
            LoadConfiguration();

            LoadFilesAsync();
        }

        private void InitializeComponents()
        {
            // Menu Strip
            menuStrip = new MenuStrip
            {
                BackColor = SystemColors.Menu, // Default menu color
                ForeColor = SystemColors.MenuText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            var fileMenu = new ToolStripMenuItem("File");
            var newMenuItem = new ToolStripMenuItem("New", null, NewFile)
            {
                ShortcutKeys = Keys.Control | Keys.N
            };
            var createNoteMenuItem = new ToolStripMenuItem("Create Today's Note File", null, CreateTodaysNoteFile);
            var saveMenuItem = new ToolStripMenuItem("Save", null, SaveFile)
            {
                ShortcutKeys = Keys.Control | Keys.S
            };
            var exitMenuItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
            fileMenu.DropDownItems.Add(newMenuItem);
            fileMenu.DropDownItems.Add(createNoteMenuItem);
            fileMenu.DropDownItems.Add(saveMenuItem);
            fileMenu.DropDownItems.Add(exitMenuItem);
            configureMenu = new ToolStripMenuItem("Configure", null, ConfigureMenu_Click)
            {
                ForeColor = SystemColors.MenuText,
                BackColor = SystemColors.Menu
            };
            var aboutMenuItem = new ToolStripMenuItem("About", null, ShowAboutBox);
            menuStrip?.Items.Add(fileMenu);
            menuStrip?.Items.Add(configureMenu!);
            menuStrip?.Items.Add(aboutMenuItem);
            Controls.Add(menuStrip!);


            // Left panel (search, file list)
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Padding = new Padding(0, 25, 0, 0)

            };

            // Search controls (below menu, above file list)
            searchBox = new TextBox
            {
                Width = 300, // Increased width
                Height = 25,
                Left = 100,
                PlaceholderText = "Search for files...",
                BackColor = SystemColors.Window, // Default white background
                ForeColor = SystemColors.WindowText,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) UpdateFileList(searchBox.Text); };
            searchButton = new Button
            {
                Text = "🔍 Search",
                Width = 90,
                Height = 25,
                BackColor = SystemColors.ButtonFace, // Default button color
                ForeColor = SystemColors.ControlText,
                FlatStyle = FlatStyle.Standard // Use standard Windows button style
            };
            searchButton!.Click += SearchButton_Click;
            clearButton = new Button
            {
                Text = "✖ Clear",
                Width = 80,
                Height = 25,
                BackColor = SystemColors.ButtonFace, // Default button color
                ForeColor = SystemColors.ControlText,
                FlatStyle = FlatStyle.Standard // Use standard Windows button style
            };
            clearButton!.Click += (s, e) => { searchBox.Text = ""; UpdateFileList(""); };

            var searchPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,

            };
            searchPanel.Controls.Add(searchBox!);
            searchPanel.Controls.Add(searchButton!);
            searchPanel.Controls.Add(clearButton!);


            fileList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true, // Add gridlines for better visibility
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
            };
            fileList!.Columns.Add("File", 250);
            fileList!.Columns.Add("Size", 100);
            fileList!.Columns.Add("Date Modified", 150);
            fileList!.SelectedIndexChanged += FileList_SelectedIndexChanged;
            fileList!.ColumnClick += FileList_ColumnClick; // For sorting
            fileList!.DoubleClick += FileList_DoubleClick; // Load file on double-click

            var filesPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(3, 0, 0, 0)
            };
            filesPanel.Controls.Add(fileList);

            leftPanel.Controls.Add(filesPanel!);
            leftPanel.Controls.Add(searchPanel);

            var rightPanel = CreateRightPanel();

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 100,
                Panel2MinSize = 100,
                BorderStyle = BorderStyle.Fixed3D
            };

            splitContainer.Panel1.Controls.Add(leftPanel);
            splitContainer.Panel2.Controls.Add(rightPanel);

            Controls.Add(splitContainer);

            // Status strip
            statusStrip = new StatusStrip
            {
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlText,
            };
            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            if (statusStrip != null)
            {
                statusStrip.Items.Add(statusLabel!);
                Controls.Add(statusStrip!);
            }
        }

        private Panel CreateRightPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                Padding = new Padding(0, 31, 3, 0),
            };

            // Current file label
            currentFileLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            // Preview box (taller and larger, editable with scrollbars)
            previewBox = new RichTextBox
            {
                Dock = DockStyle.Fill, // Use Fill to take up more vertical space
                Height = 400, // Much taller editor for 6-8+ rows of text
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Both,

                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,

                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
            };
            previewBox!.TextChanged += PreviewBox_TextChanged; // Save on change

            var previewPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,

            };
            previewPanel.Controls.Add(previewBox);

            // Related files view (clustering, resized to 6-8 rows, sortable)
            relatedFilesView = new ListView
            {
                Dock = DockStyle.Bottom,
                Height = 150, // Reduced height for 6-8 rows (adjust based on font size)
                View = View.Details,
                FullRowSelect = true,
                GridLines = true, // Add gridlines for better visibility
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
            };


            relatedFilesView!.Columns.Add("File", 250);
            relatedFilesView!.Columns.Add("Related Words", 250);
            relatedFilesView!.ColumnClick += RelatedFilesView_ColumnClick; // For sorting
            relatedFilesView!.DoubleClick += RelatedFilesView_DoubleClick; // Load file on double-click

            var upperPanel = new Panel()
            {
                Dock = DockStyle.Fill,
            };

            upperPanel.Controls.Add(previewPanel);
            upperPanel.Controls.Add(currentFileLabel);

            panel.Controls.Add(upperPanel);
            panel.Controls.Add(relatedFilesView!);

            return panel;
        }

        private void PreviewBox_TextChanged(object? sender, EventArgs e)
        {
            if (fileList?.SelectedItems.Count > 0 && previewBox != null)
            {
                var selectedFile = fileList.SelectedItems[0].Text;
                var fullPath = fileContents.Keys.FirstOrDefault(f => Path.GetFileName(f) == selectedFile);
                if (fullPath != null)
                {
                    try
                    {
                        File.WriteAllText(fullPath, previewBox.Text);
                        fileContents[fullPath] = previewBox.Text;
                        statusLabel!.Text = $"Saved changes to {Path.GetFileName(fullPath)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            UpdateFileList(searchBox!.Text);
        }

        private void ConfigureMenu_Click(object? sender, EventArgs e)
        {
            var configForm = new ConfigureForm(this);
            configForm.ShowDialog();
        }

        private void SaveFile(object? sender, EventArgs e)
        {
            if (fileList?.SelectedItems.Count > 0)
            {
                var selectedFile = fileList.SelectedItems[0].Text;
                var fullPath = fileContents.Keys.FirstOrDefault(f => Path.GetFileName(f) == selectedFile);
                if (fullPath != null)
                {
                    File.WriteAllText(fullPath, previewBox!.Text);
                    MessageBox.Show($"Saved {Path.GetFileName(fullPath)} successfully.", "File Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void NewFile(object? sender, EventArgs e)
        {
            if (folders.Count == 0)
            {
                MessageBox.Show("Please add a folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var newFileName = Path.Combine(folders[0], "NewFile.txt");
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter file name:", "New File", "NewFile.txt");
            if (!string.IsNullOrEmpty(input))
            {
                newFileName = Path.Combine(folders[0], input);
            }

            if (!File.Exists(newFileName))
            {
                File.WriteAllText(newFileName, string.Empty);
            }
            fileContents[newFileName] = string.Empty;
            LoadFilesAsync();
            OpenFile(newFileName);
        }

        private void CreateTodaysNoteFile(object? sender, EventArgs e)
        {
            if (folders.Count == 0)
            {
                MessageBox.Show("Please add a folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var todayFileName = Path.Combine(folders[0], $"{DateTime.Now:MM-dd-yy}Notes.txt");
            if (!File.Exists(todayFileName))
            {
                File.WriteAllText(todayFileName, string.Empty);
            }
            fileContents[todayFileName] = File.ReadAllText(todayFileName);
            LoadFilesAsync();
            OpenFile(todayFileName);
        }

        private void OpenFile(string filePath)
        {
            if (fileContents.ContainsKey(filePath))
            {
                previewBox!.Text = fileContents[filePath];
                currentFileLabel!.Text = filePath;
            }
        }

        internal void AddFolder_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    if (!folders.Contains(fbd.SelectedPath))
                    {
                        folders.Add(fbd.SelectedPath);
                        SaveConfiguration();
                        LoadFilesAsync();
                    }
                }
            }
        }

        internal void AddExtension_Click(object? sender, EventArgs e)
        {
            var ext = Microsoft.VisualBasic.Interaction.InputBox("Enter file extension (e.g., .md):", "Add Extension");
            if (!string.IsNullOrEmpty(ext) && ext.StartsWith(".") && !extensions.Contains(ext))
            {
                extensions.Add(ext);
                SaveConfiguration();
                LoadFilesAsync();
            }
        }

        private async void LoadFilesAsync()
        {
            if (statusLabel == null || fileList == null) return;

            statusLabel.Text = "Loading files...";
            Enabled = false;
            fileContents.Clear();
            fileList.Items.Clear();

            await Task.Run(() =>
            {
                foreach (var folder in folders)
                {
                    foreach (var ext in extensions)
                    {
                        foreach (var file in Directory.GetFiles(folder, $"*{ext}", SearchOption.AllDirectories))
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                fileContents[file] = File.ReadAllText(file);
                                Invoke((MethodInvoker)(() =>
                                {
                                    var item = new ListViewItem(new[] { Path.GetFileName(file), FormatFileSize(fileInfo.Length), fileInfo.LastWriteTime.ToString("g") });
                                    fileList.Items.Add(item);
                                }));
                            }
                            catch (Exception ex)
                            {
                                Invoke((MethodInvoker)(() => MessageBox.Show($"Error reading {file}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                            }
                        }
                    }
                }
            });

            statusLabel.Text = $"Loaded {fileContents.Count} files";
            Enabled = true;
        }

        private string FormatFileSize(long bytes)
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

        private void FileList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (fileList?.SelectedItems.Count > 0 && searchBox != null && previewBox != null)
            {
                var selectedFile = fileList.SelectedItems[0].Text;
                var fullPath = fileContents.Keys.FirstOrDefault(f => Path.GetFileName(f) == selectedFile);
                if (fullPath != null)
                {
                    previewBox.Text = fileContents[fullPath];
                    currentFileLabel!.Text = fullPath;
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        System.Diagnostics.Process.Start("notepad.exe", fullPath);
                    }
                    UpdateRelatedFiles(searchBox.Text ?? string.Empty); // Ensure searchTerm is not null
                }
            }
        }

        private void FileList_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (fileList != null)
            {
                fileList.ListViewItemSorter = new ListViewItemComparer(e.Column, fileList.ListViewItemSorter is ListViewItemComparer comparer && comparer.Col == e.Column ? !comparer.Ascending : true);
                fileList.Sort();
            }
        }

        private void FileList_DoubleClick(object? sender, EventArgs e)
        {
            if (fileList?.SelectedItems.Count > 0 && previewBox != null)
            {
                var selectedFile = fileList.SelectedItems[0].Text;
                var fullPath = fileContents.Keys.FirstOrDefault(f => Path.GetFileName(f) == selectedFile);
                if (fullPath != null)
                {
                    previewBox.Text = fileContents[fullPath];
                    currentFileLabel!.Text = fullPath;
                }
            }
        }

        private void RelatedFilesView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (relatedFilesView != null)
            {
                relatedFilesView.ListViewItemSorter = new ListViewItemComparer(e.Column, relatedFilesView.ListViewItemSorter is ListViewItemComparer comparer && comparer.Col == e.Column ? !comparer.Ascending : true);
                relatedFilesView.Sort();
            }
        }

        private void RelatedFilesView_DoubleClick(object? sender, EventArgs e)
        {
            if (relatedFilesView?.SelectedItems.Count > 0 && previewBox != null)
            {
                var selectedFile = relatedFilesView.SelectedItems[0].Text;
                var fullPath = fileContents.Keys.FirstOrDefault(f => Path.GetFileName(f) == selectedFile);
                if (fullPath != null)
                {
                    previewBox.Text = fileContents[fullPath];
                    currentFileLabel!.Text = fullPath;
                }
            }
        }

        private void UpdateFileList(string searchTerm)
        {
            try
            {
                if (fileList == null || statusLabel == null) return;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                fileList.Items.Clear();
                int filesSearched = 0, matchesFound = 0, keywordInstances = 0;

                foreach (var file in fileContents.Keys)
                {
                    filesSearched++;
                    var content = fileContents.TryGetValue(file, out string? value) ? value : string.Empty;
                    var fileInfo = new FileInfo(file);
                    bool matches = string.IsNullOrEmpty(searchTerm) ||
                                   content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || // Case-insensitive
                                   Path.GetFileName(file).Contains(searchTerm, StringComparison.OrdinalIgnoreCase); // Include filename in search, case-insensitive
                    if (matches)
                    {
                        matchesFound++;
                        keywordInstances += CountKeywordInstances(content, searchTerm);
                        var item = new ListViewItem(new[] { Path.GetFileName(file), FormatFileSize(fileInfo.Length), fileInfo.LastWriteTime.ToString("g") });
                        fileList.Items.Add(item);
                    }
                }

                stopwatch.Stop();
                statusLabel.Text = $"Searched {filesSearched} files in {stopwatch.ElapsedMilliseconds}ms – {matchesFound} matches, {keywordInstances} instances of '{searchTerm ?? "N/A"}'";
                UpdateRelatedFiles(searchTerm ?? string.Empty); // Ensure searchTerm is not null
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int CountKeywordInstances(string content, string searchTerm)
        {
            try
            {
                return Regex.Matches(content, Regex.Escape(searchTerm), RegexOptions.IgnoreCase).Count; // Case-insensitive
            }
            catch (Exception)
            {
                return 0; // Return 0 on error to prevent crashes
            }
        }

        private void UpdateRelatedFiles(string searchTerm)
        {
            try
            {
                if (relatedFilesView == null) return;

                relatedFilesView.Items.Clear();
                if (string.IsNullOrEmpty(searchTerm)) return;

                var wordFrequencies = new Dictionary<string, int>();
                foreach (var file in fileContents.Keys)
                {
                    var content = fileContents.TryGetValue(file, out string? value) ? value : string.Empty;
                    if (!string.IsNullOrEmpty(content) && content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 3 && !IsCommonWord(w.ToLower()))
                            .GroupBy(w => w.ToLower())
                            .Select(g => new { Word = g.Key, Count = g.Count() })
                            .OrderBy(w => w.Count); // Order by least frequent
                        foreach (var word in words.Take(10)) // Take 10 least frequent words
                        {
                            wordFrequencies[word.Word] = wordFrequencies.GetValueOrDefault(word.Word, 0) + 1;
                        }
                    }
                }

                var clusters = new Dictionary<string, List<string>>();
                foreach (var file in fileContents.Keys)
                {
                    var content = fileContents.TryGetValue(file, out string? value) ? value : string.Empty;
                    if (!string.IsNullOrEmpty(content) && content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        var leastFrequentWords = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 3 && !IsCommonWord(w.ToLower()))
                            .GroupBy(w => w.ToLower())
                            .Select(g => new { Word = g.Key, Count = g.Count() })
                            .OrderBy(w => w.Count)
                            .Take(10) // Take 10 least frequent words
                            .Select(w => w.Word)
                            .ToList();

                        foreach (var word in leastFrequentWords)
                        {
                            if (wordFrequencies.ContainsKey(word) && wordFrequencies[word] > 1) // Only cluster if word appears in multiple files
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
                    var relatedWords = cluster.Value.Select(f => Path.GetFileName(f))
                        .Where(f => fileContents.Keys.Any(k => Path.GetFileName(k) == f && fileContents.TryGetValue(k, out string? content) && content?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true))
                        .Take(6) // Show up to 6 most related files
                        .Select(f =>
                        {
                            var content = fileContents.Values.FirstOrDefault(v => Path.GetFileName(fileContents.Keys.FirstOrDefault(k => v == k)) == f) ?? string.Empty;
                            return new { File = f, Words = GetImportantWords(content, 6, searchTerm) };
                        })
                        .Where(x => !string.IsNullOrEmpty(x.File)) // Ensure valid files
                        .ToList();

                    foreach (var item in relatedWords)
                    {
                        var listItem = new ListViewItem(item.File);
                        listItem.SubItems.Add(string.Join(", ", item.Words)); // Comma-delimited related words
                        relatedFilesView!.Items.Add(listItem);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Clustering error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show($"Clustering error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> GetImportantWords(string content, int count, string excludeWord)
        {
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !IsCommonWord(w.ToLower()) && !w.Equals(excludeWord, StringComparison.OrdinalIgnoreCase))
                .GroupBy(w => w.ToLower())
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .OrderByDescending(w => w.Count) // Sort by most frequent
                .Take(count)
                .Select(w => w.Word)
                .ToList();

            return words.Any() ? words : new List<string> { "No related words found" };
        }

        private bool IsCommonWord(string word)
        {
            var stopwords = new HashSet<string> { "the", "is", "and", "in", "it", "of", "to", "for", "with", "on", "at", "by", "from", "up", "about", "into", "over", "after" };
            return stopwords.Contains(word);
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var config = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(configPath));
                    if (config != null)
                    {
                        folders = config.ContainsKey("Folders") ? config["Folders"] : new List<string>();
                        extensions = config.ContainsKey("Extensions") ? config["Extensions"] : new List<string>();
                    }
                }
                else
                {
                    // If config file doesn’t exist, use defaults
                    folders.Clear();
                    extensions.Clear();
                    if (!extensions.Contains(".txt")) extensions.Add(".txt");
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Configuration error: {ex.Message}. Using default settings.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                folders.Clear();
                extensions.Clear();
                if (!extensions.Contains(".txt")) extensions.Add(".txt"); // Default to .txt
            }
        }

        internal void SaveConfiguration()
        {
            try
            {
                var config = new { Folders = folders, Extensions = extensions };
                File.WriteAllText(configPath, JsonSerializer.Serialize(config));
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAboutBox(object? sender, EventArgs e)
        {
            MessageBox.Show("TextOrganizer by Amir Husain\nVersion 1.0", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class ConfigureForm : Form
    {
        private MainForm mainForm;
        private ListBox? folderList;
        private ListBox? extensionList;
        private Button? addFolderButton;
        private Button? removeFolderButton;
        private Button? addExtensionButton;
        private Button? removeExtensionButton;
        private Button? saveButton;

        public ConfigureForm(MainForm parent)
        {
            mainForm = parent;
            Text = "Configure TextFileOrganizer";
            Size = new Size(400, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.Control;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            folderList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            folderList!.Items.AddRange(mainForm.folders.ToArray());

            extensionList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            extensionList!.Items.AddRange(mainForm.extensions.ToArray());

            addFolderButton = new Button { Text = "Add Folder", Dock = DockStyle.Top, Height = 30, BackColor = SystemColors.ButtonFace, ForeColor = SystemColors.ControlText, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
            removeFolderButton = new Button { Text = "Remove Folder", Dock = DockStyle.Top, Height = 30, BackColor = SystemColors.ButtonFace, ForeColor = SystemColors.ControlText, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
            addExtensionButton = new Button { Text = "Add Extension", Dock = DockStyle.Top, Height = 30, BackColor = SystemColors.ButtonFace, ForeColor = SystemColors.ControlText, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
            removeExtensionButton = new Button { Text = "Remove Extension", Dock = DockStyle.Top, Height = 30, BackColor = SystemColors.ButtonFace, ForeColor = SystemColors.ControlText, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
            saveButton = new Button { Text = "Save", Dock = DockStyle.Bottom, Height = 30, BackColor = SystemColors.ButtonFace, ForeColor = SystemColors.ControlText, Font = new Font("Segoe UI", 10, FontStyle.Regular) };

            addFolderButton!.Click += (s, e) => mainForm.AddFolder_Click(s, e);
            removeFolderButton!.Click += (s, e) => { if (folderList.SelectedIndex >= 0) mainForm.folders.RemoveAt(folderList.SelectedIndex); folderList.Items.RemoveAt(folderList.SelectedIndex); mainForm.SaveConfiguration(); };
            addExtensionButton!.Click += (s, e) => mainForm.AddExtension_Click(s, e);
            removeExtensionButton!.Click += (s, e) => { if (extensionList.SelectedIndex >= 0) mainForm.extensions.RemoveAt(extensionList.SelectedIndex); extensionList.Items.RemoveAt(extensionList.SelectedIndex); mainForm.SaveConfiguration(); };
            saveButton!.Click += (s, e) => { mainForm.SaveConfiguration(); Close(); };

            layout.Controls.Add(folderList!, 0, 0);
            layout.Controls.Add(extensionList!, 1, 0);
            layout.Controls.Add(addFolderButton!, 0, 1);
            layout.Controls.Add(addExtensionButton!, 1, 1);
            layout.Controls.Add(removeFolderButton!, 0, 2);
            layout.Controls.Add(removeExtensionButton!, 1, 2);
            layout.Controls.Add(saveButton!, 0, 3);
            layout.SetColumnSpan(saveButton!, 2);

            Controls.Add(layout);
        }
    }

    public class ListViewItemComparer : System.Collections.IComparer // Use non-generic IComparer
    {
        public int Col { get; }
        public bool Ascending { get; }

        public ListViewItemComparer(int column, bool ascending)
        {
            Col = column;
            Ascending = ascending;
        }

        public int Compare(object? x, object? y)
        {
            ListViewItem? itemX = x as ListViewItem;
            ListViewItem? itemY = y as ListViewItem;

            if (itemX == null || itemY == null) return 0;

            int result;
            if (Col == 1) // Sort by file size
            {
                double sizeX = ParseSize(itemX.SubItems[Col].Text);
                double sizeY = ParseSize(itemY.SubItems[Col].Text);
                result = sizeX.CompareTo(sizeY);
            }
            else if (Col == 2) // Sort by date
            {
                DateTime dateX = DateTime.Parse(itemX.SubItems[Col].Text);
                DateTime dateY = DateTime.Parse(itemY.SubItems[Col].Text);
                result = dateX.CompareTo(dateY);
            }
            else // Sort by name
            {
                result = string.Compare(itemX.SubItems[Col].Text, itemY.SubItems[Col].Text);
            }

            return Ascending ? result : -result;
        }

        private double ParseSize(string size)
        {
            string[] parts = size.Split(' ');
            if (parts.Length < 2) return 0;
            double number = double.Parse(parts[0], System.Globalization.NumberStyles.Any);
            string suffix = parts[1];
            return number * (suffix == "KB" ? 1024 : suffix == "MB" ? 1024 * 1024 : suffix == "GB" ? 1024 * 1024 * 1024 : 1);
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string? source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
