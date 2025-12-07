using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnnoDesigner.Controls.EditorCanvas.Core;

namespace AnnoDesigner.Controls.EditorCanvas.Views
{
    public partial class LayerManagerControl : UserControl
    {
        public LayerManagerControl()
        {
            InitializeComponent();
            _items = new ObservableCollection<LayerViewModel>();
            LayersList.ItemsSource = _items;
        }

        private ObservableCollection<LayerViewModel> _items;
        private ILayeredRenderer? _renderer;

        public ILayeredRenderer? Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                Refresh();
            }
        }

        private void Refresh()
        {
            _items.Clear();
            if (_renderer == null) return;
            foreach (var l in _renderer.Layers.OrderByDescending(x=>x.Order))
            {
                _items.Add(new LayerViewModel(l));
            }
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = LayersList.SelectedItem as LayerViewModel;
            if (vm == null || _renderer == null) return;

            var layers = _renderer.Layers.OrderBy(x => x.Order).ToArray();
            var idx = System.Array.IndexOf(layers, vm.Layer);
            if (idx <= 0) return;

            // swap orders
            var above = layers[idx - 1];
            var tmp = above.Order;
            above.GetType().GetProperty("Order")?.SetValue(above, vm.Layer.Order);
            vm.Layer.GetType().GetProperty("Order")?.SetValue(vm.Layer, tmp);

            Refresh();
        }

        private void DownBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = LayersList.SelectedItem as LayerViewModel;
            if (vm == null || _renderer == null) return;

            var layers = _renderer.Layers.OrderBy(x => x.Order).ToArray();
            var idx = System.Array.IndexOf(layers, vm.Layer);
            if (idx < 0 || idx >= layers.Length - 1) return;

            var below = layers[idx + 1];
            var tmp = below.Order;
            below.GetType().GetProperty("Order")?.SetValue(below, vm.Layer.Order);
            vm.Layer.GetType().GetProperty("Order")?.SetValue(vm.Layer, tmp);

            Refresh();
        }

        private void TopBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = LayersList.SelectedItem as LayerViewModel;
            if (vm == null || _renderer == null) return;
            var layers = _renderer.Layers.OrderBy(x => x.Order).ToArray();
            var max = layers.Max(x => x.Order);
            // place this layer above all others
            vm.Layer.GetType().GetProperty("Order")?.SetValue(vm.Layer, max + 1);
            Refresh();
        }

        private void BottomBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = LayersList.SelectedItem as LayerViewModel;
            if (vm == null || _renderer == null) return;
            var layers = _renderer.Layers.OrderBy(x => x.Order).ToArray();
            var min = layers.Min(x => x.Order);
            vm.Layer.GetType().GetProperty("Order")?.SetValue(vm.Layer, min - 1);
            Refresh();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            // apply enabled flags back to the real layer objects
            if (_renderer == null) return;
            foreach (var vm in _items)
            {
                vm.Layer.Enabled = vm.Enabled;
            }
            Refresh();
        }
    }

    public class LayerViewModel
    {
        public LayerViewModel(IRenderLayer layer)
        {
            Layer = layer;
            Enabled = layer.Enabled;
        }

        public IRenderLayer Layer { get; }
        public string Display => $"{Layer.Order,-4} {Layer.Name}";
        public bool Enabled { get; set; }
    }
}
