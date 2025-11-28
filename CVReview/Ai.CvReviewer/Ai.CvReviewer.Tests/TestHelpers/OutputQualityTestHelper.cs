using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class OutputQualityTestHelper
{
    private readonly IGrammarChecker _grammarChecker;
    private readonly IReadabilityAnalyzer _readabilityAnalyzer;
    private readonly IHallucinationDetector _hallucinationDetector;

    public OutputQualityTestHelper(
        IGrammarChecker grammarChecker,
        IReadabilityAnalyzer readabilityAnalyzer,
        IHallucinationDetector hallucinationDetector)
    {
        _grammarChecker = grammarChecker;
        _readabilityAnalyzer = readabilityAnalyzer;
        _hallucinationDetector = hallucinationDetector;
    }

    public async Task<OutputQualityAnalysis> AnalyzeQualityAsync(
        CandidateEvaluation evaluation,
        string requirements,
        string cv)
    {
        var grammarErrors = await _grammarChecker.GetGrammarErrorCountAsync(evaluation.explanation);
        var readability = _readabilityAnalyzer.Analyze(evaluation.explanation);
        var hallucinatedSkills = await _hallucinationDetector.DetectHallucinatedSkillsAsync(
            evaluation.matchedSkills, evaluation.missingSkills, requirements, cv);

        return new OutputQualityAnalysis(
            GrammarErrorCount: grammarErrors,
            Readability: readability,
            HallucinatedSkills: hallucinatedSkills,
            HasValidScore: evaluation.fitScore >= 1 && evaluation.fitScore <= 10,
            ExplanationLength: evaluation.explanation.Length
        );
    }
}

public record OutputQualityAnalysis(
    int GrammarErrorCount,
    ReadabilityAnalysis Readability,
    List<string> HallucinatedSkills,
    bool HasValidScore,
    int ExplanationLength
);