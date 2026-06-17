import { chromium } from '@playwright/test';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1100, height: 430 } });
await page.goto('https://adriandeutsch.github.io/Qa_Automation/', { waitUntil: 'networkidle' });
await page.waitForTimeout(1200);
await page.screenshot({ path: '../docs/images/live-report.png', clip: { x: 0, y: 0, width: 1100, height: 410 } });
await browser.close();
console.log('saved tight live-report.png');
