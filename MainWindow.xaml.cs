﻿using HandyControl.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextHiveGrok.Models;
using TextHiveGrok.ViewModels;
using TextHiveGrok.Views;

namespace TextHiveGrok
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly MainWindowViewModel _vm;
        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            this.DataContext = _vm;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _vm.SearchText = searchBox.Text;
                _vm.PerformSearch();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.PerformSearch();
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs? e)
        {
            _vm.SearchText = searchBox.Text = "";
            _vm.PerformSearch();
        }

        private void PreviewBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void fileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                _vm.SelectedFile = e.AddedItems[0] as FileItem;
            }

        }

        private void relatedFilesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                var selectedRelatedFile = e.AddedItems[0] as RelatedFile;
                var files = _vm.Files.Where(x => x.FileName == selectedRelatedFile?.FileName).ToList();
                if (files.Count > 0)
                {
                    _vm.SelectedFile = files[0];

                }
            }
        }

        private void HandleCreateNewFile(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = Microsoft.VisualBasic.Interaction.InputBox("Enter file name :", "New File", "NewFile.txt");

                if (!string.IsNullOrEmpty(fileName))
                {
                    var newFile = MainWindowViewModel.CreateFile(fileName);
                    if (newFile != null)
                    {
                        _vm.LoadFiles();
                        _vm.SelectedFile = _vm.Files.FirstOrDefault(f => f.FullPath == newFile.FullPath);
                    }
                }
            }
            catch (Exception)
            {
                CustomMessageBox.Show("Unable to create file", "Error", this);
                throw;
            }
        }

        private void HandleCreateTodaysNote(object sender, RoutedEventArgs e)
        {
            var todayFile = MainWindowViewModel.CreateTodaysNoteFile();
            if (todayFile != null)
            {
                _vm.LoadFiles();
                _vm.SelectedFile = _vm.Files.FirstOrDefault(f => f.FullPath == todayFile.FullPath);
            }
        }

        private void HanldeOpenInNotepad(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedFile != null)
            {
                Process.Start("notepad.exe", _vm.SelectedFile.FullPath);
            }
        }

        private void HandleConfigure(object sender, RoutedEventArgs e)
        {
            var w = new ConfigureWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };
            var output = w.ShowDialog();
            if (output == true)
            {
                ClearButton_Click(null, null);
                _vm.LoadFiles();
            }
        }

        private void HandleExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("TextOrganizer by Amir Husain\nVersion 1.0", "About", this);
        }

        private void HandleSaveFile(object? sender, RoutedEventArgs? e)
        {
            if (_vm.PreviewText == previewBox.Text)
            {
                return;
            }

            _vm.PreviewText = previewBox.Text;
            var result = _vm.SaveCurrentFile();
            if (result)
            {
                CustomMessageBox.Show($"Saved {Path.GetFileName(_vm.SelectedFile?.FullPath)} successfully.", "File Saved", this);
            }
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                HandleSaveFile(null, null);
            }
        }
    }
}