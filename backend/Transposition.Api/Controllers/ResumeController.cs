using Microsoft.AspNetCore.Mvc;
using Transposition.Api.Models;
using Transposition.Api.Services;

namespace Transposition.Api.Controllers;

/// <summary>
/// Accepts resume submissions and returns analysis results.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IAnalysisJobQueue _queue;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(IAnalysisJobQueue queue, ILogger<ResumeController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Submit a resume and job role for asynchronous analysis.
    /// Returns a job ID that can be polled via GET /api/resume/{jobId}.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitResumeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Submit([FromBody] SubmitResumeRequest request)
    {
        if (request.Resume is null || request.JobRole is null)
            return BadRequest("Resume and JobRole are required.");

        var job = new AnalysisJob
        {
            Resume = request.Resume,
            JobRole = request.JobRole
        };

        var jobId = _queue.Enqueue(job);

        _logger.LogInformation("Enqueued analysis job {JobId}.", jobId);

        var response = new SubmitResumeResponse { JobId = jobId };
        return AcceptedAtAction(nameof(GetStatus), new { jobId }, response);
    }

    /// <summary>
    /// Poll the status of a previously submitted analysis job.
    /// Returns the full <see cref="SkillAnalysisResult"/> once the job is complete.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(AnalysisStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetStatus(Guid jobId)
    {
        var job = _queue.GetById(jobId);
        if (job is null)
            return NotFound($"No analysis job found with ID {jobId}.");

        return Ok(new AnalysisStatusResponse
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            Result = job.Result,
            ErrorMessage = job.ErrorMessage
        });
    }
}
