using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class GmlDocument : ValidationDataElement, IDisposable
    {
        private ILookup<string, XElement> _featureElements;
        private ILookup<string, XElement> _gmlElements;
        private ILookup<string, XElement> _geometryElements;
        private readonly Dictionary<string, IndexedGeometry> _geometryIndex = new(25000);
        private readonly object geoLock = new();
        private bool _disposed = false;

        public GmlDocument(XDocument document, string fileName) : this(document, fileName, null)
        {
        }

        public GmlDocument(XDocument document, string fileName, object dataType) : base(document, fileName, dataType)
        {
            Initialize(document);
        }

        public List<XElement> GetFeatureElements(params string[] featureNames)
        {
            if (!featureNames.Any())
                return _featureElements.SelectMany(element => element).ToList();

            var featureElements = new List<XElement>();

            foreach (var featureName in featureNames)
                if (_featureElements.Contains(featureName))
                    featureElements.AddRange(_featureElements[featureName]);

            return featureElements;
        }

        public List<XElement> GetFeatureGeometryElements(params string[] geometryNames)
        {
            return GetGeometryElements(geometryNames, true);
        }

        public List<XElement> GetGeometryElements(params string[] geometryNames)
        {
            return GetGeometryElements(geometryNames, false);
        }

        public XElement GetElementByGmlId(string gmlId)
        {
            return _gmlElements[gmlId].SingleOrDefault();
        }

        public Geometry GetOrCreateGeometry(XElement geoElement, out string errorMessage)
        {
            lock (geoLock)
            {
                return GeometryHelper.GetOrCreateGeometry(_geometryIndex, geoElement, out errorMessage);
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

        private List<XElement> GetGeometryElements(IEnumerable<string> geometryNames, bool featureGeometriesOnly)
        {
            if (!geometryNames.Any())
            {
                return _geometryElements.SelectMany(element => element)
                    .Where(element => !featureGeometriesOnly || (element.Parent.Name.Namespace != element.Parent.GetNamespaceOfPrefix("gml")))
                    .ToList();
            }

            var geometryElements = new List<XElement>();

            foreach (var geometryName in geometryNames)
            {
                if (!_geometryElements.Contains(geometryName))
                    continue;

                var geometryElementsOfType = _geometryElements[geometryName]
                    .Where(element => !featureGeometriesOnly || (element.Parent.Name.Namespace != element.Parent.GetNamespaceOfPrefix("gml")));

                geometryElements.AddRange(geometryElementsOfType);
            }

            return geometryElements;
        }

        private void Initialize(XDocument document)
        {
            var localName = document.Root.Elements()
                .Any(element => element.Name.LocalName == "featureMember") ? "featureMember" : "featureMembers";

            _featureElements = document.Root.Elements()
                .Where(element => element.Name.LocalName == localName)
                .SelectMany(element => element.Elements())
                .ToLookup(element => element.Name.LocalName);

            _gmlElements = document.Descendants()
                .Where(element => element.Attributes(GmlHelper.GmlNs + "id").Any())
                .ToLookup(element => element.Attribute(GmlHelper.GmlNs + "id").Value);

            _geometryElements = _gmlElements
                .SelectMany(element => element)
                .Where(element => GmlHelper.GeometryElementNames.Contains(element.Name.LocalName))
                .ToLookup(element => element.Name.LocalName);
        }

        public static new GmlDocument Create(InputData data)
        {
            return new(XDocument.Load(data.Stream), data.FileName, data.DataType);
        }
    }
}
