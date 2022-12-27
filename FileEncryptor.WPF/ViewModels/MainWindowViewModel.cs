using FileEncryptor.WPF.VIewModels.Base;

namespace FileEncryptor.WPF.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Свойство Title - Заголовок окна

        private string _title = "Шифратор";

        public string Title { get => _title; set => Set(ref _title, value); }
    }

    #endregion
}
