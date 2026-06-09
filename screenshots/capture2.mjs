import { chromium } from 'playwright';

const browser = await chromium.launch({ channel: 'msedge', headless: false });
const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
const page = await context.newPage();

const screens = [
    ['http://localhost:5050/Login', 'login.png'],
    ['http://localhost:5050/Clientes', 'clientes.png'],
    ['http://localhost:5050/Clientes/Criar', 'cliente_criar.png'],
    ['http://localhost:5050/TabelasAuxiliares', 'tabelas_aux.png'],
    ['http://localhost:5050/TabelasAuxiliares/AmostrasStatus', 'tabelas_status.png'],
    ['http://localhost:5050/Propostas/Criar', 'proposta_criar.png'],
    ['http://localhost:5050/Resultados', 'resultados.png'],
];

for (const [url, file] of screens) {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(1500);
    await page.screenshot({ path: `C:/Project/LabControl/screenshots/${file}`, fullPage: false });
    console.log(`✓ ${file}`);
}

await browser.close();
console.log('Concluído!');
