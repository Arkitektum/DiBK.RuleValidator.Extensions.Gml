using NetTopologySuite.Operation.Valid;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpatialReference = OSGeo.OSR.SpatialReference;
using DiBK.RuleValidator.Extensions.Gml.Models;
using System.Collections.Concurrent;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public class GeometryHelper
    {
        private static readonly Regex _exceptionRegex = new(@"^\w+Exception: ", RegexOptions.Compiled);

        public static Geometry CreatePoint(double x, double y, double z = 0, int? epsg = null)
        {
            var point = new Geometry(wkbGeometryType.wkbPoint);

            if (epsg.HasValue)
            {
                var spatialReference = new SpatialReference(null);
                spatialReference.ImportFromEPSG(epsg.Value);

                point.AssignSpatialReference(spatialReference);
            }

            point.AddPoint(x, y, z);

            return point;
        }

        public static Geometry CreateLineString(double[][] points, int? epsg = null)
        {
            var lineString = new Geometry(wkbGeometryType.wkbLineString);

            if (epsg.HasValue)
            {
                var spatialReference = new SpatialReference(null);
                spatialReference.ImportFromEPSG(epsg.Value);

                lineString.AssignSpatialReference(spatialReference);
            }

            for (int i = 0; i < points.Length; i++)
                lineString.AddPoint(points[i][0], points[i][1], 0);

            return lineString;
        }

        public static Geometry CreateLineString(XElement element, int? epsg = null)
        {
            var points = GetCoordinates(element).ToArray();

            return CreateLineString(points, epsg);
        }

        public static Geometry CreatePolygon(double[][] points, int? epsg = null)
        {
            var polygon = new Geometry(wkbGeometryType.wkbPolygon);
            var linearRing = new Geometry(wkbGeometryType.wkbLinearRing);

            if (epsg.HasValue)
            {
                var spatialReference = new SpatialReference(null);
                spatialReference.ImportFromEPSG(epsg.Value);

                polygon.AssignSpatialReference(spatialReference);
                linearRing.AssignSpatialReference(spatialReference);
            }

            foreach (var point in points)
                linearRing.AddPoint(point[0], point[1], 0);

            polygon.AddGeometry(linearRing);

            return polygon;
        }

        public static Geometry CreatePolygon(XElement element, int? epsg = null)
        {
            var points = GetCoordinates(element).ToArray();

            return CreatePolygon(points, epsg);
        }

        public static Geometry CreatePolygonFromRing(Geometry ring)
        {
            Geometry polygon = null;

            if (ring.GetGeometryType() == wkbGeometryType.wkbCompoundCurve)
            {
                polygon = new Geometry(wkbGeometryType.wkbCurvePolygon);
                polygon.AddGeometry(ring);
            }
            else if (ring.GetGeometryType() == wkbGeometryType.wkbLineString)
            {
                polygon = new Geometry(wkbGeometryType.wkbPolygon);
                using var linearRing = new Geometry(wkbGeometryType.wkbLinearRing);

                var points = ring.GetPoints();

                foreach (var point in points)
                    linearRing.AddPoint_2D(point[0], point[1]);

                polygon.AddGeometry(linearRing);
            }

            return polygon;
        }

        public static NetTopologySuite.Geometries.Geometry CreateEnvelope(Ring ring)
        {
            var points = ring.Segments
                .SelectMany(segment => segment.Points)
                .Select(point => new NetTopologySuite.Geometries.Point(point.X, point.Y))
                .ToArray();

            var multiPoint = new NetTopologySuite.Geometries.MultiPoint(points);

            return multiPoint.Envelope;
        }

        public static Geometry GetFootprintOfSolid(XElement geoElement)
        {
            if (geoElement == null)
                return null;

            var posLists = GetPosLists(geoElement);

            if (!posLists.Any())
                return null;

            using var multiPolygon = new Geometry(wkbGeometryType.wkbMultiPolygon);

            foreach (var posList in posLists)
            {
                var coords = PosListToCoordinates(posList, 3);

                using var polygon = new Geometry(wkbGeometryType.wkbPolygon);
                using var ring = new Geometry(wkbGeometryType.wkbLinearRing);

                ring.AddPoint(coords[0][0], coords[0][1], 0);
                ring.AddPoint(coords[1][0], coords[1][1], 0);
                ring.AddPoint(coords[2][0], coords[2][1], 0);
                ring.AddPoint(coords[3][0], coords[3][1], 0);

                polygon.AddGeometry(ring);
                multiPolygon.AddGeometry(polygon);
            }

            return multiPolygon.UnionCascaded();
        }

        public static List<string> GetPosLists(XElement geoElement)
        {
            return geoElement.GetValues<string>("//*:posList | //*:pos").ToList();
        }

        public static List<double[]> GetCoordinates(XElement geoElement, int dimensions = 2)
        {
            return GetPosLists(geoElement)
                .SelectMany(posList => PosListToCoordinates(posList, dimensions))
                .ToList();
        }

        public static List<double[]> PosListsToCoordinates(List<string> posLists, int dimensions = 2)
        {
            return posLists
                .SelectMany(posList => PosListToCoordinates(posList, dimensions))
                .ToList();
        }

        public static List<double[]> PosListToCoordinates(string posList, int dimensions = 2)
        {
            if (posList == null)
                throw new Exception("Element gml:posList eksisterer ikke");

            var posStrings = posList.Split(" ");

            if (posStrings.Length == 0 || posStrings.Length % dimensions != 0)
                throw new Exception($"Element gml:posList har ugyldig antall koordinater: '{posStrings.Length}'");

            static double posStringToDouble(string posString)
            {
                try
                {
                    return double.Parse(posString, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception($"Element gml:posList har ugyldig koordinat: '{posString}'");
                }
            }

            var positions = new List<double[]>();

            for (var i = dimensions - 1; i < posStrings.Length; i += dimensions)
            {
                var x = dimensions == 2 ? posStringToDouble(posStrings[i - 1]) : posStringToDouble(posStrings[i - 2]);
                var y = dimensions == 2 ? posStringToDouble(posStrings[i]) : posStringToDouble(posStrings[i - 1]);
                var z = dimensions == 2 ? 0 : posStringToDouble(posStrings[i]);

                positions.Add(new[] { x, y, z });
            }

            return positions;
        }

        public static DisposableList<Geometry> PosListsToPoints(List<string> posLists, int dimensions = 2)
        {
            var coordinates = PosListsToCoordinates(posLists, dimensions);
            var points = new DisposableList<Geometry>();

            foreach (var coordinate in coordinates)
            {
                points.Add(CreatePoint(coordinate[0], coordinate[1], coordinate[2]));
            }

            return points;
        }

        public static bool Within(XElement geoElement, Geometry geometry, double threshold, out List<double[][]> outsidePoints)
        {
            outsidePoints = new List<double[][]>();

            var posLists = GetPosLists(geoElement);
            using var points = PosListsToPoints(posLists);

            foreach (var point in points)
                if (point.Distance(geometry) > threshold)
                    outsidePoints.Add(point.GetPoints());

            return !outsidePoints.Any();
        }

        public static bool Overlaps(Geometry surface1, Geometry surface2)
        {
            if (!surface1.Intersects(surface2))
                return false;

            return !surface1.Within(surface2) && !surface2.Within(surface1);
        }

        public static Geometry DetectSelfIntersection(Geometry surface)
        {
            if (!surface.TryConvertToNtsGeometry(out var ntsPolygon))
                return null;
            
            var validOperation = new IsValidOp(ntsPolygon);
            var error = validOperation.ValidationError;

            if (error != null && (error.ErrorType == TopologyValidationErrors.RingSelfIntersection || error.ErrorType == TopologyValidationErrors.SelfIntersection))
            {
                var point = new Geometry(wkbGeometryType.wkbPoint);
                point.AddPoint_2D(error.Coordinate.X, error.Coordinate.Y);
                return point;
            }

            return null;
        }

        public static string GetZoomToPoint(Geometry point)
        {
            var pointX = point.GetX(0).ToString(CultureInfo.InvariantCulture);
            var pointY = point.GetY(0).ToString(CultureInfo.InvariantCulture);

            return $"POINT ({pointX} {pointY})";
        }

        public static string ToWkt(Geometry geometry, int? decimals)
        {
            geometry.ExportToWkt(out var wkt);

            if (decimals.HasValue && decimals.Value > 0)
                return Regex.Replace(wkt, $@"(\d+\.\d{{{decimals}}})\d+", "$1");

            return wkt;
        }

        public static List<List<Geometry>> GetLineSegmentsOfPolygon(Geometry polygon)
        {
            var geometryCount = polygon.GetGeometryCount();
            var lineSegmentsList = new List<List<Geometry>>();

            for (var i = 0; i < geometryCount; i++)
            {
                using var ring = polygon.GetGeometryRef(i);
                var points = ring.GetPoints();

                if (!points.Any())
                    continue;

                var lineSegments = new List<Geometry>();

                for (var j = 1; j < points.Length; j++)
                {
                    var x1 = points[j - 1][0];
                    var y1 = points[j - 1][1];
                    var x2 = points[j][0];
                    var y2 = points[j][1];

                    var lineString = new Geometry(wkbGeometryType.wkbLineString);
                    lineString.AddPoint(x1, y1, 0);
                    lineString.AddPoint(x2, y2, 0);

                    lineSegments.Add(lineString);
                }

                lineSegmentsList.Add(lineSegments);
            }

            return lineSegmentsList;
        }

        public static bool PointsAreClockWise(List<double[]> points)
        {
            double sum = 0;

            for (int i = 1; i < points.Count; i++)
                sum += (points[i][0] - points[i - 1][0]) * (points[i][1] + points[i - 1][1]);

            return sum >= 0;
        }

        public static bool LineSegmentsAreConnected(Geometry lineA, Geometry lineB)
        {
            var pointsA = lineA.GetPoints();
            var pointsB = lineB.GetPoints();

            return pointsA[1][0] == pointsB[0][0] && pointsA[1][1] == pointsB[0][1] ||
                pointsA[0][0] == pointsB[1][0] && pointsA[0][1] == pointsB[1][1];
        }

        public static Circle PointsToCircle(double[] p1, double[] p2, double[] p3)
        {
            var ax = (p1[0] + p2[0]) / 2;
            var ay = (p1[1] + p2[1]) / 2;
            var ux = p1[1] - p2[1];
            var uy = p2[0] - p1[0];
            var bx = (p2[0] + p3[0]) / 2;
            var by = (p2[1] + p3[1]) / 2;
            var vx = p2[1] - p3[1];
            var vy = p3[0] - p2[0];
            var dx = ax - bx;
            var dy = ay - by;
            var vu = vx * uy - vy * ux;

            if (vu == 0)
                return null;

            var g = (dx * uy - dy * ux) / vu;
            var cx = bx + g * vx;
            var cy = by + g * vy;
            var radius = Math.Sqrt(Math.Pow(p2[0] - cx, 2) + Math.Pow(p2[1] - cy, 2));

            return new Circle
            {
                Center = new Point(cx, cy),
                Radius = radius
            };
        }

        public static List<Surface> ConvertSegmentsToSurfaces(IEnumerable<Segment> segments)
        {
            var rings = new List<Ring> { new Ring() };
            var firstSegment = segments.First();
            var restSegments = segments.Skip(1).ToList();

            rings.Last().Segments.Add(firstSegment);

            while (restSegments.Any())
            {
                var lastPoint = rings.Last().Segments.Last().Points.Last();
                var found = false;

                foreach (var segment in restSegments)
                {
                    if (lastPoint.Equals(segment.Points.First()))
                    {
                        rings.Last().Segments.Add(segment);
                        restSegments.Remove(segment);
                        found = true;
                        break;
                    }
                    else if (lastPoint.Equals(segment.Points.Last()))
                    {
                        var newSegment = new Segment { Points = Enumerable.Reverse(segment.Points).ToList(), Type = segment.Type };
                        rings.Last().Segments.Add(newSegment);
                        restSegments.Remove(segment);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var firstRestSegment = restSegments.First();
                    rings.Add(new Ring(new List<Segment> { firstRestSegment }));
                    restSegments.Remove(firstRestSegment);
                }
            }

            List<Surface> surfaces;

            if (rings.Count == 1)
            {
                var exterior = rings.Single();
                exterior.IsExterior = true;
                surfaces = new List<Surface> { new Surface { Exterior = exterior } };
            }
            else
            {
                surfaces = ConvertRingsToSurfaces(rings);
            }

            return surfaces;
        }

        public static List<Surface> ConvertRingsToSurfaces(List<Ring> rings)
        {
            foreach (var ring in rings)
                ring.Envelope = CreateEnvelope(ring);

            for (var i = 0; i < rings.Count; i++)
            {
                var ring = rings[i];
                var otherRings = rings.Where(rng => rng != ring).ToList();

                for (int j = 0; j < otherRings.Count; j++)
                {
                    var otherRing = otherRings[j];

                    if (ring.Envelope.Within(otherRing.Envelope))
                        rings.SingleOrDefault(rng => rng == ring).WithinRings.Add(otherRing);
                }
            }

            foreach (var ring in rings)
            {
                ring.IsExterior = ring.WithinRings.Count % 2 == 0;
                ring.WithinRings.RemoveAll(ring => !ring.IsExterior);
            }

            var surfaces = rings.Where(ring => ring.IsExterior).Select(ring => new Surface { Exterior = ring }).ToList();
            var interiors = rings.Where(ring => !ring.IsExterior).OrderBy(ring => ring.WithinRings.Count);

            foreach (var interior in interiors.ToList())
            {
                foreach (var ring in interior.WithinRings.ToList())
                {
                    var exterior = surfaces.SingleOrDefault(polygon => polygon.Exterior == ring);

                    if (exterior == null)
                        continue;

                    exterior.Interior.Add(interior);
                    interior.WithinRings.Remove(ring);

                    interiors
                        .Where(interior => interior.WithinRings.Count > 1)
                        .ToList()
                        .ForEach(interior => interior.WithinRings.Remove(ring));
                }
            }

            return surfaces;
        }

        public static Geometry GeometryFromGML(XElement geoElement)
        {
            try
            {
                return Geometry.CreateFromGML(geoElement.ToString());
            }
            catch (Exception exception)
            {
                throw new GeometryFromGMLException($"Ugyldig geometri: {geoElement.GetName()} '{geoElement.GetAttribute("gml:id")}'. Detaljert feil: {_exceptionRegex.Replace(exception.Message, "")}");
            }
        }

        public static Geometry GetOrCreateGeometry(ConcurrentDictionary<string, IndexedGeometry> geometryIndex, XElement geoElement, out string errorMessage)
        {
            var xPath = geoElement?.GetXPath();

            if (xPath == null)
            {
                errorMessage = "Ugyldig GML-element";
                return null;
            }

            if (geometryIndex.TryGetValue(xPath, out var indexed))
            {
                errorMessage = indexed.ErrorMessage;
                return indexed.Geometry?.Clone();
            }

            var newIndexed = IndexedGeometry.Create(geoElement);
            errorMessage = null;

            geometryIndex.AddOrUpdate(xPath, newIndexed, (xPath, indexed) => newIndexed);

            return newIndexed.Geometry?.Clone();
        }
    }
}
