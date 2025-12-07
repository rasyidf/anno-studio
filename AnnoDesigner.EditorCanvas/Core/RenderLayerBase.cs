namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    public abstract class RenderLayerBase : IRenderLayer
    {
        protected RenderLayerBase(string name, int order)
        {
            Name = name;
            Order = order;
            Enabled = true;
        }

        public string Name { get; }

        public int Order { get; set; }

        public bool Enabled { get; set; }

        public abstract void Render(System.Windows.Media.DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, System.Windows.Rect clip);
    }
}
