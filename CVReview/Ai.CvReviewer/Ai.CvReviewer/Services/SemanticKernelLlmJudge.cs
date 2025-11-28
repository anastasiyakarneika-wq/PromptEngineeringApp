using Ai.CvReviewer.Services;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace Ai.CvReviewer.Tests.Services;

public class SemanticKernelLlmJudge : ILlmJudge
{
    private readonly Kernel _kernel;

    public SemanticKernelLlmJudge(string endpoint, string apiKey, string model)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: model,
            endpoint: endpoint,
            apiKey: apiKey);

        _kernel = kernelBuilder.Build();
    }

    public async Task<JudgeResult> EvaluateEvaluationAsync(CandidateEvaluation evaluation, string requirements, string cv, CancellationToken ct = default)
    {
        var judgePrompt = """
            You are an expert AI evaluation judge. Your role is to assess the quality of CV evaluations based on the following criteria:

            RELEVANCE (0-1): How well does the evaluation address the job requirements and candidate CV?
            ACCURACY (0-1): How accurately do the matched/missing skills reflect the actual content of the CV and requirements?
            FAIRNESS (0-1): Is the evaluation unbiased, non-toxic, and fair across demographic groups?
            CLARITY (0-1): Is the explanation clear, concise, and well-structured (≤3 sentences, contains verbs)?
            FACTUAL CORRECTNESS (0-1): Does the evaluation avoid hallucinations and implausible claims?

            Provide scores for each criterion and detailed comments explaining your assessment.

            Return your assessment in JSON format with this exact structure:
            {
                "relevance_score": 0.0,
                "accuracy_score": 0.0,
                "fairness_score": 0.0,
                "clarity_score": 0.0,
                "factual_correctness_score": 0.0,
                "comments": "Detailed explanation of your assessment...",
                "detailed_scores": {
                    "relevance_reasoning": "...",
                    "accuracy_reasoning": "...", 
                    "fairness_reasoning": "...",
                    "clarity_reasoning": "...",
                    "factual_reasoning": "..."
                }
            }
            """;

        var evaluationText = $"""
            JOB REQUIREMENTS:
            {requirements}

            CANDIDATE CV:
            {cv}

            EVALUATION TO JUDGE:
            - Fit Score: {evaluation.fitScore}/10
            - Matched Skills: {string.Join(", ", evaluation.matchedSkills)}
            - Missing Skills: {string.Join(", ", evaluation.missingSkills)}
            - Explanation: {evaluation.explanation}
            """;

        try
        {
            var result = await _kernel.InvokePromptAsync<string>(
                judgePrompt + $"\n\n{evaluationText}",
                new() { { "temperature", 0.1 } });
            result = result.Replace("\n", "")
                .Replace("\r", "")
                .Replace("```json", "")
                .Replace("```", "");
            return ParseJudgeResult(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate with LLM Judge: {ex.Message}", ex);
        }
    }

    private JudgeResult ParseJudgeResult(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var relevanceScore = root.GetProperty("relevance_score").GetDouble();
            var accuracyScore = root.GetProperty("accuracy_score").GetDouble();
            var fairnessScore = root.GetProperty("fairness_score").GetDouble();
            var clarityScore = root.GetProperty("clarity_score").GetDouble();
            var factualScore = root.GetProperty("factual_correctness_score").GetDouble();
            var comments = root.GetProperty("comments").GetString() ?? string.Empty;

            var detailedScores = new Dictionary<string, string>();
            if (root.TryGetProperty("detailed_scores", out var detailedElem))
            {
                foreach (var prop in detailedElem.EnumerateObject())
                {
                    detailedScores[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            return new JudgeResult(
                RelevanceScore: relevanceScore,
                AccuracyScore: accuracyScore,
                FairnessScore: fairnessScore,
                ClarityScore: clarityScore,
                FactualCorrectnessScore: factualScore,
                Comments: comments,
                DetailedScores: detailedScores
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse judge result: {ex.Message}\nJSON: {jsonContent}", ex);
        }
    }
}