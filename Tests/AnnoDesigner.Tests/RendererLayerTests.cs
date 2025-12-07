using System.Linq;
using Xunit;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Controls.EditorCanvas.Core.Layers;
using System.Windows.Controls;

namespace AnnoDesigner.Tests
{
    public class RendererLayerTests
    {
        [StaFact]
        public void RendererWpf_ImplementsLayering_AddRemoveLayers()
        {
            var owner = new UserControl();
            var renderer = new RendererWpf(owner);

            Assert.IsAssignableFrom<ILayeredRenderer>(renderer);

            var layered = (ILayeredRenderer)renderer;
            var grid = new GridLayer(order: 10) { CellSize = 64 };

            layered.AddLayer(grid);

            var found = layered.Layers.FirstOrDefault(l => l.Name == grid.Name);
            Assert.NotNull(found);

            var removed = layered.RemoveLayer(grid);
            Assert.True(removed);
            Assert.DoesNotContain(layered.Layers, l => l.Name == grid.Name);
        }

        [StaFact]
        public void EditorCanvas_DefaultLayers_ContainsNewGridTypes()
        {
            var canvas = new AnnoDesigner.Controls.EditorCanvas.EditorCanvas();
            var layered = canvas.LayeredRenderer;
            Assert.NotNull(layered);

            var names = layered.Layers.Select(l => l.Name).ToArray();
            Assert.Contains("SubGrid", names);
            Assert.Contains("DotGrid", names);
            Assert.Contains("CrossGrid", names);
            Assert.Contains("Grid", names);
        }

        [StaFact]
        public void LayerManager_Refresh_ShowsAllLayers()
        {
            var canvas = new AnnoDesigner.Controls.EditorCanvas.EditorCanvas();
            var renderer = canvas.LayeredRenderer;
            Assert.NotNull(renderer);

            var manager = new AnnoDesigner.Controls.EditorCanvas.Views.LayerManagerControl();
            manager.Renderer = renderer;

            // reflect into the private ListBox to inspect number of items
            var listBoxField = typeof(AnnoDesigner.Controls.EditorCanvas.Views.LayerManagerControl).GetField("LayersList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var listBox = (System.Windows.Controls.ListBox)listBoxField.GetValue(manager);

            Assert.Equal(renderer.Layers.Count(), listBox.Items.Count);
        }
    }
}
