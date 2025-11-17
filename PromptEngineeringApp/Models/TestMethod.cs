namespace PromptEngineeringApp.Models
{
    public class TestMethod
    {       
        public required string Name { get; set; }
        public List<string> Attributes { get; set; } = [];
        public List<string> Parameters { get; set; } = [];
        public required string Body { get; set; }
    }
}