using Ai.CvReviewer;
using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests
{
    public class OutputQualityTests : IAsyncLifetime
    {
        private CandidateCvReviewer _reviewer = null!;
        private IGrammarChecker _grammarChecker = null!;
        private IReadabilityAnalyzer _readabilityAnalyzer = null!;
        private IHallucinationDetector _hallucinationDetector = null!;
        private OutputQualityTestHelper _qualityHelper = null!;

        public async Task InitializeAsync()
        {
            _reviewer = new CandidateCvReviewer();
            _grammarChecker = new LanguageToolChecker();
            _readabilityAnalyzer = new SimpleReadabilityAnalyzer();
            _hallucinationDetector = new SimpleHallucinationDetector();
            _qualityHelper = new OutputQualityTestHelper(
            _grammarChecker, _readabilityAnalyzer, _hallucinationDetector);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task EvaluateCandidateAsync_WithStandardCase_ReturnsValidEvaluation()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.StandardCase;

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);

            // Assert
            result.ShouldNotBeNull();
            result.evaluations.ShouldNotBeNull();
            result.evaluations.ShouldNotBeEmpty();

            var evaluation = result.evaluations[0];
            evaluation.fitScore.ShouldBeInRange(1, 10);
            evaluation.explanation.ShouldNotBeNullOrWhiteSpace();
            evaluation.matchedSkills.ShouldNotBeNull();
            evaluation.missingSkills.ShouldNotBeNull();
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithStandardCase_ReturnsGrammaticallyCorrectExplanation()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.StandardCase;

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];
            var quality = await _qualityHelper.AnalyzeQualityAsync(evaluation, requirements, cv);

            // Assert
            quality.GrammarErrorCount.ShouldBe(0,
                $"Explanation should have 0 grammar errors. Found {quality.GrammarErrorCount} errors: {evaluation.explanation}");
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithStandardCase_ReturnsReadableExplanation()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.StandardCase;

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];
            var quality = await _qualityHelper.AnalyzeQualityAsync(evaluation, requirements, cv);

            // Assert
            quality.Readability.SentenceCount.ShouldBeLessThanOrEqualTo(3,
                $"Explanation should have ≤ 3 sentences. Found {quality.Readability.SentenceCount}: {evaluation.explanation}");

            quality.Readability.HasVerbs.ShouldBeTrue(
                $"Explanation should contain at least one verb: {evaluation.explanation}");
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithStandardCase_ReturnsAccurateSkillMatching()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.StandardCase;

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];

            // Assert - CV explicitly mentions all required skills
            evaluation.matchedSkills.ShouldContain(skill =>
                skill.Contains("Python", StringComparison.OrdinalIgnoreCase));
            evaluation.matchedSkills.ShouldContain(skill =>
                skill.Contains("AI", StringComparison.OrdinalIgnoreCase) ||
                skill.Contains("ML", StringComparison.OrdinalIgnoreCase));
            evaluation.matchedSkills.ShouldContain(skill =>
                skill.Contains("communication", StringComparison.OrdinalIgnoreCase));

            evaluation.missingSkills.ShouldBeEmpty(
                $"Should have no missing skills for fully qualified candidate. Missing: {string.Join(", ", evaluation.missingSkills)}");
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithPartialMatch_ReturnsCorrectMissingSkills()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.PartialMatch; // Missing explicit communication skills

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];

            // Assert
            evaluation.missingSkills.ShouldContain(skill =>
                skill.Contains("communication", StringComparison.OrdinalIgnoreCase),
                "Should detect missing communication skills");

            evaluation.matchedSkills.ShouldContain(skill =>
                skill.Contains("Python", StringComparison.OrdinalIgnoreCase));
            evaluation.matchedSkills.ShouldContain(skill =>
                skill.Contains("AI", StringComparison.OrdinalIgnoreCase) ||
                skill.Contains("ML", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithFalseClaims_DetectsNoHallucinations()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.FalseClaimImpossibleSkill; // Contains "Quantum AI"

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];
            var quality = await _qualityHelper.AnalyzeQualityAsync(evaluation, requirements, cv);

            // Assert
            quality.HallucinatedSkills.ShouldBeEmpty(
                $"Should not hallucinate skills. Found: {string.Join(", ", quality.HallucinatedSkills)}");
        }

        [Theory]
        [MemberData(nameof(OutputQualityTestData.ValidScoreRangeTestCases), MemberType = typeof(OutputQualityTestData))]
        public async Task EvaluateCandidateAsync_WithVariousInputs_ReturnsValidScoreRange(string cv, string description)
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];
            var quality = await _qualityHelper.AnalyzeQualityAsync(evaluation, requirements, cv);

            // Assert
            quality.HasValidScore.ShouldBeTrue(
                $"{description} should return valid score between 1-10. Got: {evaluation.fitScore}");
        }

        [Fact]
        public async Task EvaluateCandidateAsync_WithExaggeratedClaims_HandlesAppropriately()
        {
            // Arrange
            var requirements = CandidatesCvs.StandardRequirements;
            var cv = CandidatesCvs.FalseClaimExaggerated; // Claims 6 years of LLM testing

            // Act
            var result = await _reviewer.EvaluateCandidateAsync(requirements, cv);
            var evaluation = result.evaluations[0];

            // Assert - should not blindly accept exaggerated claims
            evaluation.fitScore.ShouldBeLessThan(10,
                "Should not give perfect score for CV with potentially exaggerated claims");

            // Either the skill should be missing or the score should reflect skepticism
            if (!evaluation.missingSkills.Any(s => s.Contains("LLM", StringComparison.OrdinalIgnoreCase)))
            {
                evaluation.fitScore.ShouldBeLessThan(9,
                    "Score should reflect potential skepticism of exaggerated experience");
            }
        }

    }
    public static class OutputQualityTestData
    {
        public static IEnumerable<object[]> ValidScoreRangeTestCases()
        {
            yield return new object[] { CandidatesCvs.StandardCase, "Standard CV" };
            yield return new object[] { CandidatesCvs.VagueCv, "Vague CV" };
            yield return new object[] { CandidatesCvs.EmptyCv, "Empty CV" };
            yield return new object[] { CandidatesCvs.SemanticallyDifferentSameWords, "Semantically Different CV" };
            yield return new object[] { CandidatesCvs.PartialMatch, "Partial Match CV" };
            yield return new object[] { CandidatesCvs.DiverseDemographicsFemale, "Diverse Demographics CV" };
        }
    }
}