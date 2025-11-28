using System.Text.Json;

namespace Ai.CvReviewer.Tests.Services;

public interface IGrammarChecker
{
    Task<int> GetGrammarErrorCountAsync(string text, CancellationToken ct = default);
}

public class LanguageToolChecker : IGrammarChecker
{
    private readonly HttpClient _httpClient;

    public LanguageToolChecker()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri("https://api.languagetool.org/v2/") };
    }

    public async Task<int> GetGrammarErrorCountAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("text", text),
            new KeyValuePair<string, string>("language", "en-US")
        });

        var response = await _httpClient.PostAsync("check", content, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseContent);

        return doc.RootElement.GetProperty("matches").GetArrayLength();
    }
}