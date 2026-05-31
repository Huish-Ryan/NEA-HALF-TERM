#nullable enable
using System.Windows;
using System.Windows.Media;

namespace NEA.Rendering
{
    public class VisualHost : FrameworkElement
    {
        private readonly VisualCollection _children;

        public VisualHost()
        {
            _children = new VisualCollection(this);
        }

        public VisualCollection Children => _children;

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index) => _children[index];
    }
}
