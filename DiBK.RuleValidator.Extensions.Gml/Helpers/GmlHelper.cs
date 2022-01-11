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
            "CompositeCurve",
            "CompositeSolid",
            "CompositeSurface",
            "Curve",
            "GeometricComplex",
            "Grid",
            "LineString",
            "MultiCurve",
            "MultiGeometry",
            "MultiPoint",
            "MultiSolid",
            "MultiSurface",
            "OrientableCurve",
            "OrientableSurface",
            "Point",
            "Polygon",
            "PolyhedralSurface",
            "RectifiedGrid",
            "Solid",
            "Surface",
            "Tin",
            "TriangulatedSurface"
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

        public static XElement GetElementByXLink(IEnumerable<GmlDocument> documents, XLink xLink)
        {
            return documents
                .SingleOrDefault(document => document.FileName == xLink.FileName)?.Document
                .Descendants()
                .SingleOrDefault(element => element.Attribute(GmlNs + "id")?.Value == xLink.GmlId);
        }

        public static XElement GetElementByGmlId(GmlDocument document, string gmlId)
        {
            if (document == null || gmlId == null)
                return null;

            XName name = GmlNs + "id";

            return document.Document
                .Descendants()
                .SingleOrDefault(element => element.Attribute(name)?.Value == gmlId);
        }

        public static XElement GetElementByGmlId(IEnumerable<GmlDocument> documents, string gmlId, string fileName)
        {
            var document = documents.SingleOrDefault(document => document.FileName == fileName);

            return GetElementByGmlId(document, gmlId);
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
