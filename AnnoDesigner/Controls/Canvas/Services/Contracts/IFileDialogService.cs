namespace AnnoDesigner.Controls.Canvas.Services.Contracts
{
    public interface IFileDialogService
    {
        // Shows a SaveFile dialog and returns the selected filename or null if cancelled
        string ShowSaveFile(string defaultExt, string filter);

        // Shows an OpenFile dialog and returns the selected filename or null if cancelled
        string ShowOpenFile(string defaultExt, string filter);
    }
}
