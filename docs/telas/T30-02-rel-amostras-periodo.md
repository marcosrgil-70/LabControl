# T30-02 — Nº de Amostras por Período

**Fase:** 8 · **Rota:** `/Relatorios/AmostrasPeriodo` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelAmostrasPeriodo.pas`, menu *Relatórios → Nr. de Amostras por período*.
**Objetos do banco:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../banco/objetos/tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md)

## SQL do legado (resumo)
Por cliente (de `VW_LAB_HIST_AMOSTRA`, só amostras **com pedido de venda**):
- `QTDE_ENTRADA` = nº de amostras com `DT_ENTREGA` no período;
- `QTDE_ANALISADA` = nº de **números de amostra** distintos com algum `DT_RESULTADO` no período.

## v2
- **Colunas:** cliente, qtde de entrada (amostras **físicas** = revisão 0 com entrega no período), qtde analisada (amostras com resultado finalizado no período — última revisão do resultado, contada uma vez por amostra física), % analisada; total geral.
- **Filtros:** período; opção "somente amostras com pedido de venda" (padrão **marcado**, como o legado).

## Diferenças em relação ao legado (corrigidas)
- A view do legado duplicava amostras (6.287 linhas para 5.770) e a consulta externa não agrupava: o cliente aparecia repetido (era o `.fr3` que agrupava).
- Revisões da amostra contavam como entradas novas.

**Status:** ⬜
