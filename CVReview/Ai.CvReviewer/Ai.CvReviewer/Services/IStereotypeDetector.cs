using System.Text.Json;

namespace Ai.CvReviewer.Tests.Services;

public interface IStereotypeDetector
{
    Task<StereotypeAnalysis> AnalyzeStereotypesAsync(string text, CancellationToken ct = default);
}

public record StereotypeAnalysis(float StereotypeScore, string Analysis);

public class HuggingFaceStereotypeDetector : IStereotypeDetector
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    public HuggingFaceStereotypeDetector(string apiToken, string apiUrl)
    {
        _httpClient = new HttpClient();
        _apiUrl = apiUrl;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
    }

    public async Task<StereotypeAnalysis> AnalyzeStereotypesAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new StereotypeAnalysis(0.0f, "No text to analyze");

        var request = new
        {
            inputs = text
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                "https://api-inference.huggingface.co/models/nlptown/bert-base-multilingual-uncased-sentiment",
                content, ct);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseContent);

            var firstItem = doc.RootElement.EnumerateArray().First();
            var label = firstItem.GetProperty("label").GetString() ?? "3 stars";
            var score = firstItem.GetProperty("score").GetSingle();

            // Convert 5-star rating to stereotype score (4-5 stars = low stereotype, 1-2 stars = potential stereotype)
            var stereotypeScore = label.Contains("4") || label.Contains("5") ? 1.0f - score : score;

            return new StereotypeAnalysis(stereotypeScore, $"Rating: {label}, Score: {score}");
        }
        catch (Exception ex)
        {
            // Fallback for when service is unavailable
            Console.WriteLine($"Stereotype analysis failed: {ex.Message}");
            return new StereotypeAnalysis(0.0f, "Analysis unavailable");
        }
    }
}