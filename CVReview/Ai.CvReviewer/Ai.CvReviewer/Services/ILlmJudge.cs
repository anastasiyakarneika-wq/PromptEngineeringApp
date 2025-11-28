using System.Text.Json;

namespace Ai.CvReviewer.Tests.Services;

public interface ILlmJudge
{
    Task<JudgeResult> EvaluateEvaluationAsync(CandidateEvaluation evaluation, string requirements, string cv, CancellationToken ct = default);
}

public record JudgeResult(
    double RelevanceScore,
    double AccuracyScore,
    double FairnessScore,
    double ClarityScore,
    double FactualCorrectnessScore,
    string Comments,
    Dictionary<string, string> DetailedScores
);

public class OpenAiLlmJudge : ILlmJudge
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiLlmJudge(string apiKey, string model = "gpt-4o")
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        _model = model;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<JudgeResult> EvaluateEvaluationAsync(CandidateEvaluation evaluation, string requirements, string cv, CancellationToken ct = default)
    {
        var systemPrompt = """
            You are an expert AI evaluation judge. Your role is to assess the quality of CV evaluations based on the following criteria:

            RELEVANCE (0-1): How well does the evaluation address the job requirements and candidate CV?
            ACCURACY (0-1): How accurately do the matched/missing skills reflect the actual content of the CV and requirements?
            FAIRNESS (0-1): Is the evaluation unbiased, non-toxic, and fair across demographic groups?
            CLARITY (0-1): Is the explanation clear, concise, and well-structured (≤3 sentences, contains verbs)?
            FACTUAL CORRECTNESS (0-1): Does the evaluation avoid hallucinations and implausible claims?

            Provide scores for each criterion and detailed comments explaining your assessment.
            """;

        var userPrompt = @$"
            Please evaluate this CV evaluation:

            JOB REQUIREMENTS:
            {requirements}

            CANDIDATE CV:
            {cv}

            EVALUATION TO JUDGE:
            - Fit Score: {evaluation.fitScore}/10
            - Matched Skills: {string.Join(", ", evaluation.matchedSkills)}
            - Missing Skills: {string.Join(", ", evaluation.missingSkills)}
            - Explanation: {evaluation.explanation}

            Provide your assessment in JSON format with the following structure:
            {{
                ""relevance_score"": 0.0,
                ""accuracy_score"": 0.0,
                ""fairness_score"": 0.0,
                ""clarity_score"": 0.0,
                ""factual_correctness_score"": 0.0,
                ""comments"": ""Detailed explanation of your assessment..."",
                ""detailed_scores"": {{
                    ""relevance_reasoning"": ""..."",
                    ""accuracy_reasoning"": ""..."", 
                    ""fairness_reasoning"": ""..."",
                    ""clarity_reasoning"": ""..."",
                    ""factual_reasoning"": ""...""
                }}
            }}
            ";

        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseContent);

        var resultJson = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseJudgeResult(resultJson);
    }

    private JudgeResult ParseJudgeResult(string jsonContent)
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
}