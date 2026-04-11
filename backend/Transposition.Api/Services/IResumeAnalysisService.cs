using Transposition.Api.Models;

namespace Transposition.Api.Services;

/// <summary>
/// Core analysis service that maps a resume's skills and tools against
/// a job role's requirements, producing a <see cref="SkillAnalysisResult"/>.
/// </summary>
public interface IResumeAnalysisService
{
    SkillAnalysisResult Analyse(Resume resume, JobRole jobRole);
}
