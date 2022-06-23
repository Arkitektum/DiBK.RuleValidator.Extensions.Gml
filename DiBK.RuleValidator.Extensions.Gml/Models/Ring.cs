using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Ring : IDisposable
    {
        private bool _disposed = false;
        public List<Segment> Segments { get; set; } = new();
        public List<Ring> WithinRings { get; set; } = new();
        public Geometry Polygon { get; set; }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && Polygon != null)
                    Polygon.Dispose();

                _disposed = true;
            }
        }
    }
}
