// Takes the README screenshot of the published report page.
import { chromium } from '@playwright/test';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1100, height: 700 }, deviceScaleFactor: 2 });
await page.goto('https://adriandeutsch.github.io/ShopGuard/', { waitUntil: 'networkidle' });
await page.waitForTimeout(800);
// The page is short; capture all of it, however many report cards it has.
await page.screenshot({ path: '../docs/images/live-report.png', fullPage: true });
await browser.close();
console.log('saved live-report.png');
