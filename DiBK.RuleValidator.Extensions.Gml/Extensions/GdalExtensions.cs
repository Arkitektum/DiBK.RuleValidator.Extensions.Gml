using NetTopologySuite.IO;
using OSGeo.OGR;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public static class GdalExtensions
    {
        public static double[] GetPoint(this Geometry geometry, int index)
        {
            var point = new double[3];
            geometry.GetPoint(index, point);

            return point;
        }

        public static double[][] GetPoints(this Geometry geometry)
        {
            var pointCount = geometry.GetPointCount();
            var points = new double[pointCount][];

            for (int i = 0; i < pointCount; i++)
                points[i] = GetPoint(geometry, i);

            return points;
        }

        public static bool EqualsTopologically(this Geometry geometry, Geometry other)
        {
            return geometry.Within(other) && other.Within(geometry);
        }

        public static bool TryCreateNtsGeometry(Geometry ogrGeometry, out NtsGeometry ntsGeometry)
        {
            try
            {
                using var forcedGeometry = ForceGeometry(ogrGeometry);
                var bytes = new byte[forcedGeometry.WkbSize()];
                forcedGeometry.ExportToWkb(bytes);
                ntsGeometry = new WKBReader().Read(bytes);
                return true;
            }
            catch
            {
                ntsGeometry = null;
                return false;
            }
        }

        private static Geometry ForceGeometry(Geometry geometry)
        {
            return geometry.GetGeometryType() switch
            {
                wkbGeometryType.wkbPoint or wkbGeometryType.wkbMultiPoint or wkbGeometryType.wkbLineString or
                wkbGeometryType.wkbLinearRing or wkbGeometryType.wkbMultiLineString or wkbGeometryType.wkbPolygon or
                wkbGeometryType.wkbMultiPolygon or wkbGeometryType.wkbGeometryCollection => geometry,
                wkbGeometryType.wkbCircularString or wkbGeometryType.wkbCurve => Ogr.ForceToLineString(geometry),
                wkbGeometryType.wkbMultiCurve => Ogr.ForceToMultiLineString(geometry),
                wkbGeometryType.wkbSurface => Ogr.ForceToPolygon(geometry),
                wkbGeometryType.wkbMultiSurface => Ogr.ForceToMultiPolygon(geometry),
                _ => null
            };
        }
    }
}
