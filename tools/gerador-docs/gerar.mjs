// Gera docs/banco/objetos/** a partir dos scripts v2 + metadados de negócio
import fs from 'node:fs';
import path from 'node:path';
import { loadAll, messages, refs, calls, sessionVars, setByTrigger, setByTriggerDetail } from './parse.mjs';
import { T, GRUPOS } from './meta_tabelas.mjs';
import { V, R, G } from './meta_outros.mjs';
import { TELAS, PROCESSOS } from './nomes.mjs';

import { fileURLToPath } from 'node:url';
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const OUT = path.join(ROOT, 'docs/banco/objetos');
const m = loadAll(path.join(ROOT, 'Scripts/v2'));
const tabNames = Object.keys(m.tables);
const viewNames = Object.keys(m.views);
const rotNames = Object.keys(m.routines);
const trgNames = Object.keys(m.triggers);

const CRIT = c => (c === 2 ? '🔴 Crítico' : c === 1 ? '🟠 Importante' : '⚪ Normal');
const AVISO = '> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).';
const VARS = {
  '@LC_ID_USUARIOS': 'usuário logado (gravado em logs); definido pelo interceptor de conexão a cada request',
  '@LC_MIGRACAO': '1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)',
  '@LC_PERMITE_RETROATIVO': '1 = libera data de entrega além de `DIAS_RETROATIVOS_ENTREGA` (só após confirmação + permissão `AMOSTRAS.ENTREGA_RETROATIVA`)',
  '@LC_DESFAZENDO': 'uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão',
};

const linkT = (b, base = '../../../telas/') => TELAS[b] ? `[${TELAS[b]}](${base}${b}.md)` : `\`${b}\``;
const linkP = (b, base = '../../../processos/') => PROCESSOS[b] ? `[${PROCESSOS[b]}](${base}${b}.md)` : `\`${b}\``;
const lnTab = (n, base = '../tabelas/') => `[\`${n}\`](${base}${n}.md)`;
const lnView = (n, base = '../views/') => `[\`${n}\`](${base}${n}.md)`;
const lnRot = (n, base = '../rotinas/') => `[\`${n}\`](${base}${n}.md)`;
const lnTrg = (n, base = '../triggers/') => `[\`${n}\`](${base}${n}.md)`;
const lnObj = n => (m.tables[n] ? lnTab(n) : m.views[n] ? lnView(n) : m.routines[n] ? lnRot(n) : m.triggers[n] ? lnTrg(n) : `\`${n}\``);
const list = (arr, f = x => x) => (arr.length ? arr.map(x => `- ${f(x)}`).join('\n') : '- —');
const esc = s => String(s ?? '').replace(/\|/g, '\\|');
const write = (rel, txt) => { const f = path.join(OUT, rel); fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, txt.replace(/\n{3,}/g, '\n\n'), 'utf8'); };

// --- índices cruzados ---
const trgByTable = {};
for (const g of Object.values(m.triggers)) (trgByTable[g.table] ||= []).push(g.name);
const usedBy = {}; // tabela/view -> {views, rotR, rotW, trgR, trgW}
const ensure = n => (usedBy[n] ||= { views: new Set(), rotR: new Set(), rotW: new Set(), trgR: new Set(), trgW: new Set() });
for (const v of Object.values(m.views)) { const r = refs(v.body.replace(/^CREATE OR REPLACE VIEW \w+/, ''), [...tabNames, ...viewNames]); for (const n of r.read) ensure(n).views.add(v.name); }
for (const r0 of Object.values(m.routines)) { const r = refs(r0.body, tabNames); r.read.forEach(n => ensure(n).rotR.add(r0.name)); r.write.forEach(n => ensure(n).rotW.add(r0.name)); }
for (const g of Object.values(m.triggers)) { const r = refs(g.body.replace(/ON \w+ FOR EACH ROW/, ''), tabNames); r.read.forEach(n => ensure(n).trgR.add(g.name)); r.write.forEach(n => ensure(n).trgW.add(g.name)); }
const calledBy = {}; // rotina -> {rot:Set, trg:Set, view:Set}
for (const r0 of Object.values(m.routines)) for (const c of calls(r0.body.replace(new RegExp(`^CREATE (FUNCTION|PROCEDURE) ${r0.name}`), ''), rotNames)) (calledBy[c] ||= { rot: new Set(), trg: new Set(), view: new Set() }).rot.add(r0.name);
for (const g of Object.values(m.triggers)) for (const c of calls(g.body, rotNames)) (calledBy[c] ||= { rot: new Set(), trg: new Set(), view: new Set() }).trg.add(g.name);
for (const v of Object.values(m.views)) for (const c of calls(v.body, rotNames)) (calledBy[c] ||= { rot: new Set(), trg: new Set(), view: new Set() }).view.add(v.name);
const fkIn = {};
for (const t of Object.values(m.tables)) for (const fk of t.fks) (fkIn[fk.ref] ||= []).push({ from: t.name, ...fk });

