namespace PromptEngineeringApp.Models
{
    public class PageElement
    {
        public required string Name { get; set; }
        public required string Locator { get; set; }
        public string? Description { get; set; }
    }
}
