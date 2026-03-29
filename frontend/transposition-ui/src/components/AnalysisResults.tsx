import type { SkillAnalysisResult, SkillMatch, UpskillRecommendation } from '../types';

interface Props {
  result: SkillAnalysisResult;
}

const EFFORT_COLOUR: Record<string, string> = {
  Low: 'effort-low',
  Medium: 'effort-medium',
  High: 'effort-high',
};

function MatchBadge({ match }: { match: SkillMatch }) {
  if (!match.hasSkill) return <span className="badge badge-missing">Missing</span>;
  if (match.toolAligned) return <span className="badge badge-aligned">Aligned</span>;
  return <span className="badge badge-mismatch">Tool mismatch</span>;
}

function RecommendationCard({ rec }: { rec: UpskillRecommendation }) {
  return (
    <div className={`recommendation-card ${EFFORT_COLOUR[rec.estimatedEffort] ?? ''}`}>
      <div className="rec-header">
        <strong>{rec.topic}</strong>
        <span className="effort-tag">{rec.estimatedEffort} effort</span>
        <span className="reason-tag">
          {rec.reason === 'MissingSkill' ? '⚠ Missing skill' : '🔧 Tool mismatch'}
        </span>
      </div>
      <p className="rec-description">{rec.description}</p>
      {rec.suggestedResources.length > 0 && (
        <details>
          <summary>Learning resources</summary>
          <ul className="resources-list">
            {rec.suggestedResources.map((r, i) => {
              const urlMatch = r.match(/https?:\/\/\S+/);
              return (
                <li key={i}>
                  {urlMatch ? (
                    <>
                      {r.replace(urlMatch[0], '').replace(/:\s*$/, '')}{' '}
                      <a href={urlMatch[0]} target="_blank" rel="noopener noreferrer">
                        {urlMatch[0]}
                      </a>
                    </>
                  ) : (
                    r
                  )}
                </li>
              );
            })}
          </ul>
        </details>
      )}
    </div>
  );
}

export function AnalysisResults({ result }: Props) {
  const { overallMatchPercentage, skillMatches, upskillRecommendations, summary } = result;

  const ringColour =
    overallMatchPercentage >= 75
      ? '#22c55e'
      : overallMatchPercentage >= 50
      ? '#f59e0b'
      : '#ef4444';

  return (
    <div className="analysis-results">
      <h2>Analysis Results</h2>

      <div className="overall-score" aria-label={`Overall match: ${overallMatchPercentage}%`}>
        <svg viewBox="0 0 36 36" className="score-ring">
          <circle cx="18" cy="18" r="15.9" fill="none" stroke="#e5e7eb" strokeWidth="3" />
          <circle
            cx="18"
            cy="18"
            r="15.9"
            fill="none"
            stroke={ringColour}
            strokeWidth="3"
            strokeDasharray={`${overallMatchPercentage} ${100 - overallMatchPercentage}`}
            strokeDashoffset="25"
            strokeLinecap="round"
          />
        </svg>
        <span className="score-label" style={{ color: ringColour }}>
          {overallMatchPercentage}%
        </span>
        <p className="score-sublabel">Overall match (mandatory skills)</p>
      </div>

      <p className="summary-text">{summary}</p>

      <h3>Skill Breakdown</h3>
      <table className="skill-table">
        <thead>
          <tr>
            <th>Required Skill</th>
            <th>Preferred Tool</th>
            <th>Your Tools</th>
            <th>Status</th>
            <th>Mandatory</th>
          </tr>
        </thead>
        <tbody>
          {skillMatches.map((match, i) => (
            <tr key={i} className={match.hasSkill ? '' : 'row-missing'}>
              <td>{match.skill}</td>
              <td>{match.preferredTool}</td>
              <td>{match.applicantTools.join(', ') || '—'}</td>
              <td>
                <MatchBadge match={match} />
              </td>
              <td>{match.isMandatory ? 'Yes' : 'Optional'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {upskillRecommendations.length > 0 && (
        <>
          <h3>Upskilling Recommendations</h3>
          <div className="recommendations-list">
            {upskillRecommendations.map((rec, i) => (
              <RecommendationCard key={i} rec={rec} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}
