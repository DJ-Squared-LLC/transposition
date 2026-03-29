namespace Transposition.Api.Models;

/// <summary>Request body for submitting a resume for analysis.</summary>
public class SubmitResumeRequest
{
    public Resume Resume { get; set; } = new();
    public JobRole JobRole { get; set; } = new();
}

/// <summary>Response returned immediately when a resume is accepted for analysis.</summary>
public class SubmitResumeResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = AnalysisJobStatus.Queued.ToString();
    public string Message { get; set; } = "Your resume has been queued for analysis.";
}

/// <summary>Response returned when polling for analysis results.</summary>
public class AnalysisStatusResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public SkillAnalysisResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
