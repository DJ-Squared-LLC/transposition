import { test, expect } from '@playwright/test';

test.describe('Page shell', () => {
  test('renders the app header', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Transposition');
    await expect(page.getByText('Resume-to-role skill calibration')).toBeVisible();
  });

  test('renders both form sections on load', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Applicant Resume' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Job Role' })).toBeVisible();
  });

  test('Analyse Resume button is visible and initially enabled', async ({ page }) => {
    await page.goto('/');
    const btn = page.getByRole('button', { name: 'Analyse Resume' });
    await expect(btn).toBeVisible();
    await expect(btn).toBeEnabled();
  });

  test('Reset button is visible', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('button', { name: 'Reset' })).toBeVisible();
  });
});
