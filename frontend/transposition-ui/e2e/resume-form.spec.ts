import { test, expect } from '@playwright/test';

test.describe('ResumeForm', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('has applicant name, email, and summary inputs', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Applicant Resume' });
    await expect(section.getByPlaceholder('Jane Doe')).toBeVisible();
    await expect(section.getByPlaceholder('jane@example.com')).toBeVisible();
    await expect(section.getByPlaceholder('Professional summary…')).toBeVisible();
  });

  test('fills in applicant name and email', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Applicant Resume' });
    await section.getByPlaceholder('Jane Doe').fill('Alice Smith');
    await section.getByPlaceholder('jane@example.com').fill('alice@example.com');

    await expect(section.getByPlaceholder('Jane Doe')).toHaveValue('Alice Smith');
    await expect(section.getByPlaceholder('jane@example.com')).toHaveValue('alice@example.com');
  });

  test('adds an experience entry', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Applicant Resume' });

    await section.getByPlaceholder('e.g. Built REST APIs serving 10k RPS').fill('Led backend migrations');
    await section.getByPlaceholder('e.g. Acme Corp').fill('Acme Corp');
    await section.getByPlaceholder('e.g. REST API design, Unit testing').fill('REST API design, TDD');
    await section.getByPlaceholder('e.g. Node.js, Jest').fill('Node.js, Jest');

    // Blur skill/tool fields to commit values
    await section.getByPlaceholder('e.g. REST API design, Unit testing').blur();
    await section.getByPlaceholder('e.g. Node.js, Jest').blur();

    await section.getByRole('button', { name: '+ Add Experience' }).click();

    await expect(section.getByText('Led backend migrations')).toBeVisible();
    await expect(section.getByText('Acme Corp')).toBeVisible();
  });

  test('removes an experience entry', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Applicant Resume' });

    await section.getByPlaceholder('e.g. Built REST APIs serving 10k RPS').fill('Test experience');
    await section.getByRole('button', { name: '+ Add Experience' }).click();

    await expect(section.getByText('Test experience')).toBeVisible();

    await section.getByRole('button', { name: 'Remove experience' }).click();

    await expect(section.getByText('Test experience')).not.toBeVisible();
  });

  test('does not add experience with empty description', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Applicant Resume' });

    const experienceCards = section.locator('.experience-card');
    const initialCount = await experienceCards.count();

    await section.getByRole('button', { name: '+ Add Experience' }).click();

    await expect(experienceCards).toHaveCount(initialCount);
  });
});
