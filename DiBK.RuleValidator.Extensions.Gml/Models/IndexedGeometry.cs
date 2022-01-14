using OSGeo.OGR;
using System;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class IndexedGeometry : IDisposable
    {
        private bool _disposed = false;
        public string GmlId { get; set; }
        public Geometry Geometry { get; set; }
        public NtsGeometry NtsGeometry { get; set; }
        public string ErrorMessage { get; set; }

        public IndexedGeometry(string gmlId, Geometry geometry, string errorMessage)
        {
            GmlId = gmlId;
            Geometry = geometry;
            ErrorMessage = errorMessage;
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
                if (disposing && Geometry != null)
                    Geometry.Dispose();

                _disposed = true;
            }
        }
    }
}
