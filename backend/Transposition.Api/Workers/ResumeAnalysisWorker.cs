using Transposition.Api.Models;
using Transposition.Api.Services;

namespace Transposition.Api.Workers;

/// <summary>
/// Long-running background service that listens for queued resume-analysis jobs
/// and processes them as soon as a worker thread is available.
///
/// Processing is event-driven: the worker waits on a semaphore that is signalled
/// each time a job is enqueued, avoiding busy-waiting.
/// </summary>
public class ResumeAnalysisWorker : BackgroundService
{
    private readonly IAnalysisJobQueue _queue;
    private readonly IResumeAnalysisService _analysisService;
    private readonly ILogger<ResumeAnalysisWorker> _logger;

    public ResumeAnalysisWorker(
        IAnalysisJobQueue queue,
        IResumeAnalysisService analysisService,
        ILogger<ResumeAnalysisWorker> logger)
    {
        _queue = queue;
        _analysisService = analysisService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ResumeAnalysisWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until a job is available (event-based, no busy-wait)
            await _queue.WorkAvailable.WaitAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            if (!_queue.TryDequeue(out var job) || job is null)
                continue;

            await ProcessJobAsync(job, stoppingToken);
        }

        _logger.LogInformation("ResumeAnalysisWorker stopped.");
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        job.Status = AnalysisJobStatus.Processing;
        _logger.LogInformation("Processing analysis job {JobId} for applicant '{Applicant}'.",
            job.Id, job.Resume.ApplicantName);

        try
        {
            // Offload CPU-bound work to a thread-pool thread so the event loop
            // remains responsive and does not block the ASP.NET request pipeline.
            var result = await Task.Run(
                () => _analysisService.Analyse(job.Resume, job.JobRole),
                cancellationToken);

            job.Result = result;
            job.Status = AnalysisJobStatus.Completed;

            _logger.LogInformation(
                "Job {JobId} completed. Overall match: {Match}%.",
                job.Id, result.OverallMatchPercentage);
        }
        catch (OperationCanceledException)
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorMessage = "Job was cancelled.";
            _logger.LogWarning("Job {JobId} was cancelled.", job.Id);
        }
        catch (Exception ex)
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Job {JobId} failed.", job.Id);
        }
    }
}
