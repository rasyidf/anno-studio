using System.Windows.Controls;

namespace AnnoDesigner.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // Expose colorPicker for MainWindow code that referenced it
        public ColorPicker.PortableColorPicker ColorPicker => colorPicker;
    }
}