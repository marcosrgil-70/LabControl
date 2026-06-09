import { chromium } from 'playwright';

const browser = await chromium.launch({ channel: 'msedge', headless: false });
const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
const page = await context.newPage();

try {
    await page.goto('http://localhost:5050', { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'C:/Project/LabControl/screenshots/home.png' });
    console.log('home.png OK');

    await page.goto('http://localhost:5050/HistAmostras', { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'C:/Project/LabControl/screenshots/amostras.png' });
    console.log('amostras.png OK');

    await page.goto('http://localhost:5050/HistAmostras/Criar', { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'C:/Project/LabControl/screenshots/nova_amostra.png', fullPage: true });
    console.log('nova_amostra.png OK');

    await page.goto('http://localhost:5050/Propostas', { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'C:/Project/LabControl/screenshots/propostas.png' });
    console.log('propostas.png OK');
} finally {
    await browser.close();
}
console.log('Concluído!');
