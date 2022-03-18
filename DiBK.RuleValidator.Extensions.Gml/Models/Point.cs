using System;
using System.Globalization;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point()
        {
        }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public string ToWkt()
        {
            return $"POINT ({ToString()})";
        }

        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public override bool Equals(object obj)
        {
            return obj is Point point &&
                   X == point.X &&
                   Y == point.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
