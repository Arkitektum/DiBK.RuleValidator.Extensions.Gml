using NetTopologySuite.IO;
using OSGeo.OGR;
using System;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public static class GdalExtensions
    {
        public static double[][] GetPoints(this Geometry geometry)
        {
            var pointCount = geometry.GetPointCount();
            var points = new double[pointCount][];

            for (int i = 0; i < pointCount; i++)
            {
                var point = new double[3];
                geometry.GetPoint(i, point);
                points[i] = point;
            }
            
            return points;
        }

        public static bool EqualsTopologically(this Geometry geometry, Geometry other)
        {
            return geometry.Within(other) && other.Within(geometry);
        }

        public static bool TryConvertToNtsGeometry(this Geometry geometry, out NtsGeometry ntsGeometry, double maxAngleStepSizeDegrees = 0.01)
        {
            try
            {
                var linearGeometry = geometry.GetLinearGeometry(maxAngleStepSizeDegrees, Array.Empty<string>());
                linearGeometry.ExportToWkt(out var wkt);
                ntsGeometry = new WKTReader().Read(wkt);

                return true;
            }
            catch
            {
                ntsGeometry = null;
                return false;
            }
        }
    }
}
