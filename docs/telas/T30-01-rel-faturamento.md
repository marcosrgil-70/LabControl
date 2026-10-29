# T30-01 — Relatório de Faturamento

**Fase:** 8 · **Rota:** `/Relatorios/Faturamento` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelFaturamento.pas` + `Layout_RelFaturamento.fr3`, menu *Relatórios → Faturamento*.
**Objetos do banco:** [`VW_LAB_PROPOSTAS`](../banco/objetos/views/VW_LAB_PROPOSTAS.md), [`PEDIDOS_VENDA`](../banco/objetos/tabelas/PEDIDOS_VENDA.md)

## SQL do legado
```sql
SELECT P.ID_LAB_PROPOSTAS, P.DT_AUTORIZACAO, P.NR_PROPOSTA, P.ID_PEDIDOS_VENDAS, P.CNPJ_CPF, P.NOME_CLIENTE,
       P.COND_PAGTO, P.PORC_DESCONTO, P.VR_DESCONTO, P.VR_TOTAL_PROPOSTA
  FROM VW_LAB_PROPOSTAS P
 WHERE COALESCE(P.ID_PEDIDOS_VENDAS,0) > 0
   AND P.DT_AUTORIZACAO BETWEEN :ini AND :fim
   AND P.ID_EMPRESAS = :empresa
 ORDER BY 2,3,4
```

## v2
- **Base:** `PEDIDOS_VENDA` (situação diferente de CANCELADO) ⨝ proposta vigente do pedido ⨝ cliente, cobrança e condição.
- **Filtro de data:** `DT_AUTORIZACAO` do pedido no período; filtros opcionais cliente, entidade de cobrança e situação.
- **Colunas:** data de autorização, nº do pedido (9 dígitos), nº da proposta (revisão vigente), CPF/CNPJ e nome do cliente, entidade de cobrança, condição de pagamento, % e valor do desconto, valor total.
- **Agrupamento:** por cliente com subtotal; total geral e quantidade de pedidos.

## Diferenças em relação ao legado (corrigidas)
- O legado lia a proposta **em todas as revisões** com pedido: os 19 pedidos repetidos em revisões apareciam **mais de uma vez** (valor faturado inflado). No v2 é **uma linha por pedido**.
- A `VW_LAB_PROPOSTAS` do legado fazia INNER JOIN na cobrança: propostas sem cobrança sumiam do relatório.

## Critérios de aceite
- [x] Total do período = soma dos pedidos não cancelados autorizados no período.
- [x] Nenhum pedido duplicado.

## Implementação (tarefa 8.1)
`RelatoriosService.FaturamentoAsync`: `PEDIDOS_VENDA` não cancelados com `DT_AUTORIZACAO` no período ⨝ revisão da proposta do pedido (a vigente) ⨝ cliente, cobrança e condição; filtros de cliente, cobrança (nome ou CPF/CNPJ) e situação (sem "Cancelado"). Ordem: cliente, data, nº do pedido; subtotal por cliente com a quantidade de pedidos; total geral. % e valor do desconto e total vêm da proposta (como o legado). Teste: `RelatoriosTests.Faturamento_um_pedido_por_linha_com_a_revisao_vigente_e_sem_cancelados`.

### Comparação com o legado (2017)
- Legado: 620 linhas, 555 pedidos distintos, R$ 344.743,13. v2: 555 pedidos, R$ 316.678,61 — os mesmos 555 números de pedido.
- **R$ 27.993,32** vinham de linhas repetidas: a `VW_LAB_PROPOSTAS` do legado duplicava a proposta (38 pedidos apareciam 2 ou 3 vezes, com a mesma revisão). Sem as repetições, o legado soma R$ 316.749,81.
- **R$ 71,20** vêm de 2 pedidos com revisão da proposta: o legado mostrava a revisão autorizada no período (627: R$ 139,50; 641: R$ 263,00) e o v2 a vigente do pedido (627 R2: R$ 94,60; 641 R1: R$ 236,70).
- Os outros 553 pedidos têm o mesmo valor nos dois.


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
