using System.Collections.Generic;
using System.Linq;

namespace DiBK.RuleValidator.Extensions.Gml.Models
{
    public class Surface
    {
        public Ring Exterior { get; set; }
        public List<Ring> Interior { get; set; } = new();

        public string ToWkt()
        {
            var curvePolygon = "CURVEPOLYGON ({0})";

            var wkts = Interior
                .Select(interior => interior.ToWkt())
                .ToList();

            wkts.Insert(0, Exterior.ToWkt());

            return string.Format(curvePolygon, string.Join(", ", wkts));
        }
    }
}
