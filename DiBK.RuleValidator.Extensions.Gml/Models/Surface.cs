using System;
using System.Collections.Generic;
using System.Linq;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Surface : IDisposable
    {
        private bool _disposed = false;
        public Ring Exterior { get; set; }
        public List<Ring> Interior { get; set; } = new();

        public string ToWkt()
        {
            var curvePolygon = "CURVEPOLYGON ({0})";

            var wkts = Interior
                .Select(interior => interior.ToWkt())
                .ToList();

            wkts.Insert(0, Exterior.ToWkt());

            return string.Format(curvePolygon, string.Join(", ", wkts));
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
                if (disposing)
                {
                    Exterior.Dispose();
                    Interior.ForEach(ring => ring.Dispose());
                }

                _disposed = true;
            }
        }
    }
}
