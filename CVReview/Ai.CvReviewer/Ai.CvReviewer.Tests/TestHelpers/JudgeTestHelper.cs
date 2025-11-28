using Ai.CvReviewer.Tests.Services;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class JudgeTestHelper
{
    private readonly ILlmJudge _judge;

    public JudgeTestHelper(ILlmJudge judge)
    {
        _judge = judge;
    }

    public async Task<JudgeAnalysis> AnalyzeWithJudgeAsync(
        CandidateEvaluation evaluation,
        string requirements,
        string cv,
        string scenarioDescription)
    {
        var judgeResult = await _judge.EvaluateEvaluationAsync(evaluation, requirements, cv);

        return new JudgeAnalysis(
            ScenarioDescription: scenarioDescription,
            JudgeResult: judgeResult,
            MeetsQualityStandards: judgeResult.RelevanceScore >= 0.8 &&
                                 judgeResult.AccuracyScore >= 0.8 &&
                                 judgeResult.FairnessScore >= 0.8 &&
                                 judgeResult.ClarityScore >= 0.7 &&
                                 judgeResult.FactualCorrectnessScore >= 0.8
        );
    }

    public async Task<List<JudgeAnalysis>> AnalyzeMultipleEvaluationsAsync(
        List<(CandidateEvaluation Evaluation, string Requirements, string Cv, string Description)> testCases)
    {
        var analyses = new List<JudgeAnalysis>();

        foreach (var (evaluation, requirements, cv, description) in testCases)
        {
            var analysis = await AnalyzeWithJudgeAsync(evaluation, requirements, cv, description);
            analyses.Add(analysis);
        }

        return analyses;
    }
}

public record JudgeAnalysis(
    string ScenarioDescription,
    JudgeResult JudgeResult,
    bool MeetsQualityStandards
);