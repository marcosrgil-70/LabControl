# M-14 Amostras

**Parte:** 2 (fase 4.1) · **Criticidade:** 🔴 · **Depende de:** [M-12](M-12-produtos-analises-proposta.md), [M-13](M-13-pedidos-venda.md), [M-08](M-08-profissionais.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_HIST_AMOSTRAS` | 5.770 |
| `LAB_HIST_AMOSTRAS_OBSERVACAO` | 6 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_HIST_AMOSTRAS`](../../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md)

## Regras de transformação
- `ANO_AMOSTRA = 2000 + ANO`; ANO = 0 → `YEAR(DT_ENTREGA)` (1 caso; listar). As 3 amostras com ano ≠ ano da entrega mantêm o ano original (é o que foi impresso).
- `ID_AMOSTRAS_TIPO`/`ID_ANALISES_TIPO` pelos dicionários (códigos = IDs antigos).
- `NR_AMOSTRA = FN_NR_AMOSTRA_ID(...)` — conferir com `VW_LAB_HIST_AMOSTRA.NR_AMOSTRA` do legado (**todas**; divergência = erro).
- `ID_LAB_PROPOSTAS_PRODUTOS` pelo dicionário (proposta, item); `ID_PEDIDOS_VENDA` pelo número.
- `DT_ENTREGA`/`HR_ENTREGA` timestamps → DATE/TIME; flags 'S'/'N' → 1/0; renomes de coluna (D-03).
- Por (empresa, ano, código) ordenado pela revisão: `ID_AMOSTRA_RAIZ` = revisão 0, `ID_AMOSTRA_ORIGEM` = anterior, `ULTIMA_REVISAO` na maior.
- 61 revisões sem coletor/data da coleta → copiar da raiz.
- Observação RTF → `TX_OBSERVACAO`.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 5.770 amostras; por ano 2013…2018 = 62/611/511/663/2.482/1.440 (+1 ano 0, gravada em 2014).
- [x] **Todos** os números conferidos com a `VW_LAB_HIST_AMOSTRA` (não só 20): 5.767 idênticos. As 3 amostras fora da visão do legado (670, 737, 1494) ficam listadas. A do ano 0 (359) era impressa `03-00190-01/00` e é `…/14` no v2 (doc 10 §8, listada). Qualquer outra divergência para a etapa.
- [x] 1 `ULTIMA_REVISAO = 1` por (empresa, ano, código).

## Implementação (tarefa 4.1, 23/10/2026)
- `Services/Migracao/Etapas/M14Amostras.cs`.
- INSERT direto em lote, por revisão (raiz, origem e última calculadas antes).
- Item da proposta pelo dicionário da M-12.
- **Pedido:**
  - número do legado → pedido;
  - 0 (269 amostras) → o pedido da proposta, se houver (regra do `TG_AMOSTRAS_BI`).
- Coletor/coleta copiados da raiz em **60** revisões.
- Sequência `AMOSTRA` por ano = maior código.
- **Datas parciais:** 7 inconsistentes, como "7//2014", "29/2/2018" e "1/9/20172019", ajustadas por `Conversoes.DataParcial` (a trigger as recusaria mesmo na migração). Cada uma listada.

**Status:** ✅ implementada na tarefa 4.1 (23/10/2026); conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
