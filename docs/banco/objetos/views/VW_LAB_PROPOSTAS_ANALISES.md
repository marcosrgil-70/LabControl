# VW_LAB_PROPOSTAS_ANALISES

Itens de análise da proposta com produto, parâmetro, tipo de análise, método, idioma, prazo e valores.

**Tipo:** view · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
queries `cds_ProdAnalises` da tela de propostas

Comentário do script: _Itens de análise da proposta (grid de análises / impressão da proposta e do pedido)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `PA.ID_EMPRESAS` |
| `ID_LAB_PROPOSTAS` | `PA.ID_LAB_PROPOSTAS` |
| `ID_LAB_PROPOSTAS_PRODANALISES` | `PA.ID_LAB_PROPOSTAS_PRODANALISES` |
| `ID_LAB_PROPOSTAS_PRODUTOS` | `PP.ID_LAB_PROPOSTAS_PRODUTOS` |
| `ITEM` | `PP.ITEM` |
| `ID_PRODUTOS` | `PP.ID_PRODUTOS` |
| `PRODUTO_CODIGO` | `PR.CODIGO` |
| `PRODUTO_DESCRICAO` | `PR.DESCRICAO` |
| `QTDE_PRODUTO` | `PP.QTDE_AMOSTRAS` |
| `ID_LAB_PARAMETROS_ANALISES` | `PA.ID_LAB_PARAMETROS_ANALISES` |
| `PARAMETRO` | `PAR.DESCRICAO` |
| `DESC_REDUZIDA` | `PAR.DESC_REDUZIDA` |
| `AUDITADO` | `PAR.AUDITADO` |
| `COD_TIPO_ANALISE` | `AT.CODIGO` |
| `TIPO_ANALISE` | `AT.DESCRICAO` |
| `METODO` | `M.DESCRICAO` |
| `IDIOMA` | `I.DESCRICAO` |
| `PRAZO` | `Z.DESCRICAO` |
| `QTDE_DIAS` | `Z.QTDE_DIAS` |
| `TIPO_DOCUMENTO` | `PA.TIPO_DOCUMENTO` |
| `QTDE_AMOSTRAS` | `PA.QTDE_AMOSTRAS` |
| `VR_UNITARIO` | `PA.VR_UNITARIO` |
| `VR_SUBTOTAL` | `PA.VR_SUBTOTAL` |
| `PORC_DESCONTO` | `PA.PORC_DESCONTO` |
| `VR_DESCONTO` | `PA.VR_DESCONTO` |
| `VR_TOTAL` | `PA.VR_TOTAL` |
| `ST_REV_ANTERIOR` | `PA.ST_REV_ANTERIOR` |

## Origem dos dados
- **Tabelas/views:** [`IDIOMAS`](../tabelas/IDIOMAS.md), [`PRAZOS`](../tabelas/PRAZOS.md), [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md), [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md), [`PRODUTOS`](../tabelas/PRODUTOS.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Funções:** —

## Telas que usam
- [T20 Propostas](../../../telas/T20-propostas.md)
- [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- Base do grid de análises e dos PDFs de proposta e pedido.

## Definição
```sql
CREATE OR REPLACE VIEW VW_LAB_PROPOSTAS_ANALISES AS
SELECT PA.ID_EMPRESAS, PA.ID_LAB_PROPOSTAS, PA.ID_LAB_PROPOSTAS_PRODANALISES, PP.ID_LAB_PROPOSTAS_PRODUTOS, PP.ITEM,
       PP.ID_PRODUTOS, PR.CODIGO AS PRODUTO_CODIGO, PR.DESCRICAO AS PRODUTO_DESCRICAO, PP.QTDE_AMOSTRAS AS QTDE_PRODUTO,
       PA.ID_LAB_PARAMETROS_ANALISES, PAR.DESCRICAO AS PARAMETRO, PAR.DESC_REDUZIDA, PAR.AUDITADO,
       AT.CODIGO AS COD_TIPO_ANALISE, AT.DESCRICAO AS TIPO_ANALISE, M.DESCRICAO AS METODO, I.DESCRICAO AS IDIOMA,
       Z.DESCRICAO AS PRAZO, Z.QTDE_DIAS, PA.TIPO_DOCUMENTO, PA.QTDE_AMOSTRAS, PA.VR_UNITARIO, PA.VR_SUBTOTAL,
       PA.PORC_DESCONTO, PA.VR_DESCONTO, PA.VR_TOTAL, PA.ST_REV_ANTERIOR
  FROM LAB_PROPOSTAS_PRODANALISES PA
  JOIN LAB_PROPOSTAS_PRODUTOS PP ON PP.ID_LAB_PROPOSTAS_PRODUTOS = PA.ID_LAB_PROPOSTAS_PRODUTOS
  JOIN PRODUTOS PR ON PR.ID_PRODUTOS = PP.ID_PRODUTOS
  JOIN LAB_PARAMETROS_ANALISES PAR ON PAR.ID_LAB_PARAMETROS_ANALISES = PA.ID_LAB_PARAMETROS_ANALISES
  JOIN ANALISES_TIPO AT ON AT.ID_ANALISES_TIPO = PAR.ID_ANALISES_TIPO
  JOIN ANALISES_METODOS M ON M.ID_ANALISES_METODOS = PA.ID_ANALISES_METODOS
  JOIN IDIOMAS I ON I.ID_IDIOMAS = PA.ID_IDIOMAS
  JOIN PRAZOS Z ON Z.ID_PRAZOS = PA.ID_PRAZOS;
```
