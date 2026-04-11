namespace Transposition.Api.Models;

/// <summary>
/// Tracks an enqueued resume-analysis job throughout its lifecycle.
/// </summary>
public class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Queued;
    public Resume Resume { get; set; } = new();
    public JobRole JobRole { get; set; } = new();

    /// <summary>Populated when <see cref="Status"/> reaches <see cref="AnalysisJobStatus.Completed"/>.</summary>
    public SkillAnalysisResult? Result { get; set; }

    /// <summary>Human-readable error message when <see cref="Status"/> is <see cref="AnalysisJobStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; set; }
}

public enum AnalysisJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}
