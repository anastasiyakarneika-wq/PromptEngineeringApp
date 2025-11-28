using Ai.CvReviewer.Services;
using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class ErrorHandlingTestHelper
{
    private readonly IToxicityAnalyzer _toxicityAnalyzer;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;

    public ErrorHandlingTestHelper(IToxicityAnalyzer toxicityAnalyzer, ISentimentAnalyzer sentimentAnalyzer)
    {
        _toxicityAnalyzer = toxicityAnalyzer;
        _sentimentAnalyzer = sentimentAnalyzer;
    }

    public async Task<ErrorHandlingAnalysis> AnalyzeErrorScenarioAsync(
        CandidateEvaluation evaluation,
        string scenarioDescription)
    {
        var toxicityScore = await _toxicityAnalyzer.GetToxicityScoreAsync(evaluation.explanation);
        var sentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync(evaluation.explanation);

        return new ErrorHandlingAnalysis(
            ScenarioDescription: scenarioDescription,
            ToxicityScore: toxicityScore,
            Sentiment: sentiment,
            HasValidStructure: evaluation.fitScore >= 1 && evaluation.fitScore <= 10,
            HasNonEmptyExplanation: !string.IsNullOrWhiteSpace(evaluation.explanation),
            HasReasonableScore: evaluation.fitScore >= 1 // Even in error cases, score should be at least 1
        );
    }
}

public record ErrorHandlingAnalysis(
    string ScenarioDescription,
    float ToxicityScore,
    SentimentResult Sentiment,
    bool HasValidStructure,
    bool HasNonEmptyExplanation,
    bool HasReasonableScore
);