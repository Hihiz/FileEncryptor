using FileEncryptor.WPF.Services.Interfaces;
using FileEncryptor.WPF.Views.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace FileEncryptor.WPF.Services
{
    internal class UserDialogService : IUserDialog
    {
        public bool OpenFile(string Title, out string SelectedFile, string Filter = "Все файлы (*.*)|*.*")
        {
            var fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter
            };

            if (fileDialog.ShowDialog() != true)
            {
                SelectedFile = null;
                return false;
            }

            SelectedFile = fileDialog.FileName;

            return true;
        }

        public bool OpenFiles(string Title, out IEnumerable<string> SelectedFiles, string Filter = "Все файлы (*.*)|*.*")
        {
            var fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter
            };

            if (fileDialog.ShowDialog() != true)
            {
                SelectedFiles = Enumerable.Empty<string>();
                return false;
            }

            SelectedFiles = fileDialog.FileNames;

            return true;
        }

        public bool SaveFile(string Title, out string SelectedFile, string DefaultFileName = null, string Filter = "Все файлы (*.*)|*.*")
        {
            var fileDialog = new SaveFileDialog
            {
                Title = Title,
                Filter = Filter
            };
            if (!string.IsNullOrWhiteSpace(DefaultFileName))
                fileDialog.FileName = DefaultFileName;

            if (fileDialog.ShowDialog() != true)
            {
                SelectedFile = null;
                return false;
            }

            SelectedFile = fileDialog.FileName;

            return true;
        }

        public void Information(string Title, string Message) => MessageBox.Show(Message, Title, MessageBoxButton.OK, MessageBoxImage.Information);
        public void Warning(string Title, string Message) => MessageBox.Show(Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        public void Error(string Title, string Message) => MessageBox.Show(Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);

        public (IProgress<double> Progress, IProgress<string> Status, CancellationToken Cancel, Action Close) ShowProgress(string Title)
        {
            var progressWindow = new ProgressWindow { Title = Title, Owner = App.FocusedWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            progressWindow.Show();

            return (progressWindow.ProgressInformer, progressWindow.StatusInformer, progressWindow.Cancel, progressWindow.Close);
        }
    }
}
