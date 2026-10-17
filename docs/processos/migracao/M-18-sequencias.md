# M-18 Sequências de numeração

**Parte:** 3 (fase 5.1) · **Criticidade:** 🔴 · **Depende de:** [M-11](M-11-propostas.md), [M-13](M-13-pedidos-venda.md), [M-14](M-14-amostras.md), [M-10](M-10-produtos-parametros.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `(máximos dos documentos migrados)` | — |
| `SYS_TABELAS_ID` | 1 |

## Destino (MySQL `labcontrol_v2`)
[`SEQUENCIAS_DOCUMENTOS`](../../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md)

## Regras de transformação
- `AMOSTRA` por ano = `MAX(COD_AMOSTRA)` do ano.
- `PROPOSTA` por ano = `MAX(COD_PROPOSTA)` do ano, **ignorando** as 12 com `COD = ID`.
- `PEDIDO_VENDA` (ano 0) = `MAX(NR_PEDIDO)` (legado `ULT_PED_VENDA = 1471`).
- `PRODUTO` (ano 0) = `SYS_TABELAS_ID` (3801).
- Ao final: `SET @LC_MIGRACAO = 0`; incluir uma amostra de teste numa **cópia** do banco e conferir que recebe o próximo número do ano atual.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] Próxima amostra/proposta/pedido de teste recebe o número esperado.

**Status:** ⬜
