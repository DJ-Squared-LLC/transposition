import { test, expect } from '@playwright/test';

test.describe('JobRoleForm', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('has role title, department, and description inputs', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });
    await expect(section.getByPlaceholder('e.g. Senior Backend Engineer')).toBeVisible();
    await expect(section.getByPlaceholder('e.g. Engineering')).toBeVisible();
    await expect(section.getByPlaceholder('Brief description of the role…')).toBeVisible();
  });

  test('fills in role title and department', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });
    await section.getByPlaceholder('e.g. Senior Backend Engineer').fill('Software Engineer II');
    await section.getByPlaceholder('e.g. Engineering').fill('Platform');

    await expect(section.getByPlaceholder('e.g. Senior Backend Engineer')).toHaveValue('Software Engineer II');
    await expect(section.getByPlaceholder('e.g. Engineering')).toHaveValue('Platform');
  });

  test('adds a skill requirement', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });

    await section.getByLabel('New skill').fill('REST API design');
    await section.getByLabel('New preferred tool').fill('C# .NET');
    await section.getByRole('button', { name: '+ Add' }).click();

    const table = section.locator('table.requirements-table');
    await expect(table).toBeVisible();
    await expect(table.getByRole('cell', { name: 'REST API design' })).toBeVisible();
    await expect(table.getByRole('cell', { name: 'C# .NET' })).toBeVisible();
    await expect(table.getByRole('cell', { name: 'Yes' })).toBeVisible();
  });

  test('adds an optional skill requirement', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });

    await section.getByLabel('New skill').fill('GraphQL');
    await section.getByLabel('New preferred tool').fill('Apollo');
    await section.getByLabel('Mandatory').uncheck();
    await section.getByRole('button', { name: '+ Add' }).click();

    const table = section.locator('table.requirements-table');
    await expect(table.getByRole('cell', { name: 'No' })).toBeVisible();
  });

  test('removes a skill requirement', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });

    await section.getByLabel('New skill').fill('Docker');
    await section.getByLabel('New preferred tool').fill('Docker');
    await section.getByRole('button', { name: '+ Add' }).click();

    await expect(section.getByRole('cell', { name: 'Docker' }).first()).toBeVisible();

    await section.getByRole('button', { name: 'Remove requirement' }).click();

    await expect(section.locator('table.requirements-table')).not.toBeVisible();
  });

  test('does not add requirement with empty skill or tool', async ({ page }) => {
    const section = page.locator('section', { hasText: 'Job Role' });

    // Only fill skill, not tool
    await section.getByLabel('New skill').fill('TypeScript');
    await section.getByRole('button', { name: '+ Add' }).click();

    await expect(section.locator('table.requirements-table')).not.toBeVisible();
  });
});
