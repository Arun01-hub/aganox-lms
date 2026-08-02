import { test, expect } from '@playwright/test';
import { config } from '../../utils/config';
import { HomePage, ShellPage } from '../../pages';

test.describe('Smoke', () => {
  test('API health endpoint responds', async ({ request }) => {
    const response = await request.get(`${config.apiBaseUrl}/health`, {
      ignoreHTTPSErrors: true
    });

    expect(response.ok()).toBeTruthy();
    expect(await response.text()).toContain('Healthy');
  });

  test('home page loads with featured listings', async ({ page }) => {
    const home = new HomePage(page);
    await home.goto();

    await expect(page.getByTestId('featured-listings')).toBeVisible();
    await expect(page.getByTestId('property-card')).not.toHaveCount(0);
  });

  test('shell navigation links are visible', async ({ page }) => {
    await page.goto('/');
    const shell = new ShellPage(page);
    await shell.expectLoaded();

    await expect(page.getByTestId('nav-browse')).toBeVisible();
    await expect(page.getByTestId('nav-owner')).toBeVisible();
  });
});