// --- TABELAS ---
function efNotes(t, meta, trgSet) {
  const n = [];
  const hasEmp = t.columns.some(c => c.name === 'ID_EMPRESAS');
  const globais = ['EMPRESAS', 'PAISES', 'ESTADOS', 'CIDADES', 'USUARIOS', 'PERMISSOES', 'AUDITORIA'];
  if (hasEmp && !globais.includes(t.name)) n.push('**Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.');
  if (trgSet.length) n.push(`**Preenchidas pelo banco (trigger):** ${trgSet.map(c => `\`${c}\``).join(', ')} — não depender do valor enviado; reler a linha após gravar.`);
  const ai = t.columns.filter(c => c.ai).map(c => c.name);
  if (ai.length) n.push(`Chave auto-incremento: ${ai.map(c => `\`${c}\``).join(', ')}.`);
  const bools = t.columns.filter(c => /^TINYINT\(1\)/i.test(c.type)).map(c => c.name);
  if (bools.length) n.push(`\`TINYINT(1)\` → \`bool\`: ${bools.map(c => `\`${c}\``).join(', ')}.`);
  const enums = t.columns.filter(c => /^ENUM/i.test(c.type));
  if (enums.length) n.push(`\`ENUM\` → enum C# com conversão para string: ${enums.map(c => `\`${c.name}\` ${c.type.replace(/^ENUM/i, '')}`).join('; ')}.`);
  const dt = t.columns.filter(c => /^DATE$/i.test(c.type)).map(c => c.name), tm = t.columns.filter(c => /^TIME$/i.test(c.type)).map(c => c.name);
  if (dt.length) n.push(`\`DATE\` → \`DateOnly\`: ${dt.map(c => `\`${c}\``).join(', ')}.`);
  if (tm.length) n.push(`\`TIME\` → \`TimeOnly\`: ${tm.map(c => `\`${c}\``).join(', ')}.`);
  const js = t.columns.filter(c => /JSON/i.test(c.type)).map(c => c.name);
  if (js.length) n.push(`\`JSON\` → \`string\` (ou \`JsonDocument\`): ${js.map(c => `\`${c}\``).join(', ')}.`);
  const bl = t.columns.filter(c => /BLOB/i.test(c.type)).map(c => c.name);
  if (bl.length) n.push(`Blob → \`byte[]\` carregado só quando necessário (não incluir nas listas): ${bl.map(c => `\`${c}\``).join(', ')}.`);
  const upd = t.columns.filter(c => c.onUpd || /CURRENT_TIMESTAMP/.test(c.def)).map(c => c.name);
  if (upd.length) n.push(`Datas mantidas pelo MySQL (\`DEFAULT/ON UPDATE CURRENT_TIMESTAMP\`): ${upd.map(c => `\`${c}\``).join(', ')} — mapear como gerado pelo banco.`);
  if (t.columns.some(c => c.name === 'CRIADO_POR')) n.push('`CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).');
  return [...n, ...(meta.ef || [])];
}

for (const t of Object.values(m.tables)) {
  const meta = T[t.name] || { desc: t.comment, legado: '—', vol: '—', mig: '—', telas: [], proc: [], critico: 0, regras: [] };
  const grupo = Object.entries(GRUPOS).find(([, a]) => a.includes(t.name))?.[0] || '—';
  const trgs = trgByTable[t.name] || [];
  const trgSetCols = {};
  const soLimpa = {};
  for (const g of trgs) { const gg = m.triggers[g]; if (gg.timing === 'BEFORE' && gg.event !== 'DELETE') for (const [c, rhs] of Object.entries(setByTriggerDetail(gg.body))) { (trgSetCols[c] ||= new Set()).add(g); soLimpa[c] = (soLimpa[c] ?? true) && rhs.every(x => x === 'NULL'); } }
  const filledBy = c => {
    if (c.ai) return 'auto-incremento';
    if (trgSetCols[c.name]) return soLimpa[c.name] ? `aplicação (${[...trgSetCols[c.name]].join(', ')} limpa → NULL)` : [...trgSetCols[c.name]].map(g => `trigger ${g}`).join(', ');
    if (/CURRENT_TIMESTAMP|CURRENT_DATE/.test(c.def) || c.onUpd) return 'banco';
    return 'aplicação';
  };
  const u = usedBy[t.name] || ensure(t.name);
  const cols = t.columns.map((c, i) => `| ${i + 1} | \`${c.name}\` | ${esc(c.type)} | ${c.notNull ? 'não' : 'sim'} | ${esc(c.def) || ''} | ${filledBy(c)} | ${esc(c.comment)} |`).join('\n');
  const md = `# ${t.name}

${meta.desc}

**Grupo:** ${grupo} · **Criticidade:** ${CRIT(meta.critico)} · **Script:** \`Scripts/v2/01_schema.sql\` · **Migração:** ${meta.mig}

${AVISO}

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| ${meta.legado} | ${meta.vol} |
${t.comment ? `\nComentário do script: _${t.comment}_\n` : ''}${t.notes.length ? `\nNotas do script: ${t.notes.map(x => `_${x}_`).join(' · ')}\n` : ''}
## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
${cols}

## Chaves, restrições e índices
- **PK:** ${t.pk.map(c => `\`${c}\``).join(', ') || '—'}
${t.uniques.map(q => `- **Única** \`${q.name}\` (${q.cols.join(', ')})${q.comment ? ` — ${q.comment}` : ''}`).join('\n')}
${t.checks.map(q => `- **Check** \`${q.name}\`: \`${q.expr}\``).join('\n')}
${t.indexes.map(q => `- **Índice** \`${q.name}\` (${q.cols.join(', ')})`).join('\n')}

## Relacionamentos
### Referencia
${t.fks.length ? `| Coluna | Tabela | Coluna destino | Ao excluir o pai |\n|---|---|---|---|\n${t.fks.map(f => `| ${f.cols.map(c => `\`${c}\``).join(', ')} | ${lnTab(f.ref)} | ${f.refCols.join(', ')} | ${f.onDelete || 'bloqueia'}${f.adiada ? ' (FK adiada, fim do script)' : ''} |`).join('\n')}` : '—'}

### É referenciada por
${(fkIn[t.name] || []).length ? `| Tabela | Coluna | Ao excluir esta |\n|---|---|---|\n${fkIn[t.name].map(f => `| ${lnTab(f.from)} | ${f.cols.join(', ')} | ${f.onDelete || 'bloqueia'} |`).join('\n')}` : '—'}

## Triggers
${trgs.length ? trgs.map(g => `- ${lnTrg(g)} — ${m.triggers[g].timing} ${m.triggers[g].event}: ${G[g]?.desc || m.triggers[g].comment}`).join('\n') : '- —'}

