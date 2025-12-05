using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using RagDefectDetection.Core.Models;
using System.Text.Json;

namespace RagDefectDetection
{
    public class DuplicatesDetector
    {
        private readonly IChatCompletionService _chatService;
        private readonly SimilarDefectsRetriever _retriever;
        private readonly string _modelId;

        public DuplicatesDetector(
            IChatCompletionService chatService,
            SimilarDefectsRetriever retriever,
            string modelId)
        {
            _chatService = chatService;
            _retriever = retriever;
            _modelId = modelId;
        }

        public async Task<BugDuplicateReport> DetectDuplicateAsync(string summary, string description)
        {
            var query = $"{summary}. {description}";

            // 1. Retrieve Context (RAG)
            var context = await _retriever.RetrieveSimilarDefectsContextAsync(query);

            // 2. Prepare Prompt
            var prompt = GetPromptTemplate()
                .Replace("{context}", context)
                .Replace("{query}", query);

            // 3. Call LLM
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            // Configure settings to enforce JSON mode if supported, 
            // or just rely on the prompt instruction.
            var settings = new OpenAIPromptExecutionSettings
            {
                ModelId = _modelId,
                Temperature = 0.0, // Deterministic
                ResponseFormat = "json_object" // Force JSON output
            };

            try
            {
                var result = await _chatService.GetChatMessageContentAsync(history, settings);
                var content = result.Content;

                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception("LLM returned empty response");

                // 4. Parse Result
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var report = JsonSerializer.Deserialize<BugDuplicateReport>(content, options);

                return report ?? throw new Exception("Failed to deserialize report");
            }
            catch (Exception ex)
            {
                // Fallback if JSON parsing fails or other errors
                return new BugDuplicateReport
                {
                    IsDuplicate = false,
                    Reason = $"Error during analysis: {ex.Message}",
                    Defects = new List<Bug>(),
                    Confidence = 0.0f
                };
            }
        }

        public async Task SaveReportAsync(BugDuplicateReport report, string path)
        {
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"Report saved to {path}");
        }

        private string GetPromptTemplate()
        {
            return @"You are an expert software defect analyst. Your task is to determine
if a new defect is a duplicate of existing defects.

RULES FOR DUPLICATE DETECTION:
1. Two defects are duplicates ONLY if they have the SAME ROOT CAUSE
2. Similar symptoms alone DO NOT make defects duplicates
3. Confidence > 0.8 required to mark as duplicate
4. Focus on: root cause, reproduction steps, error messages
5. Different components with same root cause ARE duplicates

CONTEXT - Similar defects from database:
{context}

NEW DEFECT:
{query}

Analyze if the new defect is a duplicate of any defect in the context.
Return your analysis as a valid JSON object with this exact structure:
{
  ""IsDuplicate"": boolean,
  ""Reason"": ""detailed explanation"",
  ""Defects"": [
      // array of duplicate defect objects found in context. 
      // Include fields: Id, Summary, Description, Severity, Status, Component, Priority
  ],
  ""Confidence"": float between 0.0 and 1.0
}";
        }
    }
}