using System.Windows;
using System.Windows.Controls;

namespace AnnoDesigner.Controls
{
    public partial class PropertyGridHeader : UserControl
    {
        public PropertyGridHeader()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(string), typeof(PropertyGridHeader), new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}
