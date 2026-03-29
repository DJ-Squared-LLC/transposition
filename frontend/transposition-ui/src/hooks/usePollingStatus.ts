import { useState, useCallback } from 'react';
import type { AnalysisStatusResponse } from '../types';
import { getAnalysisStatus } from '../services/api';

const POLL_INTERVAL_MS = 2000;
const MAX_POLLS = 60; // 2 min timeout

export function usePollingStatus() {
  const [status, setStatus] = useState<AnalysisStatusResponse | null>(null);
  const [polling, setPolling] = useState(false);

  const startPolling = useCallback((jobId: string) => {
    setPolling(true);
    let count = 0;

    const interval = setInterval(async () => {
      count++;
      try {
        const response = await getAnalysisStatus(jobId);
        setStatus(response);
        if (
          response.status === 'Completed' ||
          response.status === 'Failed' ||
          count >= MAX_POLLS
        ) {
          clearInterval(interval);
          setPolling(false);
        }
      } catch {
        clearInterval(interval);
        setPolling(false);
      }
    }, POLL_INTERVAL_MS);
  }, []);

  return { status, polling, startPolling };
}
