// -----------------------------------------------------------------------
// Domain types mirroring the C# backend models
// -----------------------------------------------------------------------

export interface ExperienceEntry {
  context?: string;
  skills: string[];
  tools: string[];
  description: string;
}

export interface Resume {
  applicantName: string;
  contactEmail: string;
  summary: string;
  experiences: ExperienceEntry[];
}

export interface SkillRequirement {
  skill: string;
  preferredTool: string;
  isMandatory: boolean;
}

export interface JobRole {
  title: string;
  department: string;
  description: string;
  requirements: SkillRequirement[];
}

export interface SubmitResumeRequest {
  resume: Resume;
  jobRole: JobRole;
}

export interface SubmitResumeResponse {
  jobId: string;
  status: string;
  message: string;
}

// -----------------------------------------------------------------------
// Analysis result types
// -----------------------------------------------------------------------

export type RecommendationReason = 'MissingSkill' | 'ToolMismatch';
export type EffortLevel = 'Low' | 'Medium' | 'High';

export interface SkillMatch {
  skill: string;
  hasSkill: boolean;
  applicantTools: string[];
  preferredTool: string;
  toolAligned: boolean;
  isMandatory: boolean;
}

export interface UpskillRecommendation {
  topic: string;
  reason: RecommendationReason;
  description: string;
  suggestedResources: string[];
  estimatedEffort: EffortLevel;
}

export interface SkillAnalysisResult {
  analysedAt: string;
  overallMatchPercentage: number;
  skillMatches: SkillMatch[];
  upskillRecommendations: UpskillRecommendation[];
  summary: string;
}

export type AnalysisJobStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export interface AnalysisStatusResponse {
  jobId: string;
  status: AnalysisJobStatus;
  createdAt: string;
  result?: SkillAnalysisResult;
  errorMessage?: string;
}
