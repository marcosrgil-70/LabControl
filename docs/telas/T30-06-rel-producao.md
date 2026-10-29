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

## Implementação (tarefa 8.1)
`RelatoriosService.ProducaoAsync`: data do resultado, parâmetro, produto, quantidade de amostra com unidade (`QT_AMOSTRA_TXT` da `VW_LAB_RESULTADOS`), analista e nº da amostra; tabelas "Análises por dia" e "Análises por parâmetro" com total. Filtros: parâmetro, produto, analista. Só a última revisão do resultado. Em 2017: legado 10.072 linhas (todas as revisões), v2 9.816. Testes: `RelatoriosTests.Parametros_tempo_e_producao_so_da_ultima_revisao_do_resultado` + navegador.


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
