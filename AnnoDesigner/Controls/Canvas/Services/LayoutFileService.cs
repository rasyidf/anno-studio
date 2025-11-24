using System;
using System.Threading.Tasks;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Models.Interface; 
using AnnoDesigner.Services.Undo;

namespace AnnoDesigner.Controls.Canvas.Services
{
    public class LayoutFileService : ILayoutFileService
    {
        private readonly IUndoManager _undoManager;
        private readonly IMessageBoxService _messageBoxService;
        private readonly ILocalizationHelper _localizationHelper;
        private readonly Contracts.IFileDialogService _fileDialogService;

        public LayoutFileService(IUndoManager undoManager, IMessageBoxService messageBoxService, ILocalizationHelper localizationHelper, Contracts.IFileDialogService fileDialogService = null)
        {
            _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));
            _messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            _localizationHelper = localizationHelper ?? throw new ArgumentNullException(nameof(localizationHelper));
            _fileDialogService = fileDialogService ?? new FileDialogService();
        }

        public async Task CheckUnsavedChangesBeforeCrashAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile)
        {
            if (_undoManager.IsDirty)
            {
                var save = await _messageBoxService.ShowQuestion(null,
                    _localizationHelper.GetLocalization("SaveUnsavedChanges"),
                    _localizationHelper.GetLocalization("UnsavedChangedBeforeCrash")
                );

                if (save)
                {
                    var file = await SaveAsAsync();
                    if (!string.IsNullOrEmpty(file))
                    {
                        onSavedFile?.Invoke(file);
                    }
                }
            }
        }

        public async Task<bool> CheckUnsavedChangesAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile)
        {
            if (_undoManager.IsDirty)
            {
                var save = await _messageBoxService.ShowQuestionWithCancel(null,
                    _localizationHelper.GetLocalization("SaveUnsavedChanges"),
                    _localizationHelper.GetLocalization("UnsavedChanged")
                );

                if (save == null)
                {
                    return false;
                }

                if (save.Value)
                {
                    return await SaveAsync(getCurrentLoadedFile, onSavedFile);
                }
            }

            return true;
        }

        public async Task<bool> SaveAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile)
        {
            var current = getCurrentLoadedFile?.Invoke();
            if (string.IsNullOrEmpty(current))
            {
                var file = await SaveAsAsync();
                if (!string.IsNullOrEmpty(file))
                {
                    onSavedFile?.Invoke(file);
                    return true;
                }
                return false;
            }

            onSavedFile?.Invoke(current);
            return true;
        }

        public Task<string> SaveAsAsync()
        {
            // delegate dialogs via file dialog abstraction to make tests and alternative flows easier
            return Task.FromResult(_fileDialogService.ShowSaveFile(Constants.SavedLayoutExtension, Constants.SaveOpenDialogFilter));
        }

        public async Task<string> OpenFileAsync(Func<string> getCurrentLoadedFile, Action<string> onSavedFile)
        {
            if (!await CheckUnsavedChangesAsync(getCurrentLoadedFile, onSavedFile))
            {
                return null;
            }

            return _fileDialogService.ShowOpenFile(Constants.SavedLayoutExtension, Constants.SaveOpenDialogFilter);
        }
    }
}
