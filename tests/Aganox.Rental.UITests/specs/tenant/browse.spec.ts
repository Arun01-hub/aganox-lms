import { test, expect } from '@playwright/test';
import { BrowsePage, PropertyDetailPage, ShellPage } from '../../pages';

test.describe('Tenant browse flow', () => {
  test('search by city filters property results', async ({ page }) => {
    const shell = new ShellPage(page);
    const browse = new BrowsePage(page);

    await page.goto('/');
    await shell.goToBrowse();
    await browse.expectLoaded();

    await browse.searchByCity('Portland');
    await expect(page).toHaveURL(/city=Portland/);

    const countText = await browse.getResultsCountText();
    expect(countText).toMatch(/property|properties found/);
    expect(await page.getByTestId('property-card').count()).toBeGreaterThan(0);
  });

  test('property detail page shows listing information', async ({ page }) => {
    const browse = new BrowsePage(page);
    const detail = new PropertyDetailPage(page);

    await page.goto('/browse');
    await browse.expectLoaded();
    await browse.openFirstProperty();

    await detail.expectLoaded();
    const title = await detail.getTitle();
    expect(title.length).toBeGreaterThan(0);
    await expect(page.getByTestId('property-detail-price')).toBeVisible();
    await expect(page.getByTestId('property-detail-description')).toBeVisible();
  });
});
