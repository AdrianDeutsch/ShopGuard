import { chromium } from '@playwright/test';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1200, height: 1000 } });
await page.goto('file:///tmp/covreport/index.html');
await page.waitForTimeout(1500);
await page.screenshot({ path: '../docs/images/coverage-report.png', fullPage: true });
await browser.close();
console.log('saved coverage-report.png');
