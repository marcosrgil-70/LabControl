# VW_LAB_BOLETINS

Consulta de boletins (código, revisão, datas de geração/emissão, flag emitido, status, amostra, cliente, responsáveis).

**Tipo:** view · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
query de consulta de `un_CadBoletim`

Comentário do script: _Boletins (consulta da tela de Boletim)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `B.ID_EMPRESAS` |
| `ID_LAB_BOLETIM_ANALISES` | `B.ID_LAB_BOLETIM_ANALISES` |
| `COD_BOLETIM` | `B.COD_BOLETIM` |
| `REVISAO` | `B.REVISAO` |
| `ULTIMA_REVISAO` | `B.ULTIMA_REVISAO` |
| `MOTIVO_REVISAO` | `B.MOTIVO_REVISAO` |
| `DT_BOLETIM` | `B.DT_BOLETIM` |
| `DH_GERACAO` | `B.DH_GERACAO` |
| `DH_EMISSAO` | `B.DH_EMISSAO` |
| `EMITIDO` | `(B.DH_EMISSAO IS NOT NULL)` |
| `ID_BOLETINS_STATUS` | `B.ID_BOLETINS_STATUS` |
| `STATUS_BOLETIM` | `S.DESCRICAO` |
| `COR_STATUS` | `S.COR` |
| `ID_LAB_HIST_AMOSTRAS` | `B.ID_LAB_HIST_AMOSTRAS` |
| `NR_AMOSTRA` | `A.NR_AMOSTRA` |
| `NR_LOTE` | `A.NR_LOTE` |
| `ID_ENTIDADES` | `A.ID_ENTIDADES` |
| `CLIENTE_NOME` | `CL.NOME` |
| `PRODUTO_DESCRICAO` | `PR.DESCRICAO` |
| `ID_LAB_RESULTADOS_ANALISES` | `B.ID_LAB_RESULTADOS_ANALISES` |
| `REVISAO_RESULTADO` | `R.REVISAO` |
| `ID_ENTIDADES_FUNC_RESP` | `B.ID_ENTIDADES_FUNC_RESP` |
| `RESPONSAVEL1` | `R1.NOME` |
| `ID_ENTIDADES_FUNC_RESP2` | `B.ID_ENTIDADES_FUNC_RESP2` |
| `RESPONSAVEL2` | `R2.NOME` |
| `DT_ENTREGA` | `A.DT_ENTREGA` |

## Origem dos dados
- **Tabelas/views:** [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md), [`ENTIDADES`](../tabelas/ENTIDADES.md), [`PRODUTOS`](../tabelas/PRODUTOS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Funções:** —

## Telas que usam
- [T27 Boletim](../../../telas/T27-boletim.md)
- [T01 Layout, menu e painel](../../../telas/T01-layout-menu.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- Painel: boletins não emitidos = `EMITIDO = 0 AND ULTIMA_REVISAO = 1`.

## Definição
```sql
CREATE OR REPLACE VIEW VW_LAB_BOLETINS AS
SELECT B.ID_EMPRESAS, B.ID_LAB_BOLETIM_ANALISES, B.COD_BOLETIM, B.REVISAO, B.ULTIMA_REVISAO, B.MOTIVO_REVISAO,
       B.DT_BOLETIM, B.DH_GERACAO, B.DH_EMISSAO, (B.DH_EMISSAO IS NOT NULL) AS EMITIDO,
       B.ID_BOLETINS_STATUS, S.DESCRICAO AS STATUS_BOLETIM, S.COR AS COR_STATUS,
       B.ID_LAB_HIST_AMOSTRAS, A.NR_AMOSTRA, A.NR_LOTE, A.ID_ENTIDADES, CL.NOME AS CLIENTE_NOME,
       PR.DESCRICAO AS PRODUTO_DESCRICAO, B.ID_LAB_RESULTADOS_ANALISES, R.REVISAO AS REVISAO_RESULTADO,
       B.ID_ENTIDADES_FUNC_RESP, R1.NOME AS RESPONSAVEL1, B.ID_ENTIDADES_FUNC_RESP2, R2.NOME AS RESPONSAVEL2,
       A.DT_ENTREGA
  FROM LAB_BOLETIM_ANALISES B
  JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = B.ID_LAB_HIST_AMOSTRAS
  JOIN ENTIDADES CL ON CL.ID_ENTIDADES = A.ID_ENTIDADES
  JOIN PRODUTOS PR ON PR.ID_PRODUTOS = A.ID_PRODUTOS
  JOIN LAB_RESULTADOS_ANALISES R ON R.ID_LAB_RESULTADOS_ANALISES = B.ID_LAB_RESULTADOS_ANALISES
  LEFT JOIN BOLETINS_STATUS S ON S.ID_BOLETINS_STATUS = B.ID_BOLETINS_STATUS
  LEFT JOIN ENTIDADES R1 ON R1.ID_ENTIDADES = B.ID_ENTIDADES_FUNC_RESP
  LEFT JOIN ENTIDADES R2 ON R2.ID_ENTIDADES = B.ID_ENTIDADES_FUNC_RESP2;
```
