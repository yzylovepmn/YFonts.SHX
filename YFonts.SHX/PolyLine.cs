using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace YFonts.SHX
{
    public struct PolyLine
    {
        public PolyLine(IEnumerable<Point> points)
        {
            _points = points.ToList();
        }

        public IEnumerable<Point> Points { get { return _points; } }
        private List<Point> _points;

        public PolyLine Offset(Vector vector)
        {
            return new PolyLine(_points.Select(p => p + vector));
        }

        public PolyLine Transform(Matrix transform)
        {
            return new PolyLine(_points.Select(p => p * transform));
        }
    }
}