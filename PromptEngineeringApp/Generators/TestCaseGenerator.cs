using PromptEngineeringApp.Models;
using PromptEngineeringApp.Utils;
using System.Text.Json;


namespace PromptEngineeringApp.Generators
{
    public class TestCaseGenerator : BaseGenerator
    {
        public async Task GenerateTestCasesAsync(string inputPath, string outputPath)
        { 
            string requirements = await FileUtils.ReadFileAsync(inputPath);

            var prompt = $@"Generate test cases based on next requirements: 
                        {requirements}
                        return the response as JSON with the following structure:
                        {{""TestCases"": {{ ""Id"": ""unique number"", ""Title"": ""test case title"", ""Description"": ""description of test"",
                        ""Steps"": {{""StepNumber"": ""number of step"", ""Action"": ""action for step"", ""Element"", ""can be skipped"", ""Data"": ""step data if needed""}}<
                        ""ExpectedResult"": ""test case result""}} }}";
            var systemMessage = @"You are test automation engineer with experience in writting test cases
                                Based on the provided requirements create test cases. 
                                Return structured response in JSON format.";

            var llmResponse = await CallLLMAsync(prompt, systemMessage);

            TestCasesList list = new();
            try
            {
                list = JsonSerializer.Deserialize<TestCasesList>(llmResponse);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error parsing test cases : {ex.Message}");
            }

            await SaveTestCasesAsync(list, outputPath);
            
        }
        
        public async Task SaveTestCasesAsync(TestCasesList testCases, string outputPath)
        {
            string json = JsonSerializer.Serialize(testCases, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);
        }
    }
}