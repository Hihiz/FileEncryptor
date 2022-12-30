using FileEncryptor.WPF.Services.Interfaces;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

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

       
    }
}
