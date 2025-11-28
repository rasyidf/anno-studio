using Avalonia.Controls;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Views;

public partial class PlaygroundWindow : Window
{
    public PlaygroundWindow()
    {
        InitializeComponent();
        
        // Initialize ViewModel with the canvas control
        DataContext = new PlaygroundViewModel(PlaygroundCanvas);
    }
}
