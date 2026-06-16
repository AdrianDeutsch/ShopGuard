// Records a real end-to-end run (register off-camera, then login -> search ->
// cart -> checkout on-camera) and saves a .webm video. Convert to GIF afterwards.
import { chromium } from '@playwright/test';

const BASE = 'https://practicesoftwaretesting.com';
const VIDEO_DIR = new URL('../../docs/images/_video/', import.meta.url).pathname;
const email = `hero.${Date.now()}@example.com`;
const password = 'ShopGuard!2026x';

const pause = (page, ms = 900) => page.waitForTimeout(ms);

async function register(browser) {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await page.goto(`${BASE}/auth/register`);
  await page.locator("[data-test='first-name']").fill('Shop');
  await page.locator("[data-test='last-name']").fill('Guard');
  await page.locator("[data-test='dob']").fill('1990-01-15');
  await page.locator("[data-test='street']").fill('Teststraße');
  await page.locator("[data-test='house_number']").fill('1');
  await page.locator("[data-test='postal_code']").fill('10115');
  await page.locator("[data-test='city']").fill('Berlin');
  await page.locator("[data-test='state']").fill('Berlin');
  await page.locator("[data-test='country']").selectOption({ label: 'Germany' });
  await page.locator("[data-test='phone']").fill('03012345678');
  await page.locator("[data-test='email']").fill(email);
  await page.locator("[data-test='password']").fill(password);
  await page.locator("[data-test='register-submit']").click();
  await page.waitForURL('**/auth/login');
  await ctx.close();
}

async function recordJourney(browser) {
  const ctx = await browser.newContext({
    viewport: { width: 1280, height: 720 },
    recordVideo: { dir: VIDEO_DIR, size: { width: 1280, height: 720 } },
  });
  const page = await ctx.newPage();

  // 1) Login
  await page.goto(`${BASE}/auth/login`);
  await pause(page);
  await page.locator("[data-test='email']").fill(email);
  await page.locator("[data-test='password']").fill(password);
  await pause(page, 500);
  await page.locator("[data-test='login-submit']").click();
  await page.waitForURL('**/account');
  await pause(page);

  // 2) Search
  await page.goto(BASE);
  await page.locator("[data-test='search-query']").fill('Pliers');
  await pause(page, 500);
  await page.locator("[data-test='search-submit']").click();
  await page.getByText('Searched for:').waitFor();
  await page.locator('a.card').first().waitFor();
  await pause(page);

  // 3) Open product + add to cart
  await page.locator('a.card', { hasText: 'Combination Pliers' }).first().click();
  await page.locator("[data-test='add-to-cart']").waitFor();
  await pause(page);
  await page.locator("[data-test='add-to-cart']").click();
  await page.getByText('Product added to shopping cart').first().waitFor();
  await pause(page);

  // 4) Checkout wizard
  await page.goto(`${BASE}/checkout`);
  await page.locator("[data-test='product-title']").first().waitFor();
  await pause(page);
  await page.locator("[data-test='proceed-1']").click();
  await page.locator("[data-test='proceed-2']").waitFor();
  await pause(page, 600);
  await page.locator("[data-test='proceed-2']").click();
  const houseNumber = page.locator("input[data-test='house_number']");
  await houseNumber.waitFor();
  if (!(await houseNumber.inputValue())) await houseNumber.fill('1');
  await pause(page, 600);
  await page.locator("[data-test='proceed-3']").click();
  await page.locator("[data-test='payment-method']").waitFor();
  await pause(page, 600);
  await page.locator("[data-test='payment-method']").selectOption({ value: 'cash-on-delivery' });
  await pause(page, 500);
  await page.locator("[data-test='finish']").click();
  await page.locator("[data-test='payment-success-message']").first().waitFor();
  await pause(page, 600);
  await page.locator("[data-test='finish']", { hasText: 'Confirm' }).click();
  await page.getByText('Thanks for your order!').waitFor({ timeout: 60000 });
  await pause(page, 1800); // linger on the confirmation for the GIF

  await ctx.close(); // flushes the video file
}

const browser = await chromium.launch({ headless: true, slowMo: 120 });
try {
  console.log('registering throwaway account...');
  await register(browser);
  console.log('recording journey...');
  await recordJourney(browser);
  console.log('done; video in', VIDEO_DIR);
} finally {
  await browser.close();
}
