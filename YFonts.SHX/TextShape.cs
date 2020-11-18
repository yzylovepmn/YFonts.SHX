using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace YFonts.SHX
{
    public struct TextShape
    {
        internal TextShape(Point lastPoint, IEnumerable<PolyLine> polyLines)
        {
            _lastPoint = lastPoint;
            _polyLines = polyLines.ToList();
            _width = CalcWidth(_polyLines);
        }

        private TextShape(Point lastPoint, IEnumerable<PolyLine> polyLines, double width)
        {
            _lastPoint = lastPoint;
            _polyLines = polyLines.ToList();
            _width = width;
        }

        public Point LastPoint { get { return _lastPoint; } }
        private Point _lastPoint;

        public IEnumerable<PolyLine> PolyLines { get { return _polyLines; } }
        private List<PolyLine> _polyLines;

        internal double Width { get { return _width; } }
        private double _width;

        internal static double CalcWidth(IEnumerable<PolyLine> _polyLines)
        {
            var min = 0.0;
            var max = 0.0;
            foreach (var polyLine in _polyLines)
            {
                foreach (var point in polyLine.Points)
                {
                    min = Math.Min(min, point.X);
                    max = Math.Max(max, point.X);
                }
            }
            return max - min;
        }

        public TextShape Offset(Vector vector)
        {
            return new TextShape(_lastPoint + vector, _polyLines.Select(p => p.Offset(vector)), _width);
        }

        public TextShape Transform(Matrix transform)
        {
            return new TextShape(_lastPoint * transform, _polyLines.Select(p => p.Transform(transform)));
        }
    }
}