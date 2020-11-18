using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using YFonts.SHX;

namespace FontsTest
{
    public class TestVisual : UIElement
    {
        public IEnumerable<TextShape> TextShapes
        {
            get { return _textShapes; }
            set
            {
                _textShapes = value;
                InvalidateVisual();
            }
        }
        private IEnumerable<TextShape> _textShapes;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_textShapes == null) return;
            var mat = new Matrix();
            mat.M22 = -1;
            mat.OffsetY = RenderSize.Height;
            var shapes = _textShapes.Select(ts => ts.Transform(mat));
            foreach (var shape in shapes)
            {
                var geo = new StreamGeometry();
                using (var stream = geo.Open())
                {
                    drawingContext.DrawGeometry(null, new Pen(Brushes.Black, 1), geo);
                    foreach (var polyLine in shape.PolyLines)
                    {
                        stream.BeginFigure(polyLine.Points.First(), true, false);
                        stream.PolyLineTo(polyLine.Points.Skip(1).ToList(), true, true);
                    }
                    stream.Close();
                }
            }
        }
    }
}