import { test, expect } from '@playwright/test';
import type { AnalysisStatusResponse, SkillAnalysisResult } from '../src/types';

const MOCK_JOB_ID = 'test-job-123';

const MOCK_ANALYSIS_RESULT: SkillAnalysisResult = {
  analysedAt: '2026-04-11T09:00:00Z',
  overallMatchPercentage: 80,
  summary: 'Strong match. You meet most of the mandatory requirements.',
  skillMatches: [
    {
      skill: 'REST API design',
      hasSkill: true,
      applicantTools: ['Node.js'],
      preferredTool: 'C# .NET',
      toolAligned: false,
      isMandatory: true,
    },
    {
      skill: 'Unit testing',
      hasSkill: true,
      applicantTools: ['Jest'],
      preferredTool: 'xUnit',
      toolAligned: false,
      isMandatory: false,
    },
  ],
  upskillRecommendations: [
    {
      topic: 'C# .NET',
      reason: 'ToolMismatch',
      description: 'Consider learning C# .NET to align with the team stack.',
      suggestedResources: ['https://learn.microsoft.com/en-us/dotnet/csharp/'],
      estimatedEffort: 'Medium',
    },
  ],
};

const MOCK_COMPLETED_STATUS: AnalysisStatusResponse = {
  jobId: MOCK_JOB_ID,
  status: 'Completed',
  createdAt: '2026-04-11T09:00:00Z',
  result: MOCK_ANALYSIS_RESULT,
};

const MOCK_QUEUED_STATUS: AnalysisStatusResponse = {
  jobId: MOCK_JOB_ID,
  status: 'Queued',
  createdAt: '2026-04-11T09:00:00Z',
};

const MOCK_FAILED_STATUS: AnalysisStatusResponse = {
  jobId: MOCK_JOB_ID,
  status: 'Failed',
  createdAt: '2026-04-11T09:00:00Z',
  errorMessage: 'AI service unavailable',
};

async function fillMinimalForm(page: import('@playwright/test').Page) {
  const resumeSection = page.locator('section', { hasText: 'Applicant Resume' });
  await resumeSection.getByPlaceholder('Jane Doe').fill('Alice Smith');

  const jobSection = page.locator('section', { hasText: 'Job Role' });
  await jobSection.getByPlaceholder('e.g. Senior Backend Engineer').fill('Software Engineer');
}

test.describe('Form submission flow', () => {
  test('shows polling status banner while queued and then displays results', async ({ page }) => {
    let pollCount = 0;

    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ jobId: MOCK_JOB_ID, status: 'Queued', message: 'Queued' }),
      });
    });

    await page.route(`**/api/resume/${MOCK_JOB_ID}`, async (route) => {
      pollCount++;
      const response = pollCount < 2 ? MOCK_QUEUED_STATUS : MOCK_COMPLETED_STATUS;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(response),
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    // Should show status/polling banner
    await expect(page.getByRole('status')).toBeVisible({ timeout: 5_000 });

    // Wait for analysis results to appear
    await expect(page.getByRole('heading', { name: 'Analysis Results' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('80%')).toBeVisible();
    await expect(page.getByText('Strong match. You meet most of the mandatory requirements.')).toBeVisible();
  });

  test('displays skill breakdown table in analysis results', async ({ page }) => {
    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ jobId: MOCK_JOB_ID, status: 'Queued', message: 'Queued' }),
      });
    });

    await page.route(`**/api/resume/${MOCK_JOB_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_COMPLETED_STATUS),
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    await expect(page.getByRole('heading', { name: 'Analysis Results' })).toBeVisible({ timeout: 15_000 });

    const table = page.locator('table.skill-table');
    await expect(table).toBeVisible();
    await expect(table.getByRole('cell', { name: 'REST API design' })).toBeVisible();
    await expect(table.getByRole('cell', { name: 'Unit testing' })).toBeVisible();
  });

  test('displays upskilling recommendations after completion', async ({ page }) => {
    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ jobId: MOCK_JOB_ID, status: 'Queued', message: 'Queued' }),
      });
    });

    await page.route(`**/api/resume/${MOCK_JOB_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_COMPLETED_STATUS),
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    await expect(page.getByRole('heading', { name: 'Upskilling Recommendations' })).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('.recommendation-card').getByRole('strong')).toHaveText('C# .NET');
    await expect(page.getByText('Medium effort')).toBeVisible();
  });

  test('navigates back to form after clicking Analyse another resume', async ({ page }) => {
    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ jobId: MOCK_JOB_ID, status: 'Queued', message: 'Queued' }),
      });
    });

    await page.route(`**/api/resume/${MOCK_JOB_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_COMPLETED_STATUS),
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    await expect(page.getByRole('heading', { name: 'Analysis Results' })).toBeVisible({ timeout: 15_000 });

    await page.getByRole('button', { name: '← Analyse another resume' }).click();

    await expect(page.getByRole('button', { name: 'Analyse Resume' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Applicant Resume' })).toBeVisible();
  });

  test('shows error banner when submission fails', async ({ page }) => {
    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'text/plain',
        body: 'Internal Server Error',
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('alert')).toContainText('Internal Server Error');
  });

  test('shows error banner when analysis job fails', async ({ page }) => {
    await page.route('**/api/resume', async (route) => {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ jobId: MOCK_JOB_ID, status: 'Queued', message: 'Queued' }),
      });
    });

    await page.route(`**/api/resume/${MOCK_JOB_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_FAILED_STATUS),
      });
    });

    await page.goto('/');
    await fillMinimalForm(page);
    await page.getByRole('button', { name: 'Analyse Resume' }).click();

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('alert')).toContainText('AI service unavailable');
  });

  test('Reset clears the form', async ({ page }) => {
    await page.goto('/');

    const resumeSection = page.locator('section', { hasText: 'Applicant Resume' });
    await resumeSection.getByPlaceholder('Jane Doe').fill('Alice Smith');

    await page.getByRole('button', { name: 'Reset' }).click();

    await expect(resumeSection.getByPlaceholder('Jane Doe')).toHaveValue('');
  });
});
