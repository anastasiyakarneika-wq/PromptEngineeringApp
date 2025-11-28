using System.Text.Json;

namespace Ai.CvReviewer.Tests.Services;

public interface ISentimentAnalyzer
{
    Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken ct = default);
}

public record SentimentResult(string Label, float Score);

public class HuggingFaceSentimentAnalyzer : ISentimentAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public HuggingFaceSentimentAnalyzer(string apiToken, string apiUrl)
    {
        _httpClient = new HttpClient();
        _apiUrl = apiUrl;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
    }

    public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new SentimentResult("NEUTRAL", 0.5f);

        var request = new
        {
            inputs = text
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiUrl}/distilbert-base-uncased-finetuned-sst-2-english",
                content, ct);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseContent);

            var firstItem = doc.RootElement.EnumerateArray().First();
            var label = firstItem.GetProperty("label").GetString() ?? "NEUTRAL";
            var score = firstItem.GetProperty("score").GetSingle();

            return new SentimentResult(label, score);
        }
        catch (Exception ex)
        {
            // Fallback for when service is unavailable
            Console.WriteLine($"Sentiment analysis failed: {ex.Message}");
            return new SentimentResult("NEUTRAL", 0.5f);
        }
    }
}