using System.Collections.Generic;
using System.Linq;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Segment
    {
        public List<Point> Points { get; set; } = new();
        public SegmentType Type { get; set; }

        public Segment()
        {
        }

        public Segment(List<Point> points, SegmentType type)
        {
            Points = points;
            Type = type;
        }

        public string ToWkt()
        {
            var type = Type == SegmentType.Arc ? "CIRCULARSTRING ({0})" : "LINESTRING ({0})";
            var pointsString = string.Join(", ", Points.Select(point => point.ToString()));

            return string.Format(type, pointsString);
        }
    }
}
