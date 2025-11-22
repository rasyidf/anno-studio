using System.Collections.ObjectModel;
using System.Windows.Input;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;

namespace AnnoDesigner.ViewModels
{
    public class WelcomeViewModel : Notify
    {
        private readonly ICommons _commons;
        private readonly IAppSettings _appSettings;
        private ObservableCollection<SupportedLanguage> _languages;
        private SupportedLanguage _selectedItem;

        public WelcomeViewModel(ICommons commonsToUse, IAppSettings appSettingsToUse)
        {
            _commons = commonsToUse;
            _appSettings = appSettingsToUse;

            Languages =
            [
                new SupportedLanguage("English")
                {
                    FlagPath = "/Assets/Flags/United Kingdom.png"
                },
                new SupportedLanguage("Deutsch")
                {
                    FlagPath = "/Assets/Flags/Germany.png"
                },
                new SupportedLanguage("Français")
                {
                    FlagPath = "/Assets/Flags/France.png"
                },
                new SupportedLanguage("Polski")
                {
                    FlagPath = "/Assets/Flags/Poland.png"
                },
                new SupportedLanguage("Русский")
                {
                    FlagPath = "/Assets/Flags/Russia.png"
                },
                new SupportedLanguage("Español")
                {
                    FlagPath = "/Assets/Flags/Spain.png"
                },
            ];

            ContinueCommand = new RelayCommand(Continue, CanContinue);
        }

        public ObservableCollection<SupportedLanguage> Languages
        {
            get { return _languages; }
            set { _ = UpdateProperty(ref _languages, value); }
        }

        public SupportedLanguage SelectedItem
        {
            get { return _selectedItem; }
            set { _ = UpdateProperty(ref _selectedItem, value); }
        }

        public ICommand ContinueCommand { get; private set; }

        private void Continue(object param)
        {
            LoadSelectedLanguage(param as ICloseable);
        }

        private bool CanContinue(object param)
        {
            return SelectedItem != null;
        }

        private void LoadSelectedLanguage(ICloseable window)
        {
            _commons.CurrentLanguage = SelectedItem.Name;

            _appSettings.SelectedLanguage = _commons.CurrentLanguage;
            _appSettings.Save();

            window?.Close();
        }
    }
}
