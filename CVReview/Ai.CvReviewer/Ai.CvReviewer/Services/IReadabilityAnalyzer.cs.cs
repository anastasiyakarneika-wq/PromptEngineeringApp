using System.Text.RegularExpressions;

namespace Ai.CvReviewer.Tests.Services;

public interface IReadabilityAnalyzer
{
    ReadabilityAnalysis Analyze(string text);
}

public record ReadabilityAnalysis(int SentenceCount, int WordCount, bool HasVerbs, int AverageSentenceLength);

public class SimpleReadabilityAnalyzer : IReadabilityAnalyzer
{
    public ReadabilityAnalysis Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ReadabilityAnalysis(0, 0, false, 0);

        // Count sentences (simple approach)
        var sentences = Regex.Split(text, @"[.!?]+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var sentenceCount = sentences.Length;

        // Count words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        // Check for verbs (basic approach)
        var commonVerbs = new[] { "is", "are", "was", "were", "has", "have", "had", "does", "do", "did", "can", "could", "will", "would", "should", "may", "might", "must" };
        var hasVerbs = commonVerbs.Any(verb => text.Contains(verb, StringComparison.OrdinalIgnoreCase));

        // Average sentence length
        var averageSentenceLength = sentenceCount > 0 ? wordCount / sentenceCount : 0;

        return new ReadabilityAnalysis(sentenceCount, wordCount, hasVerbs, averageSentenceLength);
    }
}