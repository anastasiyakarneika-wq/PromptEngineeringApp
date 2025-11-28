using Ai.CvReviewer.Services;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class RobustnessTestHelper
{
    private readonly CandidateCvReviewer _reviewer;
    private readonly IEmbeddingProvider _embeddingProvider;

    public RobustnessTestHelper(CandidateCvReviewer reviewer, IEmbeddingProvider embeddingProvider)
    {
        _reviewer = reviewer;
        _embeddingProvider = embeddingProvider;
    }

    public async Task<List<CandidateEvaluation>> GenerateVariationEvaluationsAsync(
        string baseRequirements,
        string baseCv,
        int numberOfVariations = 3)
    {
        var evaluations = new List<CandidateEvaluation>();

        // Test with original inputs
        var originalResult = await _reviewer.EvaluateCandidateAsync(baseRequirements, baseCv);
        evaluations.AddRange(originalResult.evaluations);

        // Create requirement variations
        var requirementVariations = new[]
        {
            baseRequirements,
            $"Looking for a {baseRequirements.ToLower()}",
            $"Position requires: {baseRequirements}",
            $".NET {baseRequirements.Replace("Python", "C#")}" // Different but related technology
        };

        // Test with requirement variations
        foreach (var variation in requirementVariations.Take(numberOfVariations))
        {
            var result = await _reviewer.EvaluateCandidateAsync(variation, baseCv);
            evaluations.AddRange(result.evaluations);
        }

        return evaluations;
    }

    public async Task<float> CalculateExplanationSimilarityAsync(
        CandidateEvaluation eval1,
        CandidateEvaluation eval2)
    {
        var embedding1 = await _embeddingProvider.GetEmbeddingAsync(eval1.explanation);
        var embedding2 = await _embeddingProvider.GetEmbeddingAsync(eval2.explanation);

        return _embeddingProvider.CalculateSimilarity(embedding1, embedding2);
    }

    public (float minScore, float maxScore, float variance) CalculateScoreStatistics(
        List<CandidateEvaluation> evaluations)
    {
        var scores = evaluations.Select(e => e.fitScore).ToList();
        var minScore = scores.Min();
        var maxScore = scores.Max();
        var average = scores.Average();
        var variance = scores.Select(s => Math.Pow(s - average, 2)).Average();

        return (minScore, maxScore, (float)variance);
    }
}