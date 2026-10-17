// Verifica links relativos (arquivo e âncora) em docs/**/*.md
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const files = [];
const walk = d => { for (const e of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, e.name); if (e.isDirectory()) { if (e.name !== '_obsoleto_v1') walk(p); } else if (p.endsWith('.md')) files.push(p); } };
walk(path.join(ROOT, 'docs'));
files.push(path.join(ROOT, 'CLAUDE.md'));

// âncoras no estilo GitHub
const slug = h => h.trim().toLowerCase().replace(/[`*]/g, '').replace(/[^\p{L}\p{N}\s_-]/gu, '').replace(/\s/g, '-');
const anchorsCache = {};
const anchors = f => {
  if (anchorsCache[f]) return anchorsCache[f];
  const s = new Set(); const count = {};
  for (const line of fs.readFileSync(f, 'utf8').split('\n')) {
    const m = line.match(/^#{1,6}\s+(.*)$/);
    if (m) { let a = slug(m[1]); if (count[a] !== undefined) { count[a]++; a = `${a}-${count[a]}`; } else count[a] = 0; s.add(a); }
  }
  return (anchorsCache[f] = s);
};

let erros = 0, total = 0;
for (const f of files) {
  const txt = fs.readFileSync(f, 'utf8').replace(/```[\s\S]*?```/g, '');
  for (const m of txt.matchAll(/\]\(([^)\s]+)\)/g)) {
    const href = m[1];
    if (/^(https?:|mailto:)/.test(href)) continue;
    total++;
    const [p, a] = href.split('#');
    const alvo = p ? path.resolve(path.dirname(f), decodeURI(p)) : f;
    if (!fs.existsSync(alvo)) { erros++; console.log(`ARQUIVO  ${path.relative(ROOT, f)} -> ${href}`); continue; }
    if (a && alvo.endsWith('.md') && !anchors(alvo).has(decodeURI(a))) { erros++; console.log(`ANCORA   ${path.relative(ROOT, f)} -> ${href}`); }
  }
}
console.log(`\n${files.length} arquivos, ${total} links, ${erros} quebrados`);