## Usada por
- **Views:** ${[...u.views].map(x => lnView(x)).join(', ') || '—'}
- **Rotinas que alteram:** ${[...u.rotW].map(x => lnRot(x)).join(', ') || '—'}
- **Rotinas que leem:** ${[...u.rotR].map(x => lnRot(x)).join(', ') || '—'}
- **Triggers de outras tabelas que alteram:** ${[...u.trgW].filter(g => m.triggers[g].table !== t.name).map(x => lnTrg(x)).join(', ') || '—'}
- **Triggers de outras tabelas que leem:** ${[...u.trgR].filter(g => m.triggers[g].table !== t.name).map(x => lnTrg(x)).join(', ') || '—'}

## Telas e processos
- **Telas:** ${meta.telas.map(x => linkT(x)).join(' · ') || '—'}
- **Processos:** ${meta.proc.map(x => linkP(x)).join(' · ') || '—'}

## Regras de negócio
${list(meta.regras)}

## Implementação no C# (EF Core)
${list(efNotes(t, meta, Object.keys(trgSetCols).filter(c => !soLimpa[c])))}

## Definição (DDL)
\`\`\`sql
${t.ddl}
\`\`\`
`;
  write(`tabelas/${t.name}.md`, md);
}

// --- VIEWS ---
function viewCols(body) {
  const sel = body.replace(/^CREATE OR REPLACE VIEW \w+ AS\n/, '').match(/^SELECT ([\s\S]*?)\n\s*FROM /);
  if (!sel) return [];
  const s = sel[1]; const out = []; let depth = 0, cur = '';
  for (const ch of s) { if (ch === '(') depth++; if (ch === ')') depth--; if (ch === ',' && depth === 0) { out.push(cur); cur = ''; } else cur += ch; }
  out.push(cur);
  return out.map(x => x.trim().replace(/\s+/g, ' ')).map(x => { const a = x.match(/\bAS (\w+)$/i); if (a) return { name: a[1], expr: x.replace(/\s+AS \w+$/i, '') }; const d = x.match(/(\w+)$/); return { name: d ? d[1] : x, expr: x }; });
}
for (const v of Object.values(m.views)) {
  const meta = V[v.name] || { desc: v.comment, legado: '—', telas: [], notas: [], critico: 0 };
  const src = refs(v.body.replace(/^CREATE OR REPLACE VIEW \w+/, ''), [...tabNames, ...viewNames.filter(x => x !== v.name)]);
  const fns = calls(v.body, rotNames);
  const cs = viewCols(v.body);
  const md = `# ${v.name}

${meta.desc}

**Tipo:** view · **Criticidade:** ${CRIT(meta.critico)} · **Script:** \`Scripts/v2/04_views.sql\`

${AVISO}

## Legado
${meta.legado}
${v.comment ? `\nComentário do script: _${v.comment}_\n` : ''}
## Colunas
| Coluna | Expressão |
|---|---|
${cs.map(c => `| \`${c.name}\` | \`${esc(c.expr).slice(0, 160)}\` |`).join('\n')}

## Origem dos dados
- **Tabelas/views:** ${[...src.read].map(x => lnObj(x)).join(', ')}
- **Funções:** ${fns.map(x => lnRot(x)).join(', ') || '—'}

## Telas que usam
${list(meta.telas, x => linkT(x))}

## Notas de uso
${list(['Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).', ...(meta.notas || [])])}

## Definição
\`\`\`sql
${v.body}
\`\`\`
`;
  write(`views/${v.name}.md`, md);
}

