# VW_PEDIDOS_VENDA_SALDO

Pedido de venda: quantidade contratada × recebida por item (casamento por proposta + ITEM em todas as revisões; conta só amostras revisão 0).

**Tipo:** view · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
— (não existia)

Comentário do script: _Pedido de venda: contratado x recebido por item (casamento por proposta + ITEM, todas as revisões)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `PV.ID_EMPRESAS` |
| `ID_PEDIDOS_VENDA` | `PV.ID_PEDIDOS_VENDA` |
| `NR_PEDIDO` | `PV.NR_PEDIDO` |
| `ID_PEDIDOS_VENDA_ITENS` | `PI.ID_PEDIDOS_VENDA_ITENS` |
| `ITEM` | `PP.ITEM` |
| `ID_PRODUTOS` | `PI.ID_PRODUTOS` |
| `PRODUTO_DESCRICAO` | `PR.DESCRICAO` |
| `QTDE_CONTRATADA` | `PI.QTDE_CONTRATADA` |
| `QTDE_RECEBIDA` | `(SELECT COUNT(*) FROM LAB_HIST_AMOSTRAS A JOIN LAB_PROPOSTAS PA ON PA.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS JOIN LAB_PROPOSTAS_PRODUTOS PPA ON PPA.ID_LAB_PROPOS` |

## Origem dos dados
- **Tabelas/views:** [`PRODUTOS`](../tabelas/PRODUTOS.md), [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Funções:** —

## Telas que usam
- [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md)
- [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- O recebimento avisa quando a quantidade recebida passa da contratada.

## Definição
```sql
CREATE OR REPLACE VIEW VW_PEDIDOS_VENDA_SALDO AS
SELECT PV.ID_EMPRESAS, PV.ID_PEDIDOS_VENDA, PV.NR_PEDIDO, PI.ID_PEDIDOS_VENDA_ITENS, PP.ITEM, PI.ID_PRODUTOS,
       PR.DESCRICAO AS PRODUTO_DESCRICAO, PI.QTDE_CONTRATADA,
       (SELECT COUNT(*) FROM LAB_HIST_AMOSTRAS A
          JOIN LAB_PROPOSTAS PA ON PA.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS
          JOIN LAB_PROPOSTAS_PRODUTOS PPA ON PPA.ID_LAB_PROPOSTAS_PRODUTOS = A.ID_LAB_PROPOSTAS_PRODUTOS
         WHERE PA.ID_EMPRESAS = PV.ID_EMPRESAS AND PA.ANO_PROPOSTA = PV.ANO_PROPOSTA
           AND PA.COD_PROPOSTA = PV.COD_PROPOSTA AND PPA.ITEM = PP.ITEM AND A.REVISAO = 0) AS QTDE_RECEBIDA
  FROM PEDIDOS_VENDA PV
  JOIN PEDIDOS_VENDA_ITENS PI ON PI.ID_PEDIDOS_VENDA = PV.ID_PEDIDOS_VENDA
  JOIN LAB_PROPOSTAS_PRODUTOS PP ON PP.ID_LAB_PROPOSTAS_PRODUTOS = PI.ID_LAB_PROPOSTAS_PRODUTOS
  JOIN PRODUTOS PR ON PR.ID_PRODUTOS = PI.ID_PRODUTOS;
```
