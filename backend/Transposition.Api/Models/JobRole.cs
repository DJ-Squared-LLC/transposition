namespace Transposition.Api.Models;

/// <summary>
/// Represents a job role that the applicant is applying for.
/// </summary>
public class JobRole
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Skill requirements for this role.
    /// Each entry maps a transferable skill to the preferred tool the employer uses.
    /// </summary>
    public List<SkillRequirement> Requirements { get; set; } = new();
}

/// <summary>
/// A single skill requirement for a job role, pairing a transferable skill with
/// the preferred tool/technology used in this organisation.
/// </summary>
public class SkillRequirement
{
    /// <summary>
    /// Transferable skill name — matches the same vocabulary used in <see cref="ExperienceEntry.Skills"/>.
    /// e.g. "REST API design", "Unit testing", "CI/CD pipeline management"
    /// </summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>
    /// The tool or technology the employer prefers for this skill.
    /// e.g. "C# .NET", "xUnit", "GitHub Actions"
    /// </summary>
    public string PreferredTool { get; set; } = string.Empty;

    /// <summary>Indicates whether this requirement is mandatory or merely preferred.</summary>
    public bool IsMandatory { get; set; } = true;
}
