using Transposition.Api.Models;

namespace Transposition.Api.Services;

/// <summary>
/// Analyses a resume against a job role by comparing transferable skills
/// (what the applicant can do) and the tools used (how they did it) against
/// each requirement.  Where a skill is present but the tool differs from the
/// employer's preference, an upskilling recommendation is generated.
/// </summary>
public class ResumeAnalysisService : IResumeAnalysisService
{
    // ---------------------------------------------------------------------------
    // Upskilling resource catalogue
    // Maps a well-known tool name to curated learning resources.
    // ---------------------------------------------------------------------------
    private static readonly Dictionary<string, List<string>> ToolResources =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["C# .NET"] = new()
            {
                "Microsoft Learn – C# Fundamentals: https://learn.microsoft.com/dotnet/csharp/",
                "Pluralsight – C# Path: https://www.pluralsight.com/paths/csharp",
                "freeCodeCamp – C# Tutorial: https://www.youtube.com/watch?v=GhQdlIFylQ8"
            },
            ["ASP.NET Core"] = new()
            {
                "Microsoft Learn – ASP.NET Core: https://learn.microsoft.com/aspnet/core/",
                "Udemy – Complete ASP.NET Core Course"
            },
            ["React"] = new()
            {
                "Official React Docs: https://react.dev/learn",
                "Scrimba – Learn React: https://scrimba.com/learn/learnreact",
                "freeCodeCamp – Full React Course: https://www.youtube.com/watch?v=bMknfKXIFA8"
            },
            ["TypeScript"] = new()
            {
                "TypeScript Handbook: https://www.typescriptlang.org/docs/handbook/",
                "Execute Program – TypeScript: https://www.executeprogram.com/courses/typescript"
            },
            ["xUnit"] = new()
            {
                "xUnit.net Docs: https://xunit.net/docs/getting-started/netcore/cmdline",
                "Microsoft Learn – Unit testing with xUnit: https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test"
            },
            ["GitHub Actions"] = new()
            {
                "GitHub Actions Docs: https://docs.github.com/actions",
                "freeCodeCamp – GitHub Actions Tutorial: https://www.youtube.com/watch?v=R8_veQiYBjI"
            },
            ["Docker"] = new()
            {
                "Docker Getting Started: https://docs.docker.com/get-started/",
                "Play With Docker: https://labs.play-with-docker.com/"
            },
            ["SQL Server"] = new()
            {
                "Microsoft Learn – SQL Server: https://learn.microsoft.com/sql/sql-server/",
                "W3Schools – SQL Tutorial: https://www.w3schools.com/sql/"
            },
            ["Azure"] = new()
            {
                "Microsoft Learn – Azure Fundamentals (AZ-900): https://learn.microsoft.com/certifications/azure-fundamentals/",
                "Azure Free Account: https://azure.microsoft.com/free/"
            }
        };

    /// <inheritdoc/>
    public SkillAnalysisResult Analyse(Resume resume, JobRole jobRole)
    {
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(jobRole);

        // Build a lookup: skill → tools the applicant used (case-insensitive)
        var applicantSkillMap = BuildApplicantSkillMap(resume);

        var skillMatches = new List<SkillMatch>();
        var recommendations = new List<UpskillRecommendation>();

        foreach (var requirement in jobRole.Requirements)
        {
            var skillKey = requirement.Skill.Trim();

            // Find the applicant tools for this skill (case-insensitive match)
            var applicantEntry = applicantSkillMap
                .FirstOrDefault(kv => string.Equals(kv.Key, skillKey, StringComparison.OrdinalIgnoreCase));

            bool hasSkill = applicantEntry.Value is { Count: > 0 };
            var applicantTools = hasSkill ? applicantEntry.Value : new List<string>();

            bool toolAligned = hasSkill && applicantTools.Any(t =>
                string.Equals(t, requirement.PreferredTool, StringComparison.OrdinalIgnoreCase));

            skillMatches.Add(new SkillMatch
            {
                Skill = skillKey,
                HasSkill = hasSkill,
                ApplicantTools = applicantTools,
                PreferredTool = requirement.PreferredTool,
                ToolAligned = toolAligned,
                IsMandatory = requirement.IsMandatory
            });

            // Generate recommendations for gaps / tool mismatches
            if (!hasSkill)
            {
                recommendations.Add(BuildMissingSkillRecommendation(requirement));
            }
            else if (!toolAligned)
            {
                recommendations.Add(BuildToolMismatchRecommendation(requirement, applicantTools));
            }
        }

        int overallMatch = CalculateOverallMatch(skillMatches);
        string summary = BuildSummary(resume, jobRole, skillMatches, overallMatch);

        return new SkillAnalysisResult
        {
            OverallMatchPercentage = overallMatch,
            SkillMatches = skillMatches,
            UpskillRecommendations = recommendations,
            Summary = summary
        };
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static Dictionary<string, List<string>> BuildApplicantSkillMap(Resume resume)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var exp in resume.Experiences)
        {
            foreach (var skill in exp.Skills)
            {
                var key = skill.Trim();
                if (!map.TryGetValue(key, out var tools))
                {
                    tools = new List<string>();
                    map[key] = tools;
                }
                foreach (var tool in exp.Tools)
                {
                    var toolTrimmed = tool.Trim();
                    if (!tools.Contains(toolTrimmed, StringComparer.OrdinalIgnoreCase))
                        tools.Add(toolTrimmed);
                }
            }
        }

        return map;
    }

    private static int CalculateOverallMatch(List<SkillMatch> matches)
    {
        var mandatory = matches.Where(m => m.IsMandatory).ToList();
        if (mandatory.Count == 0) return 100;

        int matched = mandatory.Count(m => m.HasSkill);
        return (int)Math.Round(matched / (double)mandatory.Count * 100);
    }

    private static UpskillRecommendation BuildMissingSkillRecommendation(SkillRequirement req)
    {
        var resources = GetResources(req.PreferredTool);

        return new UpskillRecommendation
        {
            Topic = req.Skill,
            Reason = RecommendationReason.MissingSkill,
            Description = $"Your resume does not demonstrate '{req.Skill}'. " +
                          $"This role requires it, preferably using {req.PreferredTool}. " +
                          "Consider building a portfolio project that showcases this skill.",
            SuggestedResources = resources,
            EstimatedEffort = EstimateEffort(req.Skill, req.PreferredTool)
        };
    }

    private static UpskillRecommendation BuildToolMismatchRecommendation(
        SkillRequirement req,
        List<string> applicantTools)
    {
        var toolList = string.Join(", ", applicantTools);
        var resources = GetResources(req.PreferredTool);

        return new UpskillRecommendation
        {
            Topic = req.PreferredTool,
            Reason = RecommendationReason.ToolMismatch,
            Description = $"You have '{req.Skill}' experience using {toolList}, which is great! " +
                          $"However, this role prefers {req.PreferredTool}. " +
                          $"Your existing skill transfers — focus on learning the {req.PreferredTool} " +
                          "syntax and ecosystem specifics.",
            SuggestedResources = resources,
            EstimatedEffort = EffortLevel.Low
        };
    }

    private static List<string> GetResources(string tool)
    {
        if (ToolResources.TryGetValue(tool, out var resources))
            return new List<string>(resources);

        return new List<string>
        {
            $"Search for '{tool}' on Microsoft Learn: https://learn.microsoft.com",
            $"Official {tool} documentation — search online for the latest link."
        };
    }

    private static EffortLevel EstimateEffort(string skill, string tool)
    {
        // Heuristic: common foundational skills are medium effort;
        // specialised or platform-specific tools are high effort.
        var highEffortKeywords = new[] { "architecture", "security", "cloud", "devops", "machine learning" };
        var lowEffortKeywords = new[] { "testing", "version control", "documentation" };

        var combined = (skill + " " + tool).ToLowerInvariant();

        if (highEffortKeywords.Any(k => combined.Contains(k))) return EffortLevel.High;
        if (lowEffortKeywords.Any(k => combined.Contains(k))) return EffortLevel.Low;
        return EffortLevel.Medium;
    }

    private static string BuildSummary(
        Resume resume,
        JobRole jobRole,
        List<SkillMatch> matches,
        int overallMatch)
    {
        int total = matches.Count;
        int present = matches.Count(m => m.HasSkill);
        int aligned = matches.Count(m => m.ToolAligned);
        int gaps = total - present;
        int mismatches = present - aligned;

        return $"{resume.ApplicantName} matches {overallMatch}% of the mandatory requirements for " +
               $"the {jobRole.Title} role. " +
               $"Out of {total} required skill(s): {present} are present, " +
               $"{aligned} are demonstrated with the preferred tool, " +
               $"{mismatches} have a tool mismatch, and " +
               $"{gaps} skill(s) are not yet evidenced on the resume.";
    }
}
