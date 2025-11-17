namespace PromptEngineeringApp.Models
{
    public class PageObject
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public List<string> Imports { get; set; } = [];
        public List<PageElement> Fields { get; set; } = [];
        public List<PageMethod> Methods { get; set; } = [];
        public string Code { get; set; }
    }
}
