import { useState } from 'react';
import type { Resume, JobRole } from './types';
import { ResumeForm } from './components/ResumeForm';
import { JobRoleForm } from './components/JobRoleForm';
import { AnalysisResults } from './components/AnalysisResults';
import { submitResume } from './services/api';
import { usePollingStatus } from './hooks/usePollingStatus';
import './App.css';

const EMPTY_RESUME: Resume = {
  applicantName: '',
  contactEmail: '',
  summary: '',
  experiences: [],
};

const EMPTY_JOB_ROLE: JobRole = {
  title: '',
  department: '',
  description: '',
  requirements: [],
};

function App() {
  const [resume, setResume] = useState<Resume>({ ...EMPTY_RESUME });
  const [jobRole, setJobRole] = useState<JobRole>({ ...EMPTY_JOB_ROLE });
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { status, polling, startPolling, resetStatus } = usePollingStatus();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    setSubmitting(true);
    try {
      const { jobId } = await submitResume({ resume, jobRole });
      startPolling(jobId);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Submission failed.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleReset = () => {
    setResume({ ...EMPTY_RESUME });
    setJobRole({ ...EMPTY_JOB_ROLE });
    setSubmitError(null);
    resetStatus();
  };

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>Transposition</h1>
        <p>Resume-to-role skill calibration — see exactly how your experience maps to the job.</p>
      </header>

      <main className="app-main">
        {!status?.result ? (
          <form className="submission-form" onSubmit={handleSubmit} noValidate>
            <ResumeForm value={resume} onChange={setResume} />
            <JobRoleForm value={jobRole} onChange={setJobRole} />

            {submitError && (
              <div className="error-banner" role="alert">
                {submitError}
              </div>
            )}

            {polling && (
              <div className="status-banner" role="status">
                {status
                  ? `Status: ${status.status} — analysing your resume…`
                  : 'Queued — waiting for a worker thread…'}
              </div>
            )}

            <div className="form-actions">
              <button
                type="submit"
                className="btn-primary"
                disabled={submitting || polling}
              >
                {submitting ? 'Submitting…' : polling ? 'Processing…' : 'Analyse Resume'}
              </button>
              <button type="button" className="btn-secondary" onClick={handleReset}>
                Reset
              </button>
            </div>
          </form>
        ) : (
          <>
            <AnalysisResults result={status.result} />
            <div className="form-actions">
              <button className="btn-secondary" onClick={handleReset}>
                ← Analyse another resume
              </button>
            </div>
          </>
        )}

        {status?.status === 'Failed' && (
          <div className="error-banner" role="alert">
            Analysis failed: {status.errorMessage ?? 'Unknown error'}
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
