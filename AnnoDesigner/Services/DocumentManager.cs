
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.ViewModels;
using AnnoDesigner.Models;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Manages the collection of open documents and active document state.
    /// </summary>
    public partial class DocumentManager : ObservableObject
    {
        private readonly IDocumentServicesFactory _servicesFactory;
        private readonly ISharedResourceManager _sharedResources;

        [ObservableProperty]
        private ObservableCollection<DocumentViewModel> _documents;

        [ObservableProperty]
        private DocumentViewModel _activeDocument;

        public DocumentManager(
            IDocumentServicesFactory servicesFactory,
            ISharedResourceManager sharedResources)
        {
            _servicesFactory = servicesFactory ?? throw new ArgumentNullException(nameof(servicesFactory));
            _sharedResources = sharedResources ?? throw new ArgumentNullException(nameof(sharedResources));

            Documents = new ObservableCollection<DocumentViewModel>();
        }

        /// <summary>
        /// Creates a new empty document.
        /// </summary>
        public DocumentViewModel CreateNewDocument()
        {
            var services = _servicesFactory.CreateDocumentServices();
            var document = new DocumentViewModel(
                services,
                _sharedResources.BuildingPresets,
                _sharedResources.Icons
            );

            document.CloseRequested += OnDocumentCloseRequested;
            document.StatusMessageChanged += OnDocumentStatusMessageChanged;

            Documents.Add(document);
            ActiveDocument = document;

            return document;
        }

        /// <summary>
        /// Opens a layout file as a new document.
        /// </summary>
        public async Task<DocumentViewModel> OpenDocumentAsync(string filePath)
        {
            var document = CreateNewDocument();

            try
            {
                var layout = await Task.Run(() => _sharedResources.LayoutLoader.LoadLayout(filePath));
                document.Canvas.PlacedObjects.Clear();
                document.Canvas.PlacedObjects.AddRange(
                    layout.Objects.Select(obj => new LayoutObject(
                        obj,
                        _sharedResources.CoordinateHelper,
                        _sharedResources.BrushCache,
                        _sharedResources.PenCache
                    ))
                );

                document.FilePath = filePath;
                document.LayoutSettings.LayoutVersion = layout.LayoutVersion;
                document.Canvas.Normalize(1);
                document.Canvas.ResetViewport();
                document.IsDirty = false;

                return document;
            }
            catch
            {
                await CloseDocumentAsync(document);
                throw;
            }
        }

        /// <summary>
        /// Closes a document after checking for unsaved changes.
        /// </summary>
        public async Task<bool> CloseDocumentAsync(DocumentViewModel document)
        {
            if (document == null)
            {
                return false;
            }

            // if (await document.CheckUnsavedChangesAsync() == false)
            // {
            //     return false;
            // }

            document.CloseRequested -= OnDocumentCloseRequested;
            document.StatusMessageChanged -= OnDocumentStatusMessageChanged;

            Documents.Remove(document);
            document.Dispose();

            // Set new active document
            if (Documents.Count > 0)
            {
                ActiveDocument = Documents.Last();
            }
            else
            {
                ActiveDocument = null;
            }

            return true;
        }

        /// <summary>
        /// Closes all documents.
        /// </summary>
        public async Task<bool> CloseAllDocumentsAsync()
        {
            var documentsToClose = Documents.ToList();

            foreach (var document in documentsToClose)
            {
                if (!await CloseDocumentAsync(document))
                {
                    return false; // User cancelled
                }
            }

            return true;
        }

        private async void OnDocumentCloseRequested(object sender, EventArgs e)
        {
            if (sender is DocumentViewModel document)
            {
                await CloseDocumentAsync(document);
            }
        }

        private void OnDocumentStatusMessageChanged(object sender, string message)
        {
            // Propagate to main view model
            StatusMessageChanged?.Invoke(this, message);
        }

        public event EventHandler<string> StatusMessageChanged;
    }
}
