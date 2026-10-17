# P24 — Migração Firebird → MySQL v2 (visão geral)

**Criticidade:** 🔴🔴 · **Fases:** 3.5, 4.1, 5.1 e 9.1
**Telas:** [T07 Migração](../telas/T07-migracao.md)
**Documento base:** [banco/13-MIGRACAO-DADOS.md](../banco/13-MIGRACAO-DADOS.md) · **Anomalias:** [banco/10 §8](../banco/10-ANALISE-FIREBIRD.md#8-anomalias-de-dados-encontradas-tratar-na-migração)
**Etapas detalhadas (um MD por etapa):** [processos/migracao/](migracao/README.md)

## Princípios
1. Banco destino **recriado do zero** pelos scripts `Scripts/v2/01..05` a cada execução completa (sem migração incremental).
2. Conexão MySQL com `SET @LC_MIGRACAO = 1` (triggers não numeram nem validam; números do legado preservados).
3. Tudo vai para a **empresa 1** (`COD_EMPRESA = '001'`).
4. `ID_LEGADO` em cada tabela + dicionário `idAntigo → idNovo` por tabela.
5. Uma transação por etapa, log por etapa, relatório final com conferências e anomalias.
6. Strings ISO8859_1 → UTF-8 (`Charset=ISO8859_1` no FirebirdClient) com `Trim()`; booleanos 'S'/'N' → 1/0; cores `TColor` → `#RRGGBB`.

## Etapas
| Parte (fase) | Etapas |
|---|---|
| Parte 1 (fase 3.5) — cadastros | [M-01](migracao/M-01-empresa.md) a [M-10](migracao/M-10-produtos-parametros.md) |
| Parte 2 (fase 4.1) — propostas, pedidos, amostras | [M-11](migracao/M-11-propostas.md) a [M-15](migracao/M-15-testes-estoque-arquivo.md) |
| Parte 3 (fase 5.1) — resultados, boletins, sequências | [M-16](migracao/M-16-resultados.md) a [M-18](migracao/M-18-sequencias.md) |

## Encerramento
1. `SET @LC_MIGRACAO = 0`.
2. Conferências obrigatórias (doc 13 §3) todas verdes.
3. Login com um usuário migrado (troca de senha obrigatória).
4. Roteiro do doc 12 §7 numa **cópia** do banco migrado.
5. `mysqldump --routines --triggers labcontrol_v2`.

## Riscos e mitigação
| Risco | Mitigação |
|---|---|
| Número de amostra diferente do impresso | `CODIGO` dos tipos = ID antigo; `NR_AMOSTRA` recalculado e comparado com o legado (20 sorteados + todos no relatório) |
| Revisões com raiz/origem erradas | cálculo por (empresa, ano, código) ordenado pela revisão; conferência "1 `ULTIMA_REVISAO` por documento" |
| Estoque duplicado | descartar movimentos de revisões > 0 e recalcular saldos |
| Boletins editáveis depois da migração | `DH_EMISSAO = DH_GERACAO` em todos |
| Encoding | conferir acentos em 20 nomes sorteados |