// --- ROTINAS ---
function params(sig) {
  const ini = sig.indexOf('('); let d = 0, fim = ini; for (let i = ini; i < sig.length; i++) { if (sig[i] === '(') d++; if (sig[i] === ')') { d--; if (d === 0) { fim = i; break; } } }
  const inside = sig.slice(ini + 1, fim);
  return inside.split(/,(?![^(]*\))/).map(p => p.trim()).filter(Boolean).map(p => { const mm = p.match(/^(IN|OUT|INOUT)?\s*(\w+)\s+(.+)$/i); return mm ? { dir: (mm[1] || 'IN').toUpperCase(), name: mm[2], type: mm[3] } : { dir: 'IN', name: p, type: '' }; });
}
function csharp(r, ps) {
  const ins = ps.filter(p => p.dir === 'IN'), outs = ps.filter(p => p.dir !== 'IN');
  if (r.kind === 'FUNCTION') return `// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:\nvar valor = await db.Database.SqlQueryRaw<string>("SELECT ${r.name}(${ins.map((_, i) => '{' + i + '}').join(', ')}) AS Value", ${ins.map(p => p.name.replace(/^p_/, '')).join(', ')}).FirstAsync();`;
  const lines = [];
  lines.push(`// ${r.name.startsWith('SP_') && !['SP_PROXIMO_NUMERO', 'SP_RECALCULAR_PROPOSTA', 'SP_AMOSTRA_GERAR_TESTES', 'SP_INICIALIZAR_EMPRESA'].includes(r.name) ? 'Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().' : 'Sem transação própria: pode ser chamada dentro da transação da aplicação.'}`);
  lines.push('await using var cmd = db.Database.GetDbConnection().CreateCommand();');
  lines.push(`cmd.CommandText = "${r.name}";`);
  lines.push('cmd.CommandType = CommandType.StoredProcedure;');
  for (const p of ins) lines.push(`cmd.Parameters.Add(new MySqlParameter("${p.name}", ${p.name.replace(/^p_/, '')}));`);
  for (const p of outs) lines.push(`var ${p.name.replace(/^p_/, 'o_')} = new MySqlParameter("${p.name}", MySqlDbType.${/INT/i.test(p.type) ? 'Int32' : 'VarChar'}) { Direction = ParameterDirection.Output };\ncmd.Parameters.Add(${p.name.replace(/^p_/, 'o_')});`);
  lines.push('if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();');
  lines.push('try { await cmd.ExecuteNonQueryAsync(); }');
  lines.push('catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio');
  return lines.join('\n');
}
for (const r of Object.values(m.routines)) {
  const meta = R[r.name] || { desc: r.comment, legado: '—', uso: '—', proc: [], critico: 0 };
  const ps = params(r.signature);
  const rr = refs(r.body.replace(new RegExp(`^CREATE (FUNCTION|PROCEDURE) ${r.name}`), ''), tabNames);
  const cs = calls(r.body.replace(new RegExp(`^CREATE (FUNCTION|PROCEDURE) ${r.name}`), ''), rotNames);
  const cb = calledBy[r.name] || { rot: new Set(), trg: new Set(), view: new Set() };
  const msgs = messages(r.body);
  const vars = sessionVars(r.body);
  const tipo = r.kind === 'FUNCTION' ? 'Função' : r.name.startsWith('PR_') ? 'Procedure interna (PR_)' : 'Procedure pública (SP_)';
  const md = `# ${r.name}

${meta.desc}

**Tipo:** ${tipo} · **Criticidade:** ${CRIT(meta.critico)} · **Script:** \`Scripts/v2/02_funcoes_procedures.sql\`

${AVISO}

## Substitui no legado
${meta.legado}
${r.comment ? `\nComentário do script: _${r.comment}_\n` : ''}
## Assinatura
\`\`\`sql
${r.signature}
\`\`\`

| Direção | Parâmetro | Tipo |
|---|---|---|
${ps.map(p => `| ${p.dir} | \`${p.name}\` | ${esc(p.type)} |`).join('\n')}

## Quem chama
- **Uso previsto:** ${meta.uso}
- **Chamada por rotinas:** ${[...cb.rot].map(x => lnRot(x)).join(', ') || '—'}
- **Chamada por triggers:** ${[...cb.trg].map(x => lnTrg(x)).join(', ') || '—'}
- **Usada em views:** ${[...cb.view].map(x => lnView(x)).join(', ') || '—'}
${meta.transacao ? `\n## Transação\n${meta.transacao}\n` : ''}
## O que acessa
- **Altera:** ${[...rr.write].map(x => lnTab(x)).join(', ') || '—'}
- **Lê:** ${[...rr.read].map(x => lnTab(x)).join(', ') || '—'}
- **Chama:** ${cs.map(x => lnRot(x)).join(', ') || '—'}
- **Variáveis de sessão:** ${vars.length ? vars.map(v => `\`${v}\` (${VARS[v] || ''})`).join('; ') : '—'}

