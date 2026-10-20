// Parser dos scripts Scripts/v2/*.sql -> modelo de objetos
import fs from 'node:fs';
import path from 'node:path';

const KW = /\s+(NOT NULL|NULL|DEFAULT|AUTO_INCREMENT|PRIMARY KEY|COMMENT|ON UPDATE)\b/i;

function leadingComments(text, idx) {
  // comentários "-- " imediatamente antes de idx (ignora linhas de ====)
  const before = text.slice(0, idx).split('\n');
  before.pop(); // linha atual (vazia até o CREATE)
  const out = [];
  for (let i = before.length - 1; i >= 0; i--) {
    const l = before[i].trim();
    if (l.startsWith('--')) {
      const c = l.replace(/^--\s?/, '');
      if (/^[=-]{5,}/.test(c)) { if (out.length) break; else continue; }
      out.unshift(c);
    } else if (l === '') { if (out.length) break; }
    else break;
  }
  return out.join(' ').trim();
}

function cols(s) { return s.split(',').map(x => x.trim()).filter(Boolean); }

export function parseSchema(sql) {
  const tables = {};
  const re = /CREATE TABLE (\w+) \(\n([\s\S]*?)\n\) ENGINE=InnoDB;/g;
  let m;
  while ((m = re.exec(sql))) {
    const name = m[1];
    const t = { name, comment: leadingComments(sql, m.index), columns: [], pk: [], uniques: [], checks: [], fks: [], indexes: [], notes: [], ddl: m[0] };
    for (let raw of m[2].split('\n')) {
      let l = raw.trim();
      if (!l) continue;
      if (l.startsWith('--')) { t.notes.push(l.replace(/^--\s?/, '')); continue; }
      l = l.replace(/,$/, '');
      let mm;
      if ((mm = l.match(/^PRIMARY KEY \(([^)]+)\)/))) { t.pk = cols(mm[1]); continue; }
      if ((mm = l.match(/^CONSTRAINT (\w+) UNIQUE \(([^)]+)\)(?: COMMENT '([^']*)')?/))) { t.uniques.push({ name: mm[1], cols: cols(mm[2]), comment: mm[3] || '' }); continue; }
      if ((mm = l.match(/^CONSTRAINT (\w+) CHECK \((.*)\)$/))) { t.checks.push({ name: mm[1], expr: mm[2] }); continue; }
      if ((mm = l.match(/^CONSTRAINT (\w+) FOREIGN KEY \(([^)]+)\) REFERENCES (\w+) \(([^)]+)\)(?: ON DELETE (CASCADE|SET NULL|RESTRICT))?/))) {
        t.fks.push({ name: mm[1], cols: cols(mm[2]), ref: mm[3], refCols: cols(mm[4]), onDelete: mm[5] || '' }); continue;
      }
      if ((mm = l.match(/^INDEX (\w+) \(([^)]+)\)/))) { t.indexes.push({ name: mm[1], cols: cols(mm[2]) }); continue; }
      if ((mm = l.match(/^(\w+)\s+(.+)$/))) {
        const cname = mm[1];
        let rest = mm[2];
        let comment = '';
        const cm = rest.match(/COMMENT '((?:[^']|'')*)'/);
        if (cm) { comment = cm[1]; rest = rest.replace(cm[0], ''); }
        const k = rest.search(KW);
        const type = (k >= 0 ? rest.slice(0, k) : rest).trim();
        const flags = k >= 0 ? rest.slice(k) : '';
        const notNull = /NOT NULL/i.test(flags) || /PRIMARY KEY/i.test(flags);
        const dm = flags.match(/DEFAULT\s+(\([^)]*\)\)?|'[^']*'|\S+)/i);
        const def = dm ? dm[1] : '';
        const ai = /AUTO_INCREMENT/i.test(flags);
        const pkInline = /PRIMARY KEY/i.test(flags);
        const onUpd = /ON UPDATE CURRENT_TIMESTAMP/i.test(flags);
        if (pkInline) t.pk = [cname];
        t.columns.push({ name: cname, type, notNull, def, ai, comment, onUpd });
      }
    }
    tables[name] = t;
  }
  // FKs adiadas
  const reAlt = /ALTER TABLE (\w+)\n([\s\S]*?);/g;
  while ((m = reAlt.exec(sql))) {
    const t = tables[m[1]];
    const reFk = /ADD CONSTRAINT (\w+)\s+FOREIGN KEY \((\w+)\)\s+REFERENCES (\w+) \((\w+)\)/g;
    let f;
    while ((f = reFk.exec(m[2]))) t.fks.push({ name: f[1], cols: [f[2]], ref: f[3], refCols: [f[4]], onDelete: '', adiada: true });
  }
  return tables;
}

