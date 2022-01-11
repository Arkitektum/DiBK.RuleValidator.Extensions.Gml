using System.Collections.Generic;

namespace DiBK.RuleValidator.Extensions.Gml
{
    public static class GmlRuleExtensions
    {
        public static void AddMessage(this ExecutableRule rule, string message, string fileName, IEnumerable<string> xPaths, IEnumerable<string> gmlIds)
        {
            rule.AddMessage(new RuleMessage
            {
                Message = message,
                Properties = new Dictionary<string, object>
                {
                    { "FileName", fileName },
                    { "XPaths", xPaths },
                    { "GmlIds", gmlIds }
                }
            });
        }

        public static void AddMessage(this ExecutableRule rule, string message, string fileName, IEnumerable<string> xPaths, IEnumerable<string> gmlIds, string zoomTo)
        {
            rule.AddMessage(new RuleMessage
            {
                Message = message,
                Properties = new Dictionary<string, object>
                {
                    { "FileName", fileName },
                    { "XPaths", xPaths },
                    { "GmlIds", gmlIds },
                    { "ZoomTo", zoomTo }
                }
            });
        }
    }
}