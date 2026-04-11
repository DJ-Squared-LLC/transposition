import type {
  SubmitResumeRequest,
  SubmitResumeResponse,
  AnalysisStatusResponse,
} from '../types';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

export async function submitResume(
  request: SubmitResumeRequest
): Promise<SubmitResumeResponse> {
  const res = await fetch(`${BASE_URL}/api/resume`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json() as Promise<SubmitResumeResponse>;
}

export async function getAnalysisStatus(
  jobId: string
): Promise<AnalysisStatusResponse> {
  const res = await fetch(`${BASE_URL}/api/resume/${jobId}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json() as Promise<AnalysisStatusResponse>;
}
