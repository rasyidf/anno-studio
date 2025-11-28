using Avalonia.Controls;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Views
{
    public partial class CanvasView : UserControl
    {
        public CanvasView()
        {
            InitializeComponent();
            
            // DataContext will be set by DockFactory
            // Initialize the canvas when the control is loaded
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is CanvasViewModel viewModel && EditorCanvasControl != null)
            {
                viewModel.InitializeCanvas(EditorCanvasControl);
            }
        }
    }
}
