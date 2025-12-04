using System.Windows;
using System.Windows.Input;
using AnnoDesigner.Core.Controls;
using AnnoDesigner.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnnoDesigner.ViewModels
{
    public partial class HotkeyRecorderViewModel : ObservableObject
    {
        public HotkeyRecorderViewModel()
        {
        }

        [ObservableProperty]
        private ActionRecorder.ActionType _result;

        [ObservableProperty]
        private Key _key;

        [ObservableProperty]
        private ExtendedMouseAction _mouseAction;

        [ObservableProperty]
        private ModifierKeys _modifiers;

        // Result, Key, MouseAction and Modifiers are generated via [ObservableProperty]

        [RelayCommand]
        private void Cancel(Window w)
        {
            w.DialogResult = false;
            w.Close();
        }

        [RelayCommand]
        private void Save(Window w)
        {
            w.DialogResult = true;
            w.Close();
        }

        public ActionRecorder ActionRecorder { get; set; }

        /// <summary>
        /// Resets the ActionRecorder and any recorded actions on the view model.
        /// </summary>
        public void Reset()
        {
            ActionRecorder.Reset();
        }
    }
}
