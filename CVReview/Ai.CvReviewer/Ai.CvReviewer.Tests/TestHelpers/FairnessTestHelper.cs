using Ai.CvReviewer.Services;
using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class FairnessTestHelper
{
    private readonly IToxicityAnalyzer _toxicityAnalyzer;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly IStereotypeDetector _stereotypeDetector;

    public FairnessTestHelper(
        IToxicityAnalyzer toxicityAnalyzer,
        ISentimentAnalyzer sentimentAnalyzer,
        IStereotypeDetector stereotypeDetector)
    {
        _toxicityAnalyzer = toxicityAnalyzer;
        _sentimentAnalyzer = sentimentAnalyzer;
        _stereotypeDetector = stereotypeDetector;
    }

    public async Task<FairnessAnalysis> AnalyzeFairnessAsync(
        CandidateEvaluation evaluation,
        string demographicGroup)
    {
        var toxicityScore = await _toxicityAnalyzer.GetToxicityScoreAsync(evaluation.explanation);
        var sentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync(evaluation.explanation);
        var stereotypeAnalysis = await _stereotypeDetector.AnalyzeStereotypesAsync(evaluation.explanation);

        return new FairnessAnalysis(
            DemographicGroup: demographicGroup,
            FitScore: evaluation.fitScore,
            ToxicityScore: toxicityScore,
            Sentiment: sentiment,
            StereotypeAnalysis: stereotypeAnalysis,
            Explanation: evaluation.explanation,
            IsFair: toxicityScore < TestConfiguration.MaximumToxicityScore &&
                   sentiment.Score > TestConfiguration.MinimumSentimentScore &&
                   stereotypeAnalysis.StereotypeScore < TestConfiguration.MaximumStereotypeScore
        );
    }

    public async Task<DemographicComparison> CompareDemographicGroupsAsync(
        List<(CandidateEvaluation Evaluation, string Group)> evaluations)
    {
        var analyses = new List<FairnessAnalysis>();

        foreach (var (evaluation, group) in evaluations)
        {
            var analysis = await AnalyzeFairnessAsync(evaluation, group);
            analyses.Add(analysis);
        }

        var minScore = analyses.Min(a => a.FitScore);
        var maxScore = analyses.Max(a => a.FitScore);
        var scoreVariance = maxScore - minScore;

        return new DemographicComparison(
            Analyses: analyses,
            MinScore: minScore,
            MaxScore: maxScore,
            ScoreVariance: scoreVariance,
            IsFair: scoreVariance <= TestConfiguration.MaximumScoreVariance2 &&
                   analyses.All(a => a.IsFair)
        );
    }
}

public record FairnessAnalysis(
    string DemographicGroup,
    int FitScore,
    float ToxicityScore,
    SentimentResult Sentiment,
    StereotypeAnalysis StereotypeAnalysis,
    string Explanation,
    bool IsFair
);

public record DemographicComparison(
    List<FairnessAnalysis> Analyses,
    int MinScore,
    int MaxScore,
    int ScoreVariance,
    bool IsFair
);