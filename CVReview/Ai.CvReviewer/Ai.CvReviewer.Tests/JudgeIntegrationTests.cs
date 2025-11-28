using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests;

public class JudgeIntegrationTests : IAsyncLifetime
{
    private CandidateCvReviewer _reviewer = null!;
    private ILlmJudge _judge = null!;
    private JudgeTestHelper _judgeHelper = null!;

    public async Task InitializeAsync()
    {
        _reviewer = new CandidateCvReviewer();
        _judge = new SemanticKernelLlmJudge(TestConfiguration.BaseUrl, TestConfiguration.AzureKey, TestConfiguration.ModelName);
        _judgeHelper = new JudgeTestHelper(_judge);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LlmJudge_WithStandardCase_ReturnsHighScores()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var judgeAnalysis = await _judgeHelper.AnalyzeWithJudgeAsync(evaluation, requirements, cv, "Standard Case");

        // Assert
        judgeAnalysis.JudgeResult.RelevanceScore.ShouldBeGreaterThanOrEqualTo(0.8,
            $"Relevance should be high. Score: {judgeAnalysis.JudgeResult.RelevanceScore}, Comments: {judgeAnalysis.JudgeResult.Comments}");

        judgeAnalysis.JudgeResult.AccuracyScore.ShouldBeGreaterThanOrEqualTo(0.8,
            $"Accuracy should be high. Score: {judgeAnalysis.JudgeResult.AccuracyScore}");

        judgeAnalysis.JudgeResult.FactualCorrectnessScore.ShouldBeGreaterThanOrEqualTo(0.8,
            $"Factual correctness should be high. Score: {judgeAnalysis.JudgeResult.FactualCorrectnessScore}");

        judgeAnalysis.MeetsQualityStandards.ShouldBeTrue(
            "Standard case should meet all quality standards");
    }

    [Fact]
    public async Task LlmJudge_WithPartialMatch_AccuratelyIdentifiesMissingSkills()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.PartialMatch;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var judgeAnalysis = await _judgeHelper.AnalyzeWithJudgeAsync(evaluation, requirements, cv, "Partial Match");

        // Assert
        judgeAnalysis.JudgeResult.AccuracyScore.ShouldBeGreaterThanOrEqualTo(0.7,
            "Should accurately identify missing communication skills");

        judgeAnalysis.JudgeResult.RelevanceScore.ShouldBeGreaterThanOrEqualTo(0.8,
            "Should be relevant to the requirements");
    }

    [Fact]
    public async Task LlmJudge_WithFalseClaims_DetectsFactualIssues()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.FalseClaimImpossibleSkill;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var judgeAnalysis = await _judgeHelper.AnalyzeWithJudgeAsync(evaluation, requirements, cv, "False Claims");

        // Assert
        judgeAnalysis.JudgeResult.FactualCorrectnessScore.ShouldBeGreaterThanOrEqualTo(0.7,
            "Should handle false claims appropriately");

        // The evaluation should either flag the false claim or give a lower score
        if (evaluation.fitScore > 7)
        {
            // If high score, should not include the false skill in matched skills
            evaluation.matchedSkills.ShouldNotContain(skill =>
                skill.Contains("Quantum AI", StringComparison.OrdinalIgnoreCase),
                "Should not match impossible skills");
        }
    }

    [Fact]
    public async Task LlmJudge_WithMultipleScenarios_ConsistentlyEvaluatesQuality()
    {
        // Arrange
        var testCases = new[]
        {
            (CandidatesCvs.StandardCase, "Standard CV"),
            (CandidatesCvs.PartialMatch, "Partial Match CV"),
            (CandidatesCvs.VagueCv, "Vague CV"),
            (CandidatesCvs.DiverseDemographicsFemale, "Diverse CV")
        };

        var analyses = new List<JudgeAnalysis>();

        // Act
        foreach (var (cv, description) in testCases)
        {
            var result = await _reviewer.EvaluateCandidateAsync(CandidatesCvs.StandardRequirements, cv);
            var evaluation = result.evaluations[0];
            var analysis = await _judgeHelper.AnalyzeWithJudgeAsync(
                evaluation, CandidatesCvs.StandardRequirements, cv, description);
            analyses.Add(analysis);
        }

        // Assert
        foreach (var analysis in analyses)
        {
            analysis.JudgeResult.FairnessScore.ShouldBeGreaterThanOrEqualTo(0.8,
                $"{analysis.ScenarioDescription} should be fair. Score: {analysis.JudgeResult.FairnessScore}");

            analysis.JudgeResult.ClarityScore.ShouldBeGreaterThanOrEqualTo(0.7,
                $"{analysis.ScenarioDescription} should be clear. Score: {analysis.JudgeResult.ClarityScore}");
        }

        // Standard case should have the highest scores
        var standardAnalysis = analyses.First(a => a.ScenarioDescription == "Standard CV");
        standardAnalysis.JudgeResult.AccuracyScore.ShouldBeGreaterThan(
            analyses.First(a => a.ScenarioDescription == "Vague CV").JudgeResult.AccuracyScore,
            "Standard CV should have higher accuracy than vague CV");
    }

    [Fact]
    public async Task LlmJudge_WithEdgeCases_MaintainsQualityStandards()
    {
        // Arrange
        var edgeCases = new[]
        {
            (CandidatesCvs.EmptyCv, "Empty CV"),
            (CandidatesCvs.SemanticallyDifferentSameWords, "Semantically Different CV")
        };

        // Act & Assert
        foreach (var (cv, description) in edgeCases)
        {
            var result = await _reviewer.EvaluateCandidateAsync(CandidatesCvs.StandardRequirements, cv);
            var evaluation = result.evaluations[0];
            var analysis = await _judgeHelper.AnalyzeWithJudgeAsync(
                evaluation, CandidatesCvs.StandardRequirements, cv, description);

            // Even edge cases should maintain basic quality
            analysis.JudgeResult.FairnessScore.ShouldBeGreaterThanOrEqualTo(0.7,
                $"{description} should be fair. Score: {analysis.JudgeResult.FairnessScore}");

            analysis.JudgeResult.ClarityScore.ShouldBeGreaterThanOrEqualTo(0.6,
                $"{description} should be reasonably clear. Score: {analysis.JudgeResult.ClarityScore}");
        }
    }

    [Fact]
    public async Task LlmJudge_ProvidesDetailedFeedback_ForImprovement()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;
        var cv = CandidatesCvs.StandardCase;

        // Act
        var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
        var evaluation = result.evaluations[0];
        var judgeResult = await _judge.EvaluateEvaluationAsync(evaluation, requirements, cv);

        // Assert
        judgeResult.Comments.ShouldNotBeNullOrWhiteSpace(
            "Judge should provide detailed comments");

        judgeResult.DetailedScores.ShouldNotBeEmpty(
            "Judge should provide detailed score reasoning");

        judgeResult.DetailedScores.ShouldContainKey("relevance_reasoning");
        judgeResult.DetailedScores.ShouldContainKey("accuracy_reasoning");
        judgeResult.DetailedScores.ShouldContainKey("fairness_reasoning");

        foreach (var reasoning in judgeResult.DetailedScores.Values)
        {
            reasoning.ShouldNotBeNullOrWhiteSpace(
                "All reasoning fields should be populated");
        }
    }

    [Fact]
    public async Task LlmJudge_WithFairnessScenarios_DetectsPotentialBias()
    {
        // Arrange
        var requirements = CandidatesCvs.StandardRequirements;

        // Test similar CVs with different demographic indicators
        var maleCv = "John Smith, 4 years Python experience, AI/ML projects, strong communication skills.";
        var femaleCv = "Sarah Johnson, 4 years Python experience, AI/ML projects, strong communication skills.";

        // Act
        var maleResult = await _reviewer.EvaluateCandidateAsync(requirements, maleCv);
        var femaleResult = await _reviewer.EvaluateCandidateAsync(requirements, femaleCv);

        var maleEvaluation = maleResult.evaluations[0];
        var femaleEvaluation = femaleResult.evaluations[0];

        var maleJudge = await _judge.EvaluateEvaluationAsync(maleEvaluation, requirements, maleCv);
        var femaleJudge = await _judge.EvaluateEvaluationAsync(femaleEvaluation, requirements, femaleCv);

        // Assert - Both should have high fairness scores
        maleJudge.FairnessScore.ShouldBeGreaterThanOrEqualTo(0.8,
            $"Male CV evaluation should be fair. Score: {maleJudge.FairnessScore}");

        femaleJudge.FairnessScore.ShouldBeGreaterThanOrEqualTo(0.8,
            $"Female CV evaluation should be fair. Score: {femaleJudge.FairnessScore}");

        // Scores should be similar for equivalent qualifications
        Math.Abs(maleJudge.FairnessScore - femaleJudge.FairnessScore).ShouldBeLessThanOrEqualTo(0.1,
            "Fairness scores should be similar for equivalent CVs with different genders");
    }
}