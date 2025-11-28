using System.Diagnostics;

namespace Ai.CvReviewer.Tests.TestHelpers;

public class PerformanceTestHelper
{
    private readonly CandidateCvReviewer _reviewer;

    public PerformanceTestHelper(CandidateCvReviewer reviewer)
    {
        _reviewer = reviewer;
    }

    public async Task<PerformanceResult> MeasureEvaluationTimeAsync(
        string requirements,
        string cv,
        int numberOfRuns = 1)
    {
        var executionTimes = new List<TimeSpan>();

        for (int i = 0; i < numberOfRuns; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            await _reviewer.EvaluateCandidateAsync(requirements, cv);

            stopwatch.Stop();
            executionTimes.Add(stopwatch.Elapsed);
        }

        return new PerformanceResult(executionTimes);
    }

    public async Task<BatchPerformanceResult> MeasureBatchEvaluationTimeAsync(
        string requirements,
        string[] cvs,
        bool parallel = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();

        if (parallel)
        {
            // Execute evaluations in parallel
            foreach (var cv in cvs)
            {
                tasks.Add(_reviewer.EvaluateCandidateAsync(requirements, cv));
            }
            await Task.WhenAll(tasks);
        }
        else
        {
            // Execute evaluations sequentially
            foreach (var cv in cvs)
            {
                await _reviewer.EvaluateCandidateAsync(requirements, cv);
            }
        }

        stopwatch.Stop();

        return new BatchPerformanceResult(stopwatch.Elapsed, cvs.Length, parallel);
    }
}

public record PerformanceResult(List<TimeSpan> ExecutionTimes)
{
    public TimeSpan AverageTime => TimeSpan.FromMilliseconds(
        ExecutionTimes.Average(ts => ts.TotalMilliseconds));

    public TimeSpan MinTime => TimeSpan.FromMilliseconds(
        ExecutionTimes.Min(ts => ts.TotalMilliseconds));

    public TimeSpan MaxTime => TimeSpan.FromMilliseconds(
        ExecutionTimes.Max(ts => ts.TotalMilliseconds));

    public TimeSpan MedianTime
    {
        get
        {
            var sortedTimes = ExecutionTimes.OrderBy(ts => ts).ToList();
            return sortedTimes[sortedTimes.Count / 2];
        }
    }
}

public record BatchPerformanceResult(TimeSpan TotalTime, int NumberOfEvaluations, bool WasParallel)
{
    public TimeSpan AverageTimePerEvaluation =>
        TimeSpan.FromMilliseconds(TotalTime.TotalMilliseconds / NumberOfEvaluations);

    public double EvaluationsPerSecond =>
        NumberOfEvaluations / TotalTime.TotalSeconds;
}