using OSGeo.OGR;
using System;
using System.Xml.Linq;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class IndexedGeometry : IDisposable
    {
        private bool _disposed = false;
        public string XPath { get; set; }
        public XElement GeoElement { get; set; }
        public Geometry Geometry { get; set; }
        public string Type { get; set; }
        public bool IsValid => Geometry != null && Geometry.IsValid();
        public string ErrorMessage { get; set; }

        private IndexedGeometry(string xPath, XElement geoElement, Geometry geometry, string type, string errorMessage)
        {
            XPath = xPath;
            GeoElement = geoElement;
            Geometry = geometry;
            Type = type;
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

        public static IndexedGeometry Create(XElement geoElement)
        {
            var xPath = geoElement.GetXPath();
            Geometry geometry = null;
            string errorMessage = null;

            try
            {
                geometry = GeometryHelper.GeometryFromGML(geoElement);
            }
            catch (GeometryFromGMLException exception)
            {
                errorMessage = exception.Message;
            }

            return new IndexedGeometry(xPath, geoElement, geometry, geoElement.Name.LocalName, errorMessage);
        }
    }
}
