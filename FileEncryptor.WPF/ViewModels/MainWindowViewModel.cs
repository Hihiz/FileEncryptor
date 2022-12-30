using FileEncryptor.WPF.Infrastructure.Commands;
using FileEncryptor.WPF.Services.Interfaces;
using FileEncryptor.WPF.VIewModels.Base;
using System.IO;
using System.Windows.Input;

namespace FileEncryptor.WPF.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly IUserDialog _userDialog;

        #region Свойство Title - Заголовок окна

        private string _title = "Шифратор";

        public string Title { get => _title; set => Set(ref _title, value); }

        #endregion

        #region Свойство Password - Пароль

        private string _password = "123";

        public string Password { get => _password; set => Set(ref _password, value); }

        #endregion

        #region SelectedFile - Выбранный файл

        private FileInfo _selectedFile;

        public FileInfo SelectedFile { get => _selectedFile; set => Set(ref _selectedFile, value); }

        #endregion

        #region Команды

        #region SelectedFileCommand

        private ICommand _selectedFileCommand;

        public ICommand SelectedFileCommand => _selectedFileCommand ??= new LambdaCommand(OnSelectedFileCommand);

        private void OnSelectedFileCommand()
        {
            if (!_userDialog.OpenFile("Выбор файла для шифрования", out var filePath)) return;
            var selectedFile = new FileInfo(filePath);

            SelectedFile = selectedFile.Exists ? selectedFile : null;
        }
        #endregion

        #region EncryptCommand

        private ICommand _encryptCommand;

        public ICommand EncryptCommand => _encryptCommand ??= new LambdaCommand(OnEncryptCommandExecuted, CanEncryptCommandExecute);

        private bool CanEncryptCommandExecute(object p) => (p is FileInfo file && file.Exists || SelectedFile != null) && !string.IsNullOrWhiteSpace(Password);

        private void OnEncryptCommandExecuted(object p)
        {
            var file = p as FileInfo ?? SelectedFile;
            if (file is null) return;
        }

        #endregion

        #region DecryptCommand

        private ICommand _decryptCommand;

        public ICommand DecryptCommand => _decryptCommand ??= new LambdaCommand(OnDecryptCommandExecuted, CanDecryptCommandExecute);

        private bool CanDecryptCommandExecute(object p) => (p is FileInfo file && file.Exists || SelectedFile != null) && !string.IsNullOrWhiteSpace(Password);

        private void OnDecryptCommandExecuted(object p)
        {
            var file = p as FileInfo ?? SelectedFile;
            if (file is null) return;
        }

        #endregion

        #endregion

        public MainWindowViewModel(IUserDialog UserDialog)
        {
            _userDialog = UserDialog;
        }
    }
}
