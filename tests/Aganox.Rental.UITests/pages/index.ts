import { Page, expect } from '@playwright/test';
import { config } from '../utils/config';

export class IdentityLoginPage {
  constructor(private readonly page: Page) {}

  async signIn(email = config.testUser.email, password = config.testUser.password): Promise<void> {
    await this.page.locator('input[name="Input.Email"]').fill(email);
    await this.page.locator('input[name="Input.Password"]').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }

  async acceptConsentIfShown(): Promise<void> {
    const accept = this.page.getByRole('button', { name: 'Accept' });
    if (await accept.isVisible({ timeout: 4_000 }).catch(() => false)) {
      await accept.click();
    }
  }
}

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/login');
    await expect(this.page.getByTestId('login-page')).toBeVisible();
  }

  async startSignIn(): Promise<void> {
    await this.page.getByTestId('login-sign-in-button').click();
  }
}

export class ShellPage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page.getByTestId('app-shell')).toBeVisible();
  }

  async goToBrowse(): Promise<void> {
    await this.page.getByTestId('nav-browse').click();
    await expect(this.page).toHaveURL(/\/browse/);
  }

  async goToOwnerDashboard(): Promise<void> {
    await this.page.getByTestId('nav-owner').click();
    await expect(this.page).toHaveURL(/\/owner$/);
  }

  async signOut(): Promise<void> {
    await this.page.getByTestId('nav-sign-out').click();
    await expect(this.page).toHaveURL(/\/login/);
  }
}

export class HomePage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/');
    await expect(this.page.getByTestId('home-page')).toBeVisible();
  }

  async openBrowseFromHero(): Promise<void> {
    await this.page.getByTestId('hero-browse').click();
    await expect(this.page).toHaveURL(/\/browse/);
  }
}

export class BrowsePage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page.getByTestId('browse-page')).toBeVisible();
  }

  async searchByCity(city: string): Promise<void> {
    await this.page.getByTestId('search-city').fill(city);
    await this.page.getByTestId('search-submit').click();
  }

  async getResultsCountText(): Promise<string> {
    return (await this.page.getByTestId('browse-results-count').innerText()).trim();
  }

  async openFirstProperty(): Promise<void> {
    await this.page.getByTestId('property-card').first().locator('a').first().click();
    await expect(this.page).toHaveURL(/\/properties\//);
  }
}

export class PropertyDetailPage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page.getByTestId('property-detail-page')).toBeVisible();
  }

  async getTitle(): Promise<string> {
    return (await this.page.getByTestId('property-detail-title').innerText()).trim();
  }
}

export class OwnerDashboardPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/owner');
    await expect(this.page.getByTestId('owner-dashboard-page')).toBeVisible();
  }

  async openAddProperty(): Promise<void> {
    await this.page.getByTestId('owner-add-property').click();
    await expect(this.page).toHaveURL(/\/owner\/new/);
  }
}

export async function loginThroughIdentity(page: Page): Promise<void> {
  const loginPage = new LoginPage(page);
  const identityPage = new IdentityLoginPage(page);

  await loginPage.goto();
  await loginPage.startSignIn();

  await page.waitForURL(url => url.href.includes('7036') || url.href.includes('5110'));
  await identityPage.signIn();
  await identityPage.acceptConsentIfShown();

  await page.waitForURL(/localhost:4200/);
  await new ShellPage(page).expectLoaded();
}