export function parseRoutines(sql) {
  const out = {};
  const re = /CREATE (FUNCTION|PROCEDURE) (\w+)\(([\s\S]*?)\n?BEGIN\n([\s\S]*?)\nEND\$\$/g;
  let m;
  while ((m = re.exec(sql))) {
    const sigFull = sql.slice(m.index, m.index + m[0].indexOf('\nBEGIN'));
    out[m[2]] = { name: m[2], kind: m[1], comment: leadingComments(sql, m.index), signature: sigFull.trim(), body: m[0].replace(/END\$\$$/, 'END') };
  }
  return out;
}

export function parseTriggers(sql) {
  const out = {};
  const re = /CREATE TRIGGER (\w+) (BEFORE|AFTER) (INSERT|UPDATE|DELETE) ON (\w+) FOR EACH ROW\n([\s\S]*?)\nEND\$\$/g;
  let m;
  while ((m = re.exec(sql))) {
    out[m[1]] = { name: m[1], timing: m[2], event: m[3], table: m[4], comment: leadingComments(sql, m.index), body: m[0].replace(/END\$\$$/, 'END') };
  }
  return out;
}

export function parseViews(sql) {
  const out = {};
  const re = /CREATE OR REPLACE VIEW (\w+) AS\n([\s\S]*?);/g;
  let m;
  while ((m = re.exec(sql))) out[m[1]] = { name: m[1], comment: leadingComments(sql, m.index), body: m[0] };
  return out;
}

export function messages(body) {
  const r = [];
  const re = /MESSAGE_TEXT = '([^']*)'/g;
  let m;
  while ((m = re.exec(body))) if (!r.includes(m[1])) r.push(m[1]);
  return r;
}

// Sem textos entre aspas e comentários: 'PEDIDOS_VENDA.*' (código de permissão) ou uma mensagem citando
// uma tabela não são referências a ela.
function codigo(body) {
  return body.replace(/'(?:[^'\\]|\\.|'')*'/g, "''").replace(/--[^\n]*/g, '');
}

export function refs(body, names) {
  body = codigo(body);
  const r = { read: new Set(), write: new Set() };
  for (const n of names) {
    const rx = new RegExp(`\\b${n}\\b`);
    if (!rx.test(body)) continue;
    if (new RegExp(`(INSERT(?: IGNORE)? INTO|UPDATE|DELETE FROM)\\s+${n}\\b`).test(body)) r.write.add(n);
    else r.read.add(n);
  }
  return r;
}

export function calls(body, names) {
  body = codigo(body);
  return names.filter(n => new RegExp(`\\b${n}\\b`).test(body));
}

export function sessionVars(body) {
  return [...new Set(body.match(/@LC_\w+/g) || [])];
}

export function setByTrigger(body) {
  return [...new Set([...body.matchAll(/(?:SET|,)\s+NEW\.(\w+)\s*=/g)].map(x => x[1]))];
}

export function loadAll(dir) {
  const read = f => fs.readFileSync(path.join(dir, f), 'utf8').replace(/\r\n/g, '\n');
  return {
    tables: parseSchema(read('01_schema.sql')),
    routines: parseRoutines(read('02_funcoes_procedures.sql')),
    triggers: parseTriggers(read('03_triggers.sql')),
    views: parseViews(read('04_views.sql')),
    seed: read('05_seed.sql'),
  };
}

// coluna -> lista de expressões atribuídas (para saber se a trigger só normaliza 0 -> NULL)
export function setByTriggerDetail(body) {
  const out = {};
  for (const x of body.matchAll(/(?:SET|,)\s+NEW\.(\w+)\s*=\s*([^;,\n]+?)(?=[;,]|\s*$|\s+END|\s+ELSE)/gm)) (out[x[1]] ||= []).push(x[2].trim());
  return out;
}
