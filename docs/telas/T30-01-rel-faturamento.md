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
- [ ] Total do período = soma dos pedidos não cancelados autorizados no período.
- [ ] Nenhum pedido duplicado.

**Status:** ⬜
