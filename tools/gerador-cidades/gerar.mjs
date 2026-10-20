// Gera Scripts/v2/06_cidades.sql a partir de municipios.json (API de localidades do IBGE).
import fs from 'node:fs';
const [, , entrada, saida] = process.argv;
const m = JSON.parse(fs.readFileSync(entrada, 'utf8'))
  .map(x => ({ ibge: String(x.id), nome: x.nome.toLocaleUpperCase('pt-BR') }))
  .sort((a, b) => a.ibge.localeCompare(b.ibge));
const sql = s => "'" + s.replace(/'/g, "''") + "'";
const lotes = [];
for (let i = 0; i < m.length; i += 500) {
  const linhas = m.slice(i, i + 500).map(c => `  ROW(${sql(c.ibge)}, ${sql(c.nome)})`).join(',\n');
  lotes.push(`INSERT IGNORE INTO CIDADES (ID_ESTADOS, CODIGO_IBGE, NOME)
SELECT E.ID_ESTADOS, V.IBGE, V.NOME
  FROM (VALUES
${linhas}
  ) AS V (IBGE, NOME)
  JOIN ESTADOS E ON E.CODIGO_IBGE = LEFT(V.IBGE, 2);`);
}
fs.writeFileSync(saida, `-- =====================================================================
-- LabControl v2 — Municípios do Brasil (${m.length}, códigos IBGE de 7 dígitos)
-- Fonte: API de localidades do IBGE (servicodados.ibge.gov.br/api/v1/localidades/municipios),
-- baixada em 20/10/2026; nomes em maiúsculas como no legado. Global (sem ID_EMPRESAS).
-- Reexecutável (INSERT IGNORE pela UNIQUE do código IBGE). A migração (M-02) liga as
-- cidades do legado (sem código IBGE) a estas pelo nome + UF.
-- =====================================================================

USE labcontrol_v2;

${lotes.join('\n\n')}
`);
console.log(m.length, 'municípios,', lotes.length, 'lotes');
