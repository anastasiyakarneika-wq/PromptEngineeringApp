
namespace PromptEngineeringApp.Models
{
    public class AutomatedTest
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public List<string> Imports { get; set; } = [];
        public List<TestMethod> TestMethods { get; set; } = [];
        public string Code { get; set; }
    }
}