## Mensagens de negócio (SQLSTATE 45000)
${msgs.length ? msgs.map(x => `- "${x}"`).join('\n') : '- — (não lança mensagens próprias)'}

## Processos
${list(meta.proc || [], x => linkP(x))}
${(meta.notas || []).length ? `\n## Notas\n${list(meta.notas)}\n` : ''}
## Como chamar no C#
\`\`\`csharp
${csharp(r, ps)}
\`\`\`

## Código
\`\`\`sql
${r.body}
\`\`\`
`;
  write(`rotinas/${r.name}.md`, md);
}

// --- TRIGGERS ---
for (const g of Object.values(m.triggers)) {
  const meta = G[g.name] || { desc: g.comment, legado: '—' };
  const set = g.timing === 'BEFORE' ? setByTrigger(g.body) : [];
  const msgs = messages(g.body);
  const vars = sessionVars(g.body);
  const rr = refs(g.body.replace(/ON \w+ FOR EACH ROW/, ''), tabNames.filter(x => x !== g.table));
  const cs = calls(g.body, rotNames);
  const tmeta = T[g.table] || {};
  const md = `# ${g.name}

${meta.desc}

**Tabela:** ${lnTab(g.table)} · **Momento:** ${g.timing} ${g.event} · **Criticidade:** ${CRIT(meta.critico ?? 0)} · **Script:** \`Scripts/v2/03_triggers.sql\`

${AVISO}

## Substitui no legado
${meta.legado}
${g.comment ? `\nComentário do script: _${g.comment}_\n` : ''}
## Colunas que a trigger preenche/ajusta
${set.length ? set.map(c => `- \`NEW.${c}\``).join('\n') : '- —'}

## Validações (mensagens SQLSTATE 45000)
${msgs.length ? msgs.map(x => `- "${x}"`).join('\n') : '- — (não recusa operações)'}

## Variáveis de sessão
${vars.length ? vars.map(v => `- \`${v}\` — ${VARS[v] || ''}`).join('\n') : '- —'}

## O que acessa
- **Altera outras tabelas:** ${[...rr.write].map(x => lnTab(x)).join(', ') || '—'}
- **Lê outras tabelas:** ${[...rr.read].map(x => lnTab(x)).join(', ') || '—'}
- **Chama:** ${cs.map(x => lnRot(x)).join(', ') || '—'}

