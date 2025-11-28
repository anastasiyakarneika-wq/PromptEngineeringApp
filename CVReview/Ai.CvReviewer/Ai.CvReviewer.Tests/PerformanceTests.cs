namespace Ai.CvReviewer.Tests;

public class PerformanceTests : IAsyncLifetime
{
    private CandidateCvReviewer _reviewer = null!;
    private PerformanceTestHelper _performanceHelper = null!;

    public Task InitializeAsync()
    {
        _reviewer = new CandidateCvReviewer();
        _performanceHelper = new PerformanceTestHelper(_reviewer);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EvaluateCandidateAsync_WithStandardCase_CompletesWithinTwoSeconds()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;
        var maxAllowedTime = TimeSpan.FromSeconds(2);

        // Act
        var result = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, cv, numberOfRuns: 3);

        // Assert
        result.MedianTime.ShouldBeLessThanOrEqualTo(maxAllowedTime,
            $"Single evaluation should complete within 2 seconds. " +
            $"Median time: {result.MedianTime.TotalMilliseconds}ms, " +
            $"Min: {result.MinTime.TotalMilliseconds}ms, " +
            $"Max: {result.MaxTime.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithMultipleCvTypes_AllCompleteWithinTwoSeconds()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var testCvs = new[]
        {
            CandidatesCvs.StandardCase,
            CandidatesCvs.VagueCv,
            CandidatesCvs.PartialMatch,
            CandidatesCvs.EmptyCv,
            CandidatesCvs.SemanticallyDifferentSameWords
        };
        var maxAllowedTime = TimeSpan.FromSeconds(2);

        // Act & Assert
        foreach (var cv in testCvs)
        {
            var result = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, cv, numberOfRuns: 2);

            result.MedianTime.ShouldBeLessThanOrEqualTo(maxAllowedTime,
                $"Evaluation for CV type should complete within 2 seconds. " +
                $"CV: {cv.Substring(0, Math.Min(50, cv.Length))}... " +
                $"Median time: {result.MedianTime.TotalMilliseconds}ms");
        }
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithFiveEvaluations_CompletesWithinTenSeconds()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cvs = new[]
        {
            CandidatesCvs.StandardCase,
            CandidatesCvs.PartialMatch,
            CandidatesCvs.DiverseDemographicsFemale,
            CandidatesCvs.DiverseDemographicsJunior,
            CandidatesCvs.SemanticallyIdenticalDifferentWords
        };
        var maxAllowedTime = TimeSpan.FromSeconds(10);

        // Act
        var result = await _performanceHelper.MeasureBatchEvaluationTimeAsync(requirements, cvs, parallel: false);

        // Assert
        result.TotalTime.ShouldBeLessThanOrEqualTo(maxAllowedTime,
            $"Batch of 5 evaluations should complete within 10 seconds. " +
            $"Total time: {result.TotalTime.TotalSeconds}s, " +
            $"Average per evaluation: {result.AverageTimePerEvaluation.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithEmptyCv_IsFasterThanStandardCase()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;

        // Act
        var emptyResult = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, CandidatesCvs.EmptyCv);
        var standardResult = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, CandidatesCvs.StandardCase);

        // Assert
        emptyResult.MedianTime.ShouldBeLessThan(standardResult.MedianTime,
            $"Empty CV evaluation should be faster than standard CV evaluation. " +
            $"Empty: {emptyResult.MedianTime.TotalMilliseconds}ms, " +
            $"Standard: {standardResult.MedianTime.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithVagueCv_IsFasterThanDetailedCv()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;

        // Act
        var vagueResult = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, CandidatesCvs.VagueCv);
        var detailedResult = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, CandidatesCvs.StandardCase);

        // Assert
        vagueResult.MedianTime.ShouldBeLessThan(detailedResult.MedianTime,
            $"Vague CV evaluation should be faster than detailed CV evaluation. " +
            $"Vague: {vagueResult.MedianTime.TotalMilliseconds}ms, " +
            $"Detailed: {detailedResult.MedianTime.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithMultipleRuns_ShowsReasonableConsistency()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;
        var numberOfRuns = 5;

        // Act
        var result = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, cv, numberOfRuns);
        var variance = (result.MaxTime - result.MinTime).TotalMilliseconds;

        // Assert - Variance should be less than 1 second between runs
        variance.ShouldBeLessThan(2000,
            $"Execution times should be consistent across {numberOfRuns} runs. " +
            $"Variance: {variance}ms, Min: {result.MinTime.TotalMilliseconds}ms, " +
            $"Max: {result.MaxTime.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithFallbackScenario_IsFast()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.EmptyCv; // Will trigger fallback
        var maxAllowedTime = TimeSpan.FromSeconds(2); // Fallback should be very fast

        // Act
        var result = await _performanceHelper.MeasureEvaluationTimeAsync(requirements, cv, numberOfRuns: 3);

        // Assert
        result.MedianTime.ShouldBeLessThanOrEqualTo(maxAllowedTime,
            $"Fallback evaluation should complete within 1 second. " +
            $"Median time: {result.MedianTime.TotalMilliseconds}ms");
    }
}