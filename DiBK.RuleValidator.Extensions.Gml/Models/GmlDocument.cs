using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class GmlDocument : ValidationDataElement, IDisposable
    {
        private List<XElement> _features;
        private readonly Dictionary<string, IndexedGeometry> _geometryIndex = new(25000);
        private readonly object geoLock = new();
        private bool _disposed = false;

        public GmlDocument(XDocument document, string fileName) : this(document, fileName, null)
        {
        }

        public GmlDocument(XDocument document, string fileName, object dataType) : base(document, fileName, dataType)
        {
            LoadFeatures(document);
        }

        public List<XElement> GetFeatures(params string[] featureNames)
        {
            if (!featureNames.Any())
                return _features;

            return _features
                .Where(element => featureNames.Any(name => name == element.Name.LocalName))
                .ToList();
        }

        public Geometry GetOrCreateGeometry(XElement geoElement, out string errorMessage)
        {
            lock (geoLock)
            {
                return GeometryHelper.GetOrCreateGeometry(_geometryIndex, geoElement, out errorMessage);
            }
        }

        private void LoadFeatures(XDocument document)
        {
            var localName = document.Root.Elements()
                .Any(element => element.Name.LocalName == "featureMember") ? "featureMember" : "featureMembers";

            _features = document.Root.Elements()
                .Where(element => element.Name.LocalName == localName)
                .SelectMany(element => element.Elements())
                .ToList();
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
                    foreach (var index in _geometryIndex)
                    {
                        if (index.Value.Geometry != null)
                            index.Value.Geometry.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        public static new GmlDocument Create(InputData data)
        {
            return new(XDocument.Load(data.Stream), data.FileName, data.DataType);
        }
    }
}
