using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoDesigner.Core.Models;

namespace AnnoDesigner.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public AboutViewModel()
        {
        }

        #region commands

        [RelayCommand]
        private void OpenOriginalHomepage(object? param)
        {
            _ = System.Diagnostics.Process.Start("http://code.google.com/p/anno-designer/");
        }

        [RelayCommand]
        private void OpenProjectHomepage(object? param)
        {
            _ = System.Diagnostics.Process.Start("https://github.com/AnnoDesigner/anno-designer/");
        }

        [RelayCommand]
        private void OpenWikiHomepage(object? param)
        {
            _ = System.Diagnostics.Process.Start("https://anno1800.fandom.com/wiki/Anno_Designer");
        }

        [RelayCommand]
        private void CloseWindow(ICloseable? window)
        {
            window?.Close();
        }

        #endregion
    }
}


