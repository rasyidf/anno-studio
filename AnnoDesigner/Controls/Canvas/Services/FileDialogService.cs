using Microsoft.Win32;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class FileDialogService : Contracts.IFileDialogService
    {
        public string ShowSaveFile(string defaultExt, string filter)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = defaultExt,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string ShowOpenFile(string defaultExt, string filter)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = defaultExt,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
