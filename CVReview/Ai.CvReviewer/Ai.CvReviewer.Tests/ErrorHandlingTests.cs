using Ai.CvReviewer.Services;
using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests;

public class ErrorHandlingTests : IAsyncLifetime
{
    private CandidateCvReviewer _reviewer = null!;
    private IToxicityAnalyzer _toxicityAnalyzer = null!;
    private ISentimentAnalyzer _sentimentAnalyzer = null!;
    private ErrorHandlingTestHelper _errorHelper = null!;

    public async Task InitializeAsync()
    {
        _reviewer = new CandidateCvReviewer();
        _toxicityAnalyzer = new HuggingFaceToxicityAnalyzer(TestConfiguration.HuggingFaceToken, TestConfiguration.HugginFaceUrl);
        _sentimentAnalyzer = new HuggingFaceSentimentAnalyzer(TestConfiguration.HuggingFaceToken, TestConfiguration.HugginFaceUrl);
        _errorHelper = new ErrorHandlingTestHelper(_toxicityAnalyzer, _sentimentAnalyzer);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EvaluateCandidateAsync_WithEmptyCv_ReturnsNonToxicAndPositiveEvaluation()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.EmptyCv;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Empty CV");

        // Assert
        analysis.ToxicityScore.ShouldBeLessThan(0.2f,
            $"Empty CV evaluation should be non-toxic. Toxicity score: {analysis.ToxicityScore}, Explanation: {evaluation.explanation}");

        analysis.Sentiment.Score.ShouldBeGreaterThan(0.3f,
            $"Empty CV evaluation should have positive sentiment. Score: {analysis.Sentiment.Score}, Label: {analysis.Sentiment.Label}");

        analysis.HasValidStructure.ShouldBeTrue("Should return valid evaluation structure");
        analysis.HasNonEmptyExplanation.ShouldBeTrue("Should provide an explanation even for empty CV");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithVagueCv_ReturnsFairAndConstructiveEvaluation()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.VagueCv;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Vague CV");

        // Assert
        analysis.ToxicityScore.ShouldBeLessThan(0.2f,
            $"Vague CV evaluation should be non-toxic. Toxicity score: {analysis.ToxicityScore}");

        analysis.Sentiment.Score.ShouldBeGreaterThan(0.3f,
            $"Vague CV evaluation should have positive sentiment. Score: {analysis.Sentiment.Score}");

        evaluation.missingSkills.ShouldNotBeNull("Should identify missing skills");
        evaluation.missingSkills.ShouldNotBeEmpty("Vague CV should have missing skills identified");

        analysis.HasReasonableScore.ShouldBeTrue("Should assign a reasonable score (>= 1)");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithEmptyRequirements_ReturnsValidEvaluation()
    {
        // Arrange
        var requirements = "";
        var cv = CandidatesCvs.StandardCase;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Empty Requirements");

        // Assert
        analysis.HasValidStructure.ShouldBeTrue("Should return valid evaluation structure");
        analysis.HasNonEmptyExplanation.ShouldBeTrue("Should provide an explanation");
        analysis.ToxicityScore.ShouldBeLessThan(0.2f, "Should be non-toxic");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithBothInputsEmpty_ReturnsFallbackEvaluation()
    {
        // Arrange
        var requirements = "";
        var cv = "";

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Both Inputs Empty");

        // Assert
        analysis.HasValidStructure.ShouldBeTrue("Should return valid evaluation structure");
        evaluation.explanation.ShouldNotBeNullOrWhiteSpace("Should provide fallback explanation");
        analysis.ToxicityScore.ShouldBeLessThan(0.2f, "Fallback should be non-toxic");
        analysis.Sentiment.Score.ShouldBeGreaterThan(0.3f, "Fallback should have positive sentiment");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithVeryLongInputs_HandlesGracefully()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var longCv = string.Join(" ", Enumerable.Repeat(CandidatesCvs.StandardCase, 10)); // Very long CV

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, longCv);
        var evaluation = result.evaluations[0];

        // Assert
        evaluation.ShouldNotBeNull("Should handle very long CV without crashing");
        evaluation.fitScore.ShouldBeInRange(1, 10, "Should return valid score range");
        evaluation.explanation.ShouldNotBeNullOrWhiteSpace("Should provide explanation");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var requirements = "Software Engineer with 3+ years in C# && .NET, experience in AI/ML 🔥";
        var cv = "John Doe, 4 years C# experience at TechCorp ⭐, worked on AI projects 🚀";

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Special Characters");

        // Assert
        analysis.HasValidStructure.ShouldBeTrue("Should handle special characters correctly");
        analysis.ToxicityScore.ShouldBeLessThan(0.2f, "Should be non-toxic despite special chars");
        evaluation.explanation.ShouldNotBeNullOrWhiteSpace("Should provide valid explanation");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithIncompleteRequirements_ReturnsReasonableEvaluation()
    {
        // Arrange
        var requirements = "Software Engineer"; // Very incomplete
        var cv = CandidatesCvs.StandardCase;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];

        // Assert
        evaluation.ShouldNotBeNull("Should handle incomplete requirements");
        evaluation.fitScore.ShouldBeInRange(1, 10, "Should return valid score");
        evaluation.explanation.ShouldNotBeNullOrWhiteSpace("Should provide explanation");

        // With incomplete requirements, we might not have clear missing skills
        // But the structure should still be valid
        evaluation.matchedSkills.ShouldNotBeNull();
        evaluation.missingSkills.ShouldNotBeNull();
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithNonsenseInputs_ReturnsNonToxicEvaluation()
    {
        // Arrange
        var requirements = "asdf jkl; qwerty 12345 !@#$%";
        var cv = "random text lorem ipsum nonsense data";

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var analysis = await _errorHelper.AnalyzeErrorScenarioAsync(evaluation, "Nonsense Inputs");

        // Assert
        analysis.ToxicityScore.ShouldBeLessThan(0.2f,
            $"Should handle nonsense inputs without toxicity. Score: {analysis.ToxicityScore}");

        analysis.Sentiment.Score.ShouldBeGreaterThan(0.3f,
            "Should maintain positive sentiment even with nonsense inputs");

        analysis.HasValidStructure.ShouldBeTrue("Should return valid structure");
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithMultipleEdgeCases_AllReturnValidStructures()
    {
        // Arrange
        var edgeCases = new[]
        {
            (CandidatesCvs.EmptyCv, "Empty CV"),
            (CandidatesCvs.VagueCv, "Vague CV"),
            ("", "Empty Requirements"),
            ("   ", "Whitespace Requirements"),
            ("Very short", "Very Short Requirements")
        };

        foreach (var (input, description) in edgeCases)
        {
            // Act
            var result = await _reviewer.EvaluateCandidateAsync(CandidatesCvs.StandardRequirements, input);
            var evaluation = result.evaluations[0];

            // Assert
            evaluation.ShouldNotBeNull($"{description} should return non-null evaluation");
            evaluation.fitScore.ShouldBeInRange(1, 10, $"{description} should have valid score");
            evaluation.explanation.ShouldNotBeNullOrWhiteSpace($"{description} should have explanation");
            evaluation.matchedSkills.ShouldNotBeNull($"{description} should have matched skills list");
            evaluation.missingSkills.ShouldNotBeNull($"{description} should have missing skills list");
        }
    }

    [Fact]
    public async Task EvaluateCandidateAsync_WithEmptyCv_ReturnsFallbackEvaluation()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.EmptyCv;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);

        // Assert
        result.ShouldNotBeNull();
        result.evaluations.ShouldNotBeEmpty();

        var evaluation = result.evaluations[0];
        evaluation.fitScore.ShouldBe(5); // Fallback score from implementation
        evaluation.explanation.ShouldContain("Fallback");
    }
}
/*namespace Ai.CvReviewer.Tests
{
    public class ErrorHandlingTests : TestBase
    {
        [Fact]
        public async Task EvaluateCandidateAsync_WithEmptyCv_ReturnsFallbackEvaluation()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.EmptyCv;

            // Act
            var result = await Reviewer.EvaluateCandidateAsync(requirements, cv);

            // Assert
            result.ShouldNotBeNull();
            result.evaluations.ShouldNotBeEmpty();

            var evaluation = result.evaluations[0];
            evaluation.fitScore.ShouldBe(5); // Fallback score from implementation
            evaluation.explanation.ShouldContain("Fallback");
        }
    }
}
*/