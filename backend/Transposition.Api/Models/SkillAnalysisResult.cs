namespace Transposition.Api.Models;

/// <summary>
/// Full analysis result for a resume against a specific job role.
/// </summary>
public class SkillAnalysisResult
{
    public DateTime AnalysedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Overall match percentage (0–100) based on mandatory skill coverage.</summary>
    public int OverallMatchPercentage { get; set; }

    /// <summary>
    /// Per-skill breakdowns: what the applicant demonstrated versus what the role requires.
    /// </summary>
    public List<SkillMatch> SkillMatches { get; set; } = new();

    /// <summary>
    /// Actionable upskilling recommendations for any skill gap or tool mismatch found.
    /// </summary>
    public List<UpskillRecommendation> UpskillRecommendations { get; set; } = new();

    /// <summary>Plain-language narrative summary of the analysis.</summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Describes how well one required skill is covered by the applicant's resume.
/// Separates the skill (what the person can do) from the tool (how they did it).
/// </summary>
public class SkillMatch
{
    /// <summary>Transferable skill being evaluated (e.g. "REST API design").</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>Whether the applicant has demonstrated this skill, regardless of tool.</summary>
    public bool HasSkill { get; set; }

    /// <summary>
    /// Tool(s) the applicant used to demonstrate this skill (may be empty if skill is absent).
    /// </summary>
    public List<string> ApplicantTools { get; set; } = new();

    /// <summary>Tool the job role prefers for this skill.</summary>
    public string PreferredTool { get; set; } = string.Empty;

    /// <summary>
    /// True when the applicant has the skill AND already uses the preferred tool.
    /// </summary>
    public bool ToolAligned { get; set; }

    public bool IsMandatory { get; set; }
}

/// <summary>
/// Recommendation for an applicant to upskill in a specific area.
/// </summary>
public class UpskillRecommendation
{
    /// <summary>The skill or tool the recommendation targets.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// The reason for this recommendation — either a missing skill or a tool mismatch.
    /// </summary>
    public RecommendationReason Reason { get; set; }

    /// <summary>Plain-language description of the gap and what to learn.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Suggested learning resources or actions.</summary>
    public List<string> SuggestedResources { get; set; } = new();

    /// <summary>Estimated effort level to acquire this skill or tool proficiency.</summary>
    public EffortLevel EstimatedEffort { get; set; }
}

public enum RecommendationReason
{
    /// <summary>The applicant lacks the skill entirely.</summary>
    MissingSkill,

    /// <summary>The applicant has the skill but uses a different tool than the employer prefers.</summary>
    ToolMismatch
}

public enum EffortLevel
{
    Low,
    Medium,
    High
}
