import { useState } from 'react';
import type { Resume, ExperienceEntry } from '../types';

interface Props {
  value: Resume;
  onChange: (resume: Resume) => void;
}

const DEFAULT_EXP: ExperienceEntry = {
  context: '',
  skills: [],
  tools: [],
  description: '',
};

export function ResumeForm({ value, onChange }: Props) {
  const [newExp, setNewExp] = useState<ExperienceEntry>({ ...DEFAULT_EXP });
  const [skillInput, setSkillInput] = useState('');
  const [toolInput, setToolInput] = useState('');

  const update = (patch: Partial<Resume>) => onChange({ ...value, ...patch });

  const addExperience = () => {
    if (!newExp.description.trim()) return;
    update({ experiences: [...value.experiences, { ...newExp }] });
    setNewExp({ ...DEFAULT_EXP });
    setSkillInput('');
    setToolInput('');
  };

  const removeExperience = (idx: number) =>
    update({ experiences: value.experiences.filter((_, i) => i !== idx) });

  const commitSkills = () => {
    const skills = skillInput
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
    setNewExp((e) => ({ ...e, skills }));
    setSkillInput(skills.join(', '));
  };

  const commitTools = () => {
    const tools = toolInput
      .split(',')
      .map((t) => t.trim())
      .filter(Boolean);
    setNewExp((e) => ({ ...e, tools }));
    setToolInput(tools.join(', '));
  };

  return (
    <section className="form-section">
      <h2>Applicant Resume</h2>

      <label>
        Full Name *
        <input
          value={value.applicantName}
          onChange={(e) => update({ applicantName: e.target.value })}
          placeholder="Jane Doe"
          required
        />
      </label>

      <label>
        Email
        <input
          type="email"
          value={value.contactEmail}
          onChange={(e) => update({ contactEmail: e.target.value })}
          placeholder="jane@example.com"
        />
      </label>

      <label>
        Summary
        <textarea
          value={value.summary}
          onChange={(e) => update({ summary: e.target.value })}
          rows={2}
          placeholder="Professional summary…"
        />
      </label>

      <fieldset>
        <legend>Experience Entries</legend>

        {value.experiences.map((exp, i) => (
          <div key={i} className="experience-card">
            <div className="experience-header">
              <strong>{exp.description}</strong>
              {exp.context && <span className="context-badge">{exp.context}</span>}
              <button
                type="button"
                className="btn-remove"
                onClick={() => removeExperience(i)}
                aria-label="Remove experience"
              >
                ×
              </button>
            </div>
            <div className="experience-meta">
              <span>
                <em>Skills:</em> {exp.skills.join(', ') || '—'}
              </span>
              <span>
                <em>Tools:</em> {exp.tools.join(', ') || '—'}
              </span>
            </div>
          </div>
        ))}

        <div className="add-experience-block">
          <label>
            Description *
            <input
              value={newExp.description}
              onChange={(e) =>
                setNewExp((exp) => ({ ...exp, description: e.target.value }))
              }
              placeholder="e.g. Built REST APIs serving 10k RPS"
            />
          </label>
          <label>
            Context (employer / project)
            <input
              value={newExp.context ?? ''}
              onChange={(e) =>
                setNewExp((exp) => ({ ...exp, context: e.target.value }))
              }
              placeholder="e.g. Acme Corp"
            />
          </label>
          <label>
            Skills (comma-separated)
            <input
              value={skillInput}
              onChange={(e) => setSkillInput(e.target.value)}
              onBlur={commitSkills}
              placeholder="e.g. REST API design, Unit testing"
            />
          </label>
          <label>
            Tools (comma-separated)
            <input
              value={toolInput}
              onChange={(e) => setToolInput(e.target.value)}
              onBlur={commitTools}
              placeholder="e.g. Node.js, Jest"
            />
          </label>
          <button type="button" className="btn-add" onClick={addExperience}>
            + Add Experience
          </button>
        </div>
      </fieldset>
    </section>
  );
}
