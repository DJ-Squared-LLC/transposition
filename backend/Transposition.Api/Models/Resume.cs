namespace Transposition.Api.Models;

/// <summary>
/// Represents an applicant's resume with experience entries.
/// </summary>
public class Resume
{
    public string ApplicantName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Free-text summary or objective from the resume.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Collection of experience entries extracted from the resume.
    /// Each entry carries one or more skills and the tools used to demonstrate them.
    /// </summary>
    public List<ExperienceEntry> Experiences { get; set; } = new();
}

/// <summary>
/// A single line of work experience describing what the applicant did and with which tools.
/// </summary>
public class ExperienceEntry
{
    /// <summary>Employer or project context (optional)</summary>
    public string? Context { get; set; }

    /// <summary>
    /// Transferable skills demonstrated (e.g. "REST API design", "Unit testing", "Data modelling").
    /// These are tool-agnostic capabilities.
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Technologies / tools used to demonstrate the skills
    /// (e.g. "Node.js", "Express", "Jest", "PostgreSQL").
    /// </summary>
    public List<string> Tools { get; set; } = new();

    /// <summary>Short human-readable description of the experience.</summary>
    public string Description { get; set; } = string.Empty;
}
