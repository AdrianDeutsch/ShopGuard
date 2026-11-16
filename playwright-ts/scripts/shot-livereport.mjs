// Takes the README screenshot of the published report page.
import { chromium } from '@playwright/test';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1100, height: 520 } });
await page.goto('https://adriandeutsch.github.io/ShopGuard/', { waitUntil: 'networkidle' });
await page.waitForTimeout(1200);
await page.screenshot({ path: '../docs/images/live-report.png', clip: { x: 0, y: 0, width: 1100, height: 500 } });
await browser.close();
console.log('saved live-report.png');
