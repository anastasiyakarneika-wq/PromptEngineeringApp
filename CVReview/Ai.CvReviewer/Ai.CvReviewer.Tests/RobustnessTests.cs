using Ai.CvReviewer.Services;

namespace Ai.CvReviewer.Tests;

public class RobustnessTests : IAsyncLifetime
{
    private CandidateCvReviewer _reviewer = null!;
    private IEmbeddingProvider _embeddingProvider = null!;
    private RobustnessTestHelper _robustnessHelper = null!;

    public async Task InitializeAsync()
    {
        _reviewer = new CandidateCvReviewer();
        _embeddingProvider = new AzuredEmbeddingProvider(TestConfiguration.BaseUrl);
        _robustnessHelper = new RobustnessTestHelper(_reviewer, _embeddingProvider);

        // Warm up the embedding provider
        await _embeddingProvider.GetEmbeddingAsync("test");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EvaluateCandidateAsync_WithSemanticallyIdenticalCvs_ReturnsConsistentEvaluations()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv1 = CandidatesCvs.StandardCase;
        var cv2 = CandidatesCvs.SemanticallyIdenticalDifferentWords;

        // Act
        var result1 = await _reviewer.EvaluateCandidateAsync(requirements, cv1);
        var result2 = await _reviewer.EvaluateCandidateAsync(requirements, cv2);

        var evaluation1 = result1.evaluations[0];
        var evaluation2 = result2.evaluations[0];

        var similarity = await _robustnessHelper.CalculateExplanationSimilarityAsync(evaluation1, evaluation2);

        // Assert
        similarity.ShouldBeGreaterThan(0.5f,
            $"Explanations should be semantically similar. Actual similarity: {similarity}");

        Math.Abs(evaluation1.fitScore - evaluation2.fitScore).ShouldBeLessThanOrEqualTo(2,
            $"Fit scores should be consistent. Score1: {evaluation1.fitScore}, Score2: {evaluation2.fitScore}");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithRequirementVariations_ReturnsConsistentScores()
    {
        // Arrange
        var baseRequirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;

        // Act
        var evaluations = await _robustnessHelper.GenerateVariationEvaluationsAsync(baseRequirements, cv, 3);
        var (minScore, maxScore, variance) = _robustnessHelper.CalculateScoreStatistics(evaluations);

        // Assert
        variance.ShouldBeLessThanOrEqualTo(4f, // variance of 4 allows for ±2 score difference
            $"Score variance should be <= 4. Actual variance: {variance}, Min: {minScore}, Max: {maxScore}");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithMultipleVariations_ReturnsSemanticallySimilarExplanations()
    {
        // Arrange
        var baseRequirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;

        // Act
        var evaluations = await _robustnessHelper.GenerateVariationEvaluationsAsync(baseRequirements, cv, 3);

        var similarities = new List<float>();
        for (int i = 0; i < evaluations.Count - 1; i++)
        {
            for (int j = i + 1; j < evaluations.Count; j++)
            {
                var similarity = await _robustnessHelper.CalculateExplanationSimilarityAsync(
                    evaluations[i], evaluations[j]);
                similarities.Add(similarity);
            }
        }

        var averageSimilarity = similarities.Average();

        // Assert
        averageSimilarity.ShouldBeGreaterThan(0.5f,
            $"Average explanation similarity should be > 0.5. Actual: {averageSimilarity}");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithSemanticallyDifferentCv_ReturnsAppropriateEvaluation()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.SemanticallyDifferentSameWords; // Python snakes vs Python programming

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];

        // Assert - should detect that this is about snakes, not programming
        evaluation.fitScore.ShouldBeLessThan(7,
            "Python snake researcher should have lower score than Python programmer");

        evaluation.missingSkills.ShouldContain(skill =>
            skill.Contains("Python") || skill.Contains("programming"),
            "Should detect missing Python programming skills");
    }
}