# M-10 Produtos e parâmetros de análise

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-03](M-03-auxiliares.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `PRODUTOS` | 1.915 |
| `LAB_PARAMETROS_ANALISES` | 462 |
| `SYS_TABELAS_ID` | 1 |

## Destino (MySQL `labcontrol_v2`)
[`PRODUTOS`](../../banco/objetos/tabelas/PRODUTOS.md), [`LAB_PARAMETROS_ANALISES`](../../banco/objetos/tabelas/LAB_PARAMETROS_ANALISES.md)

## Regras de transformação
- Produtos: código mantido; unidade e embalagem pelos dicionários.
- Parâmetros: `DESC_REDUZIDA` vazia → `DESCRICAO`; `AUDITADO` 'S'/'N' → 1/0; tipo e método pelos dicionários.
- Descrição de parâmetro duplicada → sufixo " (2)" e relatório.
- `SEQUENCIAS_DOCUMENTOS (1, 'PRODUTO', 0) = SYS_TABELAS_ID.ID` (último = 3801) — pode ficar para a M-18.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 1.915 / 462; 52 acreditados.

**Status:** ⬜
