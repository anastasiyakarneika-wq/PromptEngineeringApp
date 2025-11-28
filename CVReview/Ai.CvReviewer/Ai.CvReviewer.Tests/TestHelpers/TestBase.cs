using Ai.CvReviewer.Tests.TestData;

namespace Ai.CvReviewer.Tests.TestHelpers;

public abstract class TestBase : IAsyncLifetime
{
    protected CandidateCvReviewer Reviewer { get; private set; } = null!;
    protected string StandardRequirements => CandidatesCvs.StandardRequirements;

    public Task InitializeAsync()
    {
        Reviewer = new CandidateCvReviewer();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<EvaluationList> EvaluateStandardCandidateAsync(string cv)
    {
        return await Reviewer.EvaluateCandidateAsync(StandardRequirements, cv);
    }
}