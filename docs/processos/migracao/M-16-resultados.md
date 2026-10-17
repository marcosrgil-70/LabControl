# M-16 Resultados

**Parte:** 3 (fase 5.1) · **Criticidade:** 🔴 · **Depende de:** [M-15](M-15-testes-estoque-arquivo.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_RESULTADOS_ANALISES` | 5.345 |
| `LAB_RESULTADOS_PARAM` | 19.528 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_RESULTADOS_ANALISES`](../../banco/objetos/tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md)

## Regras de transformação
- `REVISAO` renumerada 0..n **por amostra raiz** (ordem: revisão antiga, ID); `ULTIMA_REVISAO` na maior; `ID_RESULTADO_ORIGEM` = anterior.
- 124 amostras com mais de um resultado (`VW_DUPLICADO`): todas as revisões migram; listar.
- Parâmetros: teste pelo dicionário; `ID_PARAM_ORIGEM` por teste equivalente; `VR_EXP_*` double → inteiro; `DT_INICIAL`/`DT_RESULTADO` → DATE; IDs 0 → NULL.
- Depois da carga: `UPDATE` em lote recalculando `RESULTADO_FORMATADO`/`ESPECIF_FORMATADA` com `FN_FORMATA_RESULTADO` (triggers desligadas não calculam).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 5.345 / 19.528; 1 `ULTIMA_REVISAO = 1` por amostra física.

**Status:** ⬜
