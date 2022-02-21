using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class GmlHelper
    {
        private static readonly Regex _srsNameRegex =
            new(@"^(http:\/\/www\.opengis\.net\/def\/crs\/EPSG\/0\/|^urn:ogc:def:crs:EPSG::)(?<epsg>\d+)$", RegexOptions.Compiled);

        public static readonly XNamespace GmlNs = "http://www.opengis.net/gml/3.2";

        public static readonly string[] GeometryElementNames = new[]
        {
            GmlGeometry.CompositeCurve,
            GmlGeometry.CompositeSolid,
            GmlGeometry.CompositeSurface,
            GmlGeometry.Curve,
            GmlGeometry.GeometricComplex,
            GmlGeometry.Grid,
            GmlGeometry.LineString,
            GmlGeometry.MultiCurve,
            GmlGeometry.MultiGeometry,
            GmlGeometry.MultiPoint,
            GmlGeometry.MultiSolid,
            GmlGeometry.MultiSurface,
            GmlGeometry.OrientableCurve,
            GmlGeometry.OrientableSurface,
            GmlGeometry.Point,
            GmlGeometry.Polygon,
            GmlGeometry.PolyhedralSurface,
            GmlGeometry.RectifiedGrid,
            GmlGeometry.Solid,
            GmlGeometry.Surface,
            GmlGeometry.Tin,
            GmlGeometry.TriangulatedSurface
        };

        public static string GetFeatureType(XElement element)
        {
            return GetFeatureElement(element)?.GetName();
        }

        public static XElement GetBaseGmlElement(XElement element)
        {
            return element.AncestorsAndSelf()
                .FirstOrDefault(element => element.Parent.Name.Namespace != GmlNs);
        }

        public static XElement GetFeatureElement(XElement element)
        {
            return element.AncestorsAndSelf()
                .FirstOrDefault(element => element.Parent.Name.LocalName == "featureMember" || element.Parent.Name.LocalName == "featureMembers");
        }

        public static XElement GetFeatureGeometryElement(XElement element)
        {
            return GetFeatureElement(element)?.GetElement("*/gml:*");
        }

        public static IEnumerable<XElement> GetFeatureGeometryElements(XElement featureElement)
        {
            return featureElement.Descendants()
                .Where(element => GeometryElementNames.Contains(element.Name.LocalName) &&
                    element.Parent.Name.Namespace != element.Parent.GetNamespaceOfPrefix("gml"));
        }

        public static string GetFeatureGmlId(XElement element)
        {
            return GetFeatureElement(element)?.Attribute(GmlNs + "id")?.Value;
        }

        public static XElement GetClosestGmlIdElement(XElement element)
        {
            return element.AncestorsAndSelf()
                .FirstOrDefault(element => element.Attribute(GmlNs + "id") != null);
        }

        public static string GetClosestGmlId(XElement element)
        {
            return GetClosestGmlIdElement(element)?.Attribute(GmlNs + "id")?.Value;
        }

        public static XLink GetXLink(XElement element)
        {
            if (element == null)
                return null;

            var xlink = element.GetAttribute("xlink:href")?.Split("#") ?? Array.Empty<string>();

            if (xlink.Length != 2)
                return null;

            var fileName = !string.IsNullOrWhiteSpace(xlink[0]) ? xlink[0] : null;
            var gmlId = !string.IsNullOrWhiteSpace(xlink[1]) ? xlink[1] : null;

            return new XLink(fileName, gmlId);
        }

        public static XElement GetElementByGmlId(IEnumerable<GmlDocument> documents, string gmlId, string fileName)
        {
            return documents
                .SingleOrDefault(document => document.FileName == fileName)
                .GetElementByGmlId(gmlId);
        }

        public static string GetContext(XElement element)
        {
            var feature = GetFeatureElement(element);

            return GetNameAndId(feature);
        }

        public static string GetNameAndId(XElement element)
        {
            var gmlId = element.Attribute(GmlNs + "id")?.Value;

            return $"{element.GetName()}{(!string.IsNullOrWhiteSpace(gmlId) ? $" '{gmlId}'" : "")}";
        }

        public static int GetDimensions(GmlDocument document)
        {
            var dimensions = document.Document.Root.GetElement("*:boundedBy/*:Envelope").GetAttribute("srsDimension");

            return Convert.ToInt32(dimensions);
        }

        public static string GetEpsgCode(string srsName)
        {
            var match = _srsNameRegex.Match(srsName);

            if (!match.Success)
                return null;

            return match.Groups["epsg"].Value;
        }
    }
}
