using PromptEngineeringApp.Models;
using PromptEngineeringApp.Utils;
using System.Text.Json;

namespace PromptEngineeringApp.Generators
{
    public class TestAutomationTool : BaseGenerator
    {
        public async Task AutomateTestsAsync(string testCasePath, string pageObjectPath, string outputPath)
        {
            string testCasesContent = await FileUtils.ReadFileAsync(testCasePath);
            string pageObjectContent = await FileUtils.ReadFileAsync(pageObjectPath);

            string prompt = $@"using test cases which represented as json file and page object class create automation test using C#, XUNit and Playwright framework.
TestCases:\n{testCasesContent}\n PageObject:\n{pageObjectContent}
Use this exact JSON structure for response:
{{
        ""ClassName"": ""Test"",
        ""Namespace"": ""PromptEngineeringApp.Tests"",
        ""Imports"": [
            ""using Microsoft.Playwright;"",
            ""using System.Threading.Tasks;""
        ],
        ""TestMethods"": [
            {{
                ""Name"": ""Test method name"",
                ""Attributes"": [""[Test]"", ""[TestMethod]""],
                ""Parameters"": [],
                ""Body"": ""Test method body with assertions and test logic""
            }}
        ],
        ""Code"": ""Complete C# test class code with proper namespace, imports, and test methods""
    }}";
            string systemMessage = @"Act as test automation engineer.
Based on the provided test case and Page object, create comprehensive automated tests using C#, XUNit and Playwright framework.
Return response in JSON format without any additional text or explanations.";
            string llmResponse = await CallLLMAsync(prompt, systemMessage);
            AutomatedTest automatedTest = new();
            try
            {
                automatedTest = JsonSerializer.Deserialize<AutomatedTest>(llmResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing test automation object : {ex.Message}");
            }
            
            await SaveAutomatedTestAsync(automatedTest, outputPath);
        }

        public async Task SaveAutomatedTestAsync(AutomatedTest automatedTest, string outputPath)
        {
            await File.WriteAllTextAsync(outputPath, automatedTest.Code);
        }
    }
}
