using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Ring
    {
        public List<Segment> Segments { get; set; } = new();
        public Geometry Envelope { get; set; }
        public List<Ring> WithinRings { get; set; } = new();
        public bool IsExterior { get; set; }

        public Ring()
        {
        }

        public Ring(List<Segment> segments)
        {
            Segments = segments;
        }

        public string ToWkt()
        {
            var compoundCurve = "COMPOUNDCURVE ({0})";
            var segments = string.Join(", ", Segments.Select(segment => segment.ToWkt()));

            return string.Format(compoundCurve, segments);
        }
    }
}
