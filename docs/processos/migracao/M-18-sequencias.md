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
- As linhas são gravadas pelas etapas de cada documento (desde a 3.5/4.1): `PRODUTO` (ano 0) = `SYS_TABELAS_ID` (3801) na M-10; `PROPOSTA` por ano = `MAX(COD_PROPOSTA)` do ano, **ignorando** as 12 com `COD = ID`, na M-11; `PEDIDO_VENDA` (ano 0) = `MAX(NR_PEDIDO)` (legado `ULT_PED_VENDA = 1471`) na M-13; `AMOSTRA` por ano = `MAX(COD_AMOSTRA)` do ano na M-14.
- A M-18 **confere**: nenhuma sequência abaixo do maior número usado no destino.
- Teste do próximo número "numa cópia": dentro da transação da etapa, `SAVEPOINT` → `@LC_MIGRACAO = 0` → `SP_PROXIMO_NUMERO` para AMOSTRA e PROPOSTA (ano atual do servidor), PEDIDO_VENDA e PRODUTO (ano 0) → confere último + 1 → `ROLLBACK TO SAVEPOINT` e `@LC_MIGRACAO = 1`. Nada fica gravado (conferido).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] Próxima amostra/proposta/pedido/produto de teste recebe o número esperado; sequências intactas depois do teste.

## Implementação (tarefa 5.1, 27/10/2026)
- `Services/Migracao/Etapas/M18Sequencias.cs` (sem gravação: só conferências).
- Últimos números migrados: AMOSTRA 2013–2018 = 63, 678, 521, 656, 2.484, 1.402; PROPOSTA 2012–2018 = 51, 45, 727, 233, 638, 1.405, 1.799; PEDIDO_VENDA 1.471; PRODUTO 3.801. No ano atual (2026) não há amostra nem proposta do legado: o próximo número é 1.

**Status:** ✅ implementada na tarefa 5.1 (27/10/2026); conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
