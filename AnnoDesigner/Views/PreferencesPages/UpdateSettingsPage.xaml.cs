using System.Windows.Controls;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.ViewModels;

namespace AnnoDesigner.PreferencesPages
{
    /// <summary>
    /// Interaction logic for UpdateSettings.xaml
    /// </summary>
    public partial class UpdateSettingsPage : Page, INavigatedTo
    {
        public UpdateSettingsPage()
        {
            InitializeComponent();
        }

        public void NavigatedTo(object viewModel)
        {
            if (viewModel is UpdateSettingsViewModel vm)
            {
                DataContext = vm;
            }
        }
    }
}
