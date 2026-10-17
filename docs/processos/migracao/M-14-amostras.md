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
- [ ] 5.770 amostras; por ano 2013…2018 = 62/611/511/663/2.482/1.440 (+1 ano 0).
- [ ] 20 números sorteados idênticos (e o relatório completo sem divergências).
- [ ] 1 `ULTIMA_REVISAO = 1` por (empresa, ano, código).

**Status:** ⬜
