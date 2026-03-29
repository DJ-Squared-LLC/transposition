import { useState } from 'react';
import type { JobRole, SkillRequirement } from '../types';

interface Props {
  value: JobRole;
  onChange: (role: JobRole) => void;
}

const DEFAULT_REQ: SkillRequirement = {
  skill: '',
  preferredTool: '',
  isMandatory: true,
};

export function JobRoleForm({ value, onChange }: Props) {
  const [newReq, setNewReq] = useState<SkillRequirement>({ ...DEFAULT_REQ });

  const update = (patch: Partial<JobRole>) => onChange({ ...value, ...patch });

  const addRequirement = () => {
    if (!newReq.skill.trim() || !newReq.preferredTool.trim()) return;
    update({ requirements: [...value.requirements, { ...newReq }] });
    setNewReq({ ...DEFAULT_REQ });
  };

  const removeRequirement = (idx: number) =>
    update({ requirements: value.requirements.filter((_, i) => i !== idx) });

  return (
    <section className="form-section">
      <h2>Job Role</h2>

      <label>
        Role Title *
        <input
          value={value.title}
          onChange={(e) => update({ title: e.target.value })}
          placeholder="e.g. Senior Backend Engineer"
          required
        />
      </label>

      <label>
        Department
        <input
          value={value.department}
          onChange={(e) => update({ department: e.target.value })}
          placeholder="e.g. Engineering"
        />
      </label>

      <label>
        Description
        <textarea
          value={value.description}
          onChange={(e) => update({ description: e.target.value })}
          rows={2}
          placeholder="Brief description of the role…"
        />
      </label>

      <fieldset>
        <legend>Skill Requirements</legend>

        {value.requirements.length > 0 && (
          <table className="requirements-table">
            <thead>
              <tr>
                <th>Skill</th>
                <th>Preferred Tool</th>
                <th>Mandatory</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {value.requirements.map((req, i) => (
                <tr key={i}>
                  <td>{req.skill}</td>
                  <td>{req.preferredTool}</td>
                  <td>{req.isMandatory ? 'Yes' : 'No'}</td>
                  <td>
                    <button
                      type="button"
                      className="btn-remove"
                      onClick={() => removeRequirement(i)}
                      aria-label="Remove requirement"
                    >
                      ×
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <div className="add-requirement-row">
          <input
            value={newReq.skill}
            onChange={(e) => setNewReq((r) => ({ ...r, skill: e.target.value }))}
            placeholder="Skill (e.g. REST API design)"
            aria-label="New skill"
          />
          <input
            value={newReq.preferredTool}
            onChange={(e) =>
              setNewReq((r) => ({ ...r, preferredTool: e.target.value }))
            }
            placeholder="Preferred tool (e.g. C# .NET)"
            aria-label="New preferred tool"
          />
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={newReq.isMandatory}
              onChange={(e) =>
                setNewReq((r) => ({ ...r, isMandatory: e.target.checked }))
              }
            />
            Mandatory
          </label>
          <button type="button" className="btn-add" onClick={addRequirement}>
            + Add
          </button>
        </div>
      </fieldset>
    </section>
  );
}
