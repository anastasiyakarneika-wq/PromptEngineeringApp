namespace PromptEngineeringApp.Models;

public class TestCase
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public List<TestStep> Steps { get; set; } = [];
    public required string ExpectedResult { get; set; }
}