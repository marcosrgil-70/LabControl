# T30-06 — Produção por Período

**Fase:** 8 · **Rota:** `/Relatorios/Producao` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelProducao.pas`, menu *Relatórios → Produção por período*.
**Objetos do banco:** [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md), [`PRODUTOS`](../banco/objetos/tabelas/PRODUTOS.md)

## SQL do legado (resumo)
Para cada resultado com `DT_RESULTADO` no período (empresa ativa): data do resultado, `QT_AMOSTRA` (quantidade analisada), parâmetro e produto; ordenado por data e parâmetro.

## v2
- **Colunas:** data do resultado, parâmetro, produto, quantidade de amostra analisada + unidade (`QT_AMOSTRA_TXT`), analista (novo).
- **Totais:** nº de análises por dia e por parâmetro; total geral.
- **Filtros:** período*, parâmetro, produto, analista.
- Só a última revisão do resultado.

**Status:** ⬜
