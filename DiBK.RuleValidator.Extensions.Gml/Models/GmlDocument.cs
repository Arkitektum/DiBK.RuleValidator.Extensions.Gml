using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DiBK.RuleValidator.Extensions.Gml.Constants.Namespace;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class GmlDocument : ValidationDataElement, IDisposable
    {
        private ILookup<string, XElement> _featureElements;
        private ILookup<string, XElement> _gmlElements;
        private ILookup<string, XElement> _geometryElements;
        private readonly ConcurrentDictionary<XElement, IndexedGeometry> _geometries = new();
        private ILookup<string, IndexedGeometry> _geometriesByType;
        private bool _disposed = false;

        public GmlDocument(XDocument document, string fileName) : this(document, fileName, null)
        {
        }

        public GmlDocument(XDocument document, string fileName, object dataType) : base(document, fileName, dataType)
        {
            Initialize();
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

        public List<XElement> GetGeometryElements(params string[] geometryNames)
        {
            return GetGeometryElements(geometryNames, false);
        }

        public List<XElement> GetFeatureGeometryElements(params string[] geometryNames)
        {
            return GetGeometryElements(geometryNames, true);
        }

        public ILookup<string, XElement> GetGmlElements()
        {
             return _gmlElements;
        }

        public XElement GetGmlElementById(string gmlId)
        {
            return _gmlElements[gmlId].SingleOrDefault();
        }

        public List<IndexedGeometry> GetGeometriesByType(params string[] geometryNames)
        {
            var geometries = new List<IndexedGeometry>();

            foreach (var name in geometryNames)
                geometries.AddRange(_geometriesByType[name]);

            return geometries;
        }

        public IndexedGeometry GetOrCreateGeometry(XElement geoElement)
        {
            if (_geometries.TryGetValue(geoElement, out var indexed))
                return indexed;

            var newIndexed = IndexedGeometry.Create(geoElement);

            _geometries.TryAdd(geoElement, newIndexed);

            return newIndexed;
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
                    foreach (var index in _geometries)
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

        private string GetFeatureMemberName()
        {
            if (Document.Root.Elements().Any(element => element.Name.LocalName == "featureMember"))
                return "featureMember";

            if (Document.Root.Elements().Any(element => element.Name.LocalName == "featureMembers"))
                return "featureMembers";

            if (Document.Root.Elements().Any(element => element.Name.LocalName == "member"))
                return "member";

            return "featureMember";
        }

        private void Initialize()
        {
            var featureMemberName = GetFeatureMemberName();

            _featureElements = Document.Root.Elements()
                .Where(element => element.Name.LocalName == featureMemberName)
                .Elements()
                .ToLookup(element => element.Name.LocalName);

            _gmlElements = Document.Descendants()
                .Where(element => element.Attribute(GmlNs + "id") != null)
                .ToLookup(element => element.Attribute(GmlNs + "id").Value);

            _geometryElements = _gmlElements
                .SelectMany(element => element)
                .Where(element => GmlHelper.GeometryElementNames.Contains(element.Name.LocalName))
                .ToLookup(element => element.Name.LocalName);

            var geoElements = GetGeometryElements();

            Parallel.ForEach(
                geoElements, 
                new ParallelOptions { MaxDegreeOfParallelism = GetMaxDegreeOfParallelism() }, 
                element => _geometries.TryAdd(element, IndexedGeometry.Create(element))
            );

            _geometriesByType = _geometries.ToLookup(kvp => kvp.Value.Type, kvp => kvp.Value);
        }

        public static new GmlDocument Create(InputData data)
        {
            return new GmlDocument(XDocument.Load(data.Stream, LoadOptions.SetLineInfo), data.FileName, data.DataType);
        }

        private static int GetMaxDegreeOfParallelism()
        {
            var max = Environment.ProcessorCount / 4;
            return max > 0 ? max : 1;
        }
    }
}
