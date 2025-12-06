using System.Windows;
using System.Windows.Controls;

namespace AnnoDesigner.Controls
{
    public partial class PropertyGridItem : UserControl
    {
        public PropertyGridItem()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(string), typeof(PropertyGridItem), new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(PropertyGridItem), new PropertyMetadata(string.Empty));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty ValueTemplateProperty = DependencyProperty.Register(
            nameof(ValueTemplate), typeof(DataTemplate), typeof(PropertyGridItem), new PropertyMetadata(null));

        public DataTemplate ValueTemplate
        {
            get => (DataTemplate)GetValue(ValueTemplateProperty);
            set => SetValue(ValueTemplateProperty, value);
        }
    }
}
