using Avalonia.Controls;
using Avalonia.LogicalTree;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Views
{
    public partial class ToolbarView : UserControl
    {
        public ToolbarView()
        {
            InitializeComponent();
            
            // DataContext will be set by DockFactory
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Initialize toolbar after both toolbar and canvas are loaded
            if (DataContext is not ToolbarViewModel toolbarViewModel)
            {
                return;
            }
            // Find the main window and get the canvas view model
            var mainWindow = this.FindLogicalAncestorOfType<MainWindow>();
            if (mainWindow?.DataContext is not MainWindowViewModel mainViewModel)
            {
                return;
            }
            // Get the DockFactory and find the canvas view model
            if (mainViewModel.Layout?.Factory is not DockFactory dockFactory)
            {
                return;
            }
            
            // Initialize with canvas view model
            // Use Background priority to ensure canvas has fully initialized
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var canvasViewModel = dockFactory.GetCanvasViewModel();
                if (canvasViewModel != null)
                {
                    toolbarViewModel.Initialize(canvasViewModel);
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
