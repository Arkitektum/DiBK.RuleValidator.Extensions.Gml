using OSGeo.OGR;
using System;
using System.Xml.Linq;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class IndexedGeometry : IDisposable
    {
        private bool _disposed = false;

        private IndexedGeometry(XElement element, Geometry geometry, string type, string errorMessage)
        {
            Element = element;
            Geometry = geometry;
            Type = type;
            ErrorMessage = errorMessage;
        }

        public XElement Element { get; private set; }
        public Geometry Geometry { get; private set; }
        public string Type { get; private set; }
        public string ErrorMessage { get; private set; }

        public bool IsValid
        {
            get
            {
                try
                {
                    return Geometry != null && Geometry.IsValid();
                }
                catch
                {
                    return false;
                }
            }
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

        public static IndexedGeometry Create(XElement element)
        {
            Geometry geometry = null;
            string errorMessage = null;

            try
            {
                geometry = GeometryHelper.GeometryFromGML(element);
            }
            catch (GeometryFromGMLException exception)
            {
                errorMessage = exception.Message;
            }

            return new IndexedGeometry(element, geometry, element.Name.LocalName, errorMessage);
        }
    }
}
