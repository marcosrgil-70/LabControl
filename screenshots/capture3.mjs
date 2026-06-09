import { chromium } from 'playwright';

const browser = await chromium.launch({ channel: 'msedge', headless: false });
const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
const page = await context.newPage();

const screens = [
    ['http://localhost:5050/Produtos',         'produtos_lista.png'],
    ['http://localhost:5050/Produtos/Criar',   'produtos_criar.png'],
    ['http://localhost:5050/Parametros',       'parametros_lista.png'],
    ['http://localhost:5050/Parametros/Criar', 'parametros_criar.png'],
];

for (const [url, file] of screens) {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(1500);
    await page.screenshot({ path: `C:/Project/LabControl/screenshots/${file}` });
    console.log(`✓ ${file}`);
}

await browser.close();
