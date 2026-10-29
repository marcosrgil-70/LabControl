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

## Implementação (tarefa 8.1)
`RelatoriosService.AmostrasAsync` (o mesmo da T30-03, agrupado só por cliente): uma linha por amostra física (a última revisão traz cliente, produto e pedido vigentes); **entradas** = revisão 0 com `DT_ENTREGA` no período; **analisadas** = amostras físicas com algum parâmetro da última revisão do resultado no período (uma vez cada, mesmo que entregues antes); % analisada = analisadas ÷ entradas. **"Somente amostras com pedido de venda"** vem marcado (dúvida 9, decisão do usuário em 29/10/2026) e vale **por amostra** (`ID_PEDIDOS_VENDA` da última revisão). Testes: `RelatoriosTests.Amostra_revisada_conta_uma_vez_e_somente_com_pedido_e_o_padrao` + navegador.

### Comparação com o legado (2017)
- Legado: 2.481 entradas e 2.373 analisadas. v2: **2.455 / 2.347** com a opção marcada; **2.458 / 2.350** com todas.
- Entradas: o legado contava as **revisões** (2.481 linhas = 2.458 amostras físicas + 23 revisões). Analisadas: contava o `NR_AMOSTRA` da revisão ("… R01") como outra amostra.
- **"Somente com pedido" no legado era por cliente:** a consulta externa só escolhia os clientes que tinham alguma amostra com pedido, e as subconsultas contavam **todas** as amostras desses clientes. No v2 a opção vale por amostra: as 3 amostras de 2017 sem pedido saem com ela marcada.
- O legado também listava os clientes sem movimento no período (zeros) e repetia o cliente uma vez por amostra (o `.fr3` agrupava); o v2 lista só quem teve entrada ou resultado no período (81 clientes em 2017).


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
