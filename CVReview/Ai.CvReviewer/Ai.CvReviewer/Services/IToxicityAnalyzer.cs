using System.Text.Json;

namespace Ai.CvReviewer.Services;

public interface IToxicityAnalyzer
{
    Task<float> GetToxicityScoreAsync(string text, CancellationToken ct = default);
}

public class HuggingFaceToxicityAnalyzer : IToxicityAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    public HuggingFaceToxicityAnalyzer(string apiToken, string apiUrl)
    {
        _httpClient = new HttpClient();
        _apiUrl = apiUrl;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
    }

    public async Task<float> GetToxicityScoreAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0.0f;

        var request = new
        {
            inputs = text
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiUrl}/unitary/toxic-bert",
                content, ct);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseContent);

            var firstItem = doc.RootElement.EnumerateArray().First();
            var score = firstItem.GetProperty("score").GetSingle();

            return score;
        }
        catch (Exception ex)
        {
            // Fallback for when service is unavailable
            Console.WriteLine($"Toxicity analysis failed: {ex.Message}");
            return 0.0f;
        }
    }
}