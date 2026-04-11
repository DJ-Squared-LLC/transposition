using Transposition.Api.Models;
using Transposition.Api.Services;

namespace Transposition.Tests;

public class ResumeAnalysisServiceTests
{
    private readonly ResumeAnalysisService _sut = new();

    // -----------------------------------------------------------------------
    // Helper builders
    // -----------------------------------------------------------------------

    private static Resume MakeResume(params (string skill, string tool)[] entries)
    {
        var resume = new Resume { ApplicantName = "Test Applicant" };
        foreach (var (skill, tool) in entries)
        {
            resume.Experiences.Add(new ExperienceEntry
            {
                Skills = new List<string> { skill },
                Tools = new List<string> { tool }
            });
        }
        return resume;
    }

    private static JobRole MakeJobRole(params (string skill, string tool, bool mandatory)[] reqs)
    {
        var role = new JobRole { Title = "Test Role" };
        foreach (var (skill, tool, mandatory) in reqs)
        {
            role.Requirements.Add(new SkillRequirement
            {
                Skill = skill,
                PreferredTool = tool,
                IsMandatory = mandatory
            });
        }
        return role;
    }

    // -----------------------------------------------------------------------
    // Tests: skill matching
    // -----------------------------------------------------------------------

    [Fact]
    public void Analyse_FullMatch_Returns100Percent()
    {
        var resume = MakeResume(("REST API design", "C# .NET"));
        var role = MakeJobRole(("REST API design", "C# .NET", true));

        var result = _sut.Analyse(resume, role);

        Assert.Equal(100, result.OverallMatchPercentage);
        Assert.Single(result.SkillMatches);
        Assert.True(result.SkillMatches[0].HasSkill);
        Assert.True(result.SkillMatches[0].ToolAligned);
        Assert.Empty(result.UpskillRecommendations);
    }

    [Fact]
    public void Analyse_MissingMandatorySkill_Returns0Percent()
    {
        var resume = MakeResume(); // no experiences
        var role = MakeJobRole(("Unit testing", "xUnit", true));

        var result = _sut.Analyse(resume, role);

        Assert.Equal(0, result.OverallMatchPercentage);
        Assert.Single(result.UpskillRecommendations);
        Assert.Equal(RecommendationReason.MissingSkill, result.UpskillRecommendations[0].Reason);
    }

    [Fact]
    public void Analyse_SkillPresentButWrongTool_ReturnsToolMismatchRecommendation()
    {
        // Applicant knows REST API design but with Node.js; role wants C# .NET
        var resume = MakeResume(("REST API design", "Node.js"));
        var role = MakeJobRole(("REST API design", "C# .NET", true));

        var result = _sut.Analyse(resume, role);

        // Skill IS present so mandatory coverage is 100%
        Assert.Equal(100, result.OverallMatchPercentage);

        var match = Assert.Single(result.SkillMatches);
        Assert.True(match.HasSkill);
        Assert.False(match.ToolAligned);

        var rec = Assert.Single(result.UpskillRecommendations);
        Assert.Equal(RecommendationReason.ToolMismatch, rec.Reason);
        Assert.Equal("C# .NET", rec.Topic);
    }

    [Fact]
    public void Analyse_MixedRequirements_MatchPercentageBasedOnMandatoryOnly()
    {
        // Two mandatory skills: applicant has one; one optional skill: applicant has none
        var resume = MakeResume(("Unit testing", "Jest"));
        var role = MakeJobRole(
            ("REST API design", "C# .NET", true),   // missing → 0
            ("Unit testing", "xUnit", true),          // present (tool mismatch) → 1
            ("Cloud deployment", "Azure", false));    // optional, missing → not counted

        var result = _sut.Analyse(resume, role);

        // 1 of 2 mandatory skills present = 50%
        Assert.Equal(50, result.OverallMatchPercentage);
    }

    [Fact]
    public void Analyse_CaseInsensitiveSkillMatch()
    {
        var resume = MakeResume(("rest api design", "C# .NET"));
        var role = MakeJobRole(("REST API Design", "C# .NET", true));

        var result = _sut.Analyse(resume, role);

        Assert.Equal(100, result.OverallMatchPercentage);
    }

    [Fact]
    public void Analyse_MultipleToolsForSameSkill_RecognisesPreferredTool()
    {
        var resume = new Resume { ApplicantName = "Applicant" };
        resume.Experiences.Add(new ExperienceEntry
        {
            Skills = new List<string> { "REST API design" },
            Tools = new List<string> { "Node.js", "C# .NET" }   // both listed
        });
        var role = MakeJobRole(("REST API design", "C# .NET", true));

        var result = _sut.Analyse(resume, role);

        Assert.True(result.SkillMatches[0].ToolAligned);
        Assert.Empty(result.UpskillRecommendations);
    }

    [Fact]
    public void Analyse_NoRequirements_Returns100PercentWithNoRecommendations()
    {
        var resume = MakeResume(("anything", "some tool"));
        var role = new JobRole { Title = "Empty Role" };

        var result = _sut.Analyse(resume, role);

        Assert.Equal(100, result.OverallMatchPercentage);
        Assert.Empty(result.SkillMatches);
        Assert.Empty(result.UpskillRecommendations);
    }

    [Fact]
    public void Analyse_NullResume_ThrowsArgumentNullException()
    {
        var role = MakeJobRole(("any", "tool", true));
        Assert.Throws<ArgumentNullException>(() => _sut.Analyse(null!, role));
    }

    [Fact]
    public void Analyse_NullJobRole_ThrowsArgumentNullException()
    {
        var resume = MakeResume();
        Assert.Throws<ArgumentNullException>(() => _sut.Analyse(resume, null!));
    }

    // -----------------------------------------------------------------------
    // Tests: upskilling resources
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("C# .NET")]
    [InlineData("React")]
    [InlineData("xUnit")]
    [InlineData("GitHub Actions")]
    public void Analyse_KnownTool_IncludesKnownResources(string tool)
    {
        var resume = MakeResume(); // no matching skill
        var role = MakeJobRole(("some skill", tool, true));

        var result = _sut.Analyse(resume, role);

        var rec = Assert.Single(result.UpskillRecommendations);
        Assert.NotEmpty(rec.SuggestedResources);
    }

    [Fact]
    public void Analyse_UnknownTool_StillProvidesFallbackResources()
    {
        var resume = MakeResume();
        var role = MakeJobRole(("obscure skill", "SomePropietaryTool", true));

        var result = _sut.Analyse(resume, role);

        var rec = Assert.Single(result.UpskillRecommendations);
        Assert.NotEmpty(rec.SuggestedResources);
    }

    // -----------------------------------------------------------------------
    // Tests: summary
    // -----------------------------------------------------------------------

    [Fact]
    public void Analyse_Summary_ContainsApplicantNameAndRoleTitle()
    {
        var resume = new Resume { ApplicantName = "Jane Doe" };
        var role = new JobRole { Title = "Senior Backend Engineer" };
        role.Requirements.Add(new SkillRequirement
            { Skill = "REST API design", PreferredTool = "C# .NET", IsMandatory = true });

        var result = _sut.Analyse(resume, role);

        Assert.Contains("Jane Doe", result.Summary);
        Assert.Contains("Senior Backend Engineer", result.Summary);
    }
}
