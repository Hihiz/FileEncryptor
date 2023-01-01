using FileEncryptor.WPF.Infrastructure.Commands;
using FileEncryptor.WPF.Infrastructure.Commands.Base;
using FileEncryptor.WPF.Services.Interfaces;
using FileEncryptor.WPF.VIewModels.Base;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace FileEncryptor.WPF.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        private const string _EncryptedFileSuffix = ".encrypted";

        private readonly IUserDialog _UserDialog;
        private readonly IEncryptor _Encryptor;

        private CancellationTokenSource _ProcessCancellation;

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

        #region ProgressValue - Значение прогресса

        /// <summary>Значение прогресса</summary>
        private double _ProgressValue;

        /// <summary>Значение прогресса</summary>
        public double ProgressValue { get => _ProgressValue; set => Set(ref _ProgressValue, value); }

        #endregion

        #region Команды

        #region SelectedFileCommand

        private ICommand _selectedFileCommand;

        public ICommand SelectedFileCommand => _selectedFileCommand ??= new LambdaCommand(OnSelectedFileCommand);

        private void OnSelectedFileCommand()
        {
            if (!_UserDialog.OpenFile("Выбор файла для шифрования", out var filePath)) return;
            var selectedFile = new FileInfo(filePath);

            SelectedFile = selectedFile.Exists ? selectedFile : null;
        }
        #endregion

        #region EncryptCommand

        private ICommand _encryptCommand;

        public ICommand EncryptCommand => _encryptCommand ??= new LambdaCommand(OnEncryptCommandExecuted, CanEncryptCommandExecute);

        private bool CanEncryptCommandExecute(object p) => (p is FileInfo file && file.Exists || SelectedFile != null) && !string.IsNullOrWhiteSpace(Password);

        private async void OnEncryptCommandExecuted(object p)
        {
            var file = p as FileInfo ?? SelectedFile;
            if (file is null) return;

            var defaultFile = file.FullName + _EncryptedFileSuffix;
            if (!_UserDialog.SaveFile("Выбор файла для сохранения", out var destinationPath, defaultFile)) return;

            var timer = Stopwatch.StartNew();

            var progress = new Progress<double>(percent => ProgressValue = percent);

            _ProcessCancellation = new CancellationTokenSource();
            var cancel = _ProcessCancellation.Token;

            ((Command)EncryptCommand).Executable = false;
            ((Command)DecryptCommand).Executable = false;
            try
            {
                await _Encryptor.EncryptAsync(file.FullName, destinationPath, Password, Progress: progress, Cancel: cancel);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancel)
            {

            }
            finally
            {
                _ProcessCancellation.Dispose();
                _ProcessCancellation = null;
            }

            ((Command)EncryptCommand).Executable = true;
            ((Command)DecryptCommand).Executable = true;

            timer.Stop();

            //_UserDialog.Information("Шифрование", $"Шифрование файла успешно завершено за {timer.Elapsed.TotalSeconds:0.##}");
        }

        #endregion

        #region DecryptCommand

        private ICommand _decryptCommand;

        public ICommand DecryptCommand => _decryptCommand ??= new LambdaCommand(OnDecryptCommandExecuted, CanDecryptCommandExecute);

        private bool CanDecryptCommandExecute(object p) => (p is FileInfo file && file.Exists || SelectedFile != null) && !string.IsNullOrWhiteSpace(Password);

        private async void OnDecryptCommandExecuted(object p)
        {
            var file = p as FileInfo ?? SelectedFile;
            if (file is null) return;

            var defaultFile = file.FullName.EndsWith(_EncryptedFileSuffix) ? file.FullName.Substring(0, file.FullName.Length - _EncryptedFileSuffix.Length) : file.FullName;
            if (!_UserDialog.SaveFile("Выбор файла для сохранения", out var destinationPath, defaultFile)) return;

            var timer = Stopwatch.StartNew();

            var progress = new Progress<double>(percent => ProgressValue = percent);
            _ProcessCancellation = new CancellationTokenSource();
            var cancel = _ProcessCancellation.Token;

            ((Command)EncryptCommand).Executable = false;
            ((Command)DecryptCommand).Executable = false;
            var decryptionTask = _Encryptor.DecryptAsync(file.FullName, destinationPath, Password, Progress: progress, Cancel: _ProcessCancellation.Token);
            // дополнительный код, выполняемый параллельно процессу дешифрования

            var success = false;
            try
            {
                success = await decryptionTask;
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancel)
            {

            }
            finally
            {
                _ProcessCancellation.Dispose();
                _ProcessCancellation = null;
            }

            ((Command)EncryptCommand).Executable = true;
            ((Command)DecryptCommand).Executable = true;

            timer.Stop();

            if (success)
                _UserDialog.Information("Дешифрование", $"Дешифровка файла выполнено успешно за {timer.Elapsed.TotalSeconds:0.##}");
            else
                _UserDialog.Warning("Шифрование", "Ошибка при дешифровке файла: указан неверный пароль");
        }

        #endregion

        #region CancelCommand - Отмена операции

        /// <summary>Отмена операции</summary>
        private ICommand _CancelCommand;

        /// <summary>Отмена операции</summary>
        public ICommand CancelCommand => _CancelCommand ??= new LambdaCommand(OnCancelCommandExecuted, CanCancelCommandExecute);

        /// <summary>Проверка возможности выполнения - Отмена операции</summary>
        private bool CanCancelCommandExecute() => _ProcessCancellation != null && !_ProcessCancellation.IsCancellationRequested;

        /// <summary>Логика выполнения - Отмена операции</summary>
        private void OnCancelCommandExecuted() => _ProcessCancellation.Cancel();

        #endregion

        #endregion

        public MainWindowViewModel(IUserDialog userDialog, IEncryptor encryptor)
        {
            _UserDialog = userDialog;
            _Encryptor = encryptor;
        }
    }
}
