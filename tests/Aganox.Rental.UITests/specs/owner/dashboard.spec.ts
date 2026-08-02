import { test, expect } from '@playwright/test';
import { OwnerDashboardPage, ShellPage } from '../../pages';

test.describe('Owner dashboard', () => {
  test('owner can open listings dashboard and add property form', async ({ page }) => {
    const shell = new ShellPage(page);
    const owner = new OwnerDashboardPage(page);

    await page.goto('/');
    await shell.goToOwnerDashboard();
    await owner.openAddProperty();

    await expect(page.getByTestId('property-form-page')).toBeVisible();
    await expect(page.getByTestId('property-form-title')).toBeVisible();
  });

  test('owner dashboard shows listings table or empty state', async ({ page }) => {
    const owner = new OwnerDashboardPage(page);
    await owner.goto();

    const table = page.getByTestId('owner-listings-table');
    const empty = page.getByTestId('owner-empty-state');

    await expect(table.or(empty)).toBeVisible();
  });
});
