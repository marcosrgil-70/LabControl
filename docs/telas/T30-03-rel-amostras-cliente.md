# T30-03 — Nº de Amostras por Cliente (por produto)

**Fase:** 8 · **Rota:** `/Relatorios/AmostrasCliente` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelAmostrasCliente.pas`, menu *Relatórios → Nr. Amostras por cliente* — filtro de cliente opcional (subtítulo "Cliente: CNPJ Nome").
**Objetos do banco:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md)

## SQL do legado (resumo)
Igual ao T30-02, mas agrupado por **cliente + produto** (código e descrição), com filtro opcional `A.ID_ENTIDADES = :cliente`; só amostras com pedido de venda.

## v2
- **Filtros:** período*, cliente (opcional), produto (opcional), "somente com pedido" (padrão marcado).
- **Colunas:** cliente, código do produto, descrição, qtde de entrada, qtde analisada; subtotal por cliente; total geral.
- Mesmas correções do [T30-02](T30-02-rel-amostras-periodo.md) (amostra física, última revisão).

## Implementação (tarefa 8.1)
Mesma consulta da [T30-02](T30-02-rel-amostras-periodo.md#implementação-tarefa-81), agrupada por cliente + produto (da última revisão), com filtros de cliente (nome ou CPF/CNPJ) e produto (código ou descrição); subtotal por cliente e total geral. Em 2017: 2.455 entradas / 2.347 analisadas com pedido (as mesmas diferenças da T30-02). Teste: `RelatoriosTests.Amostra_revisada_conta_uma_vez_e_somente_com_pedido_e_o_padrao`.


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
