import { test, expect } from '@playwright/test';
import { IdentityLoginPage, LoginPage, ShellPage } from '../../pages';

test.describe('Authentication', () => {
  test('redirects unauthenticated users to login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('login-page')).toBeVisible();
  });

  test('user can sign in through Aganox Identity', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const identityPage = new IdentityLoginPage(page);

    await loginPage.goto();
    await loginPage.startSignIn();

    await page.waitForURL(/7036|5110/);
    await identityPage.signIn();
    await identityPage.acceptConsentIfShown();

    await page.waitForURL(/localhost:4200/);
    await new ShellPage(page).expectLoaded();
    await expect(page.getByTestId('home-page')).toBeVisible();
  });
});
