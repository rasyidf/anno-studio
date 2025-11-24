using System;
using System.Threading.Tasks;

namespace AnnoDesigner.Controls.Canvas.Services
{
    public interface ILayoutFileService
    {
        Task CheckUnsavedChangesBeforeCrashAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile);
        Task<bool> CheckUnsavedChangesAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile);
        Task<bool> SaveAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile);
        Task<string> SaveAsAsync();
        Task<string> OpenFileAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile);
    }
}
