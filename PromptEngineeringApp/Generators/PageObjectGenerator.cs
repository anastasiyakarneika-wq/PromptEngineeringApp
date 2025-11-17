using PromptEngineeringApp.Models;
using PromptEngineeringApp.Utils;
using System.Text.Json;

namespace PromptEngineeringApp.Generators
{
    public class PageObjectGenerator : BaseGenerator
    {
        public async Task GeneratePageObjectAsync(string tracesPath, string outputPath)
        {
            string traces = await FileUtils.ReadFileAsync(tracesPath);
            var prompt = $@"Create a page object class using c# programming language based on event log for some actions within a web application:{traces}
Use this exact JSON structure for response:
{{
        ""ClassName"": ""TodoPage"",
        ""Namespace"": ""PromptEngineeringApp"",
        ""Imports"": [""string""],
        ""Fields"": [{{""Name"": ""string"", ""Locator"": ""string"", ""Description"": ""string""}}],
        ""Methods"": [{{""Name"": ""string"", ""ReturnType"": ""string"", ""Parameters"": [""string""], ""Body"": ""string""}}],
        ""Code"": ""string""
    }}
";

            var systemMessage = @"You are a test automation engineer. Based on the provided event log, create a page object class using C# and Playwright framework.
IMPORTANT: Return ONLY valid JSON format without any additional text, explanations, or code comments. Your response should be parseable by JsonSerializer.";
            var llmResponse = await CallLLMAsync(prompt, systemMessage);
            
            PageObject pageObject = new();
            try
            {
                pageObject = JsonSerializer.Deserialize<PageObject>(llmResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing page object : {ex.Message}");
            }
            await SavePageObjectAsync(pageObject, outputPath);
        }

        public async Task SavePageObjectAsync(PageObject pageObject, string outputPath)
        {
            await File.WriteAllTextAsync(outputPath, pageObject.Code);
        }
    }
}