## Impacto na aplicação
${list([
    ...(set.length ? [`A aplicação não precisa (e não deve depender de) enviar: ${set.map(c => `\`${c}\``).join(', ')}. Reler a linha depois de gravar.`] : []),
    ...(msgs.length ? ['Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).'] : []),
    ...(vars.includes('@LC_MIGRACAO') ? ['Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.'] : []),
  ])}

## Telas e processos relacionados
- **Telas:** ${(tmeta.telas || []).map(x => linkT(x)).join(' · ') || '—'}
- **Processos:** ${(tmeta.proc || []).map(x => linkP(x)).join(' · ') || '—'}

## Código
\`\`\`sql
${g.body}
\`\`\`
`;
  write(`triggers/${g.name}.md`, md);
}

// --- ÍNDICE ---
let idx = `# Objetos do banco v2 — um MD por objeto

> Banco \`labcontrol_v2\` (MySQL 8). Fonte da verdade: \`Scripts/v2/01..05\`. Modelo e decisões: [../11-MODELO-MYSQL.md](../11-MODELO-MYSQL.md).
> Cada arquivo traz: finalidade, origem no legado, colunas, chaves, relacionamentos, triggers, quem usa, telas, processos, regras de negócio, cuidados no C# e o código SQL.
> Legenda: 🔴 crítico · 🟠 importante · ⚪ normal.

**Totais:** ${tabNames.length} tabelas · ${viewNames.length} views · ${rotNames.filter(n => m.routines[n].kind === 'FUNCTION').length} funções · ${rotNames.filter(n => m.routines[n].kind === 'PROCEDURE').length} procedures · ${trgNames.length} triggers.

## Tabelas
`;
for (const [g, arr] of Object.entries(GRUPOS)) {
  idx += `\n### ${g}\n| Tabela | Crit. | Finalidade | Legado (registros) |\n|---|---|---|---|\n`;
  for (const n of arr) { const t = T[n]; idx += `| ${lnTab(n, 'tabelas/')} | ${CRIT(t.critico).split(' ')[0]} | ${esc(t.desc)} | ${esc(t.legado)} (${esc(t.vol)}) |\n`; }
}
idx += `\n## Views\n| View | Crit. | Finalidade |\n|---|---|---|\n${viewNames.map(n => `| ${lnView(n, 'views/')} | ${CRIT(V[n]?.critico ?? 0).split(' ')[0]} | ${esc(V[n]?.desc)} |`).join('\n')}\n`;
idx += `\n## Funções e procedures\n| Rotina | Tipo | Crit. | Finalidade |\n|---|---|---|---|\n${rotNames.map(n => `| ${lnRot(n, 'rotinas/')} | ${m.routines[n].kind === 'FUNCTION' ? 'Função' : n.startsWith('PR_') ? 'Interna' : 'Pública'} | ${CRIT(R[n]?.critico ?? 0).split(' ')[0]} | ${esc(R[n]?.desc)} |`).join('\n')}\n`;
idx += `\n## Triggers (por tabela)\n| Tabela | Trigger | Momento | O que faz |\n|---|---|---|---|\n`;
for (const t of tabNames) for (const g of trgByTable[t] || []) idx += `| ${lnTab(t, 'tabelas/')} | ${lnTrg(g, 'triggers/')} | ${m.triggers[g].timing} ${m.triggers[g].event} | ${esc(G[g]?.desc)} |\n`;
idx += `\n## Objetos do legado que não existem no v2\n- **Tabelas vazias não migradas:** módulos de Compras (\`CMP_*\`), Fiscal (\`FSC_*\`), Contábil e acessórios de produto — ver [../10-ANALISE-FIREBIRD.md §2.4](../10-ANALISE-FIREBIRD.md).\n- **Tabelas incorporadas:** \`ENTIDADES_PF/PJ/OBSERVACOES\` → \`ENTIDADES\`; 5 textos da proposta → \`LAB_PROPOSTAS_TEXTOS\`; \`LAB_BOLETIM_OBS\` → \`LAB_BOLETIM_ANALISES\`; \`LAB_HIST_AMOSTRAS_OBSERVACAO\` → \`LAB_HIST_AMOSTRAS.TX_OBSERVACAO\`; \`LOGRADOUROS\`/\`CIDADES_BAIRROS\` → texto em \`ENTIDADES_ENDERECOS\`; \`SYS_TABELAS_ID\` e \`EMPRESAS_PARAMETROS.ULT_*\` → \`SEQUENCIAS_DOCUMENTOS\`; \`SYS_*\`/\`ACESSO\`/\`ACOES\`/\`FORMS\` → \`PERMISSOES\`/\`PERFIS\`.\n- **Procedures descartadas:** \`SP_GEN_*\` (auto-increment), \`SP_ULT_REVISAO_*\` (lógica nas procedures de desfazer), \`VAI\` (manutenção pontual).\n- **Views descartadas:** \`VW_DUPLICADO\` (diagnóstico), \`VW_LAB_AMOSTRAS_PROP\`, \`VW_FORNECEDORES\`, \`VW_TRANSPORTADOR\`.\n`;
write('README.md', idx);
console.log('ok', tabNames.length, viewNames.length, rotNames.length, trgNames.length);
