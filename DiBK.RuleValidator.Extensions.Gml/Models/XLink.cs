namespace DiBK.RuleValidator.Extensions.Gml
{
    public class XLink
    {
        public string FileName { get; set; }
        public string GmlId { get; set; }

        public XLink(string fileName, string gmlId)
        {
            FileName = fileName;
            GmlId = gmlId;
        }
    }
}
