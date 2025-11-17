namespace PromptEngineeringApp.Models
{
    public class PageMethod
    {
        public required string Name { get; set; }
        public required string ReturnType { get; set; }
        public List<string> Parameters { get; set; } = [];
        public required string Body { get; set; }
    }
}
