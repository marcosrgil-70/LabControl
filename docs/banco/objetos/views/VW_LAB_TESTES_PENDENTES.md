# VW_LAB_TESTES_PENDENTES

**Fila de análises** (nova): testes da última revisão da amostra, com status que permite resultado, ainda sem resultado na última revisão do resultado.

**Tipo:** view · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
— (não existia)

Comentário do script: _Fila de análises: testes da última revisão da amostra sem resultado na última revisão do resultado_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `A.ID_EMPRESAS` |
| `ID_LAB_HIST_AMOSTRAS` | `A.ID_LAB_HIST_AMOSTRAS` |
| `NR_AMOSTRA` | `A.NR_AMOSTRA` |
| `DT_ENTREGA` | `A.DT_ENTREGA` |
| `ID_LAB_HIST_AMOSTRAS_TESTES` | `T.ID_LAB_HIST_AMOSTRAS_TESTES` |
| `ID_LAB_PARAMETROS_ANALISES` | `T.ID_LAB_PARAMETROS_ANALISES` |
| `PARAMETRO` | `PA.DESCRICAO` |
| `DT_PREVISTA` | `T.DT_PREVISTA` |
| `DIAS_PARA_PRAZO` | `DATEDIFF(T.DT_PREVISTA, CURDATE())` |
| `CLIENTE_NOME` | `CL.NOME` |
| `PRODUTO_DESCRICAO` | `PR.DESCRICAO` |

## Origem dos dados
- **Tabelas/views:** [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md), [`ENTIDADES`](../tabelas/ENTIDADES.md), [`PRODUTOS`](../tabelas/PRODUTOS.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md)
- **Funções:** —

## Telas que usam
- [T26 Resultados](../../../telas/T26-resultados.md)
- [T01 Layout, menu e painel](../../../telas/T01-layout-menu.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- `DIAS_PARA_PRAZO < 0` = vencido (vermelho no painel e na fila).

## Definição
```sql
CREATE OR REPLACE VIEW VW_LAB_TESTES_PENDENTES AS
SELECT A.ID_EMPRESAS, A.ID_LAB_HIST_AMOSTRAS, A.NR_AMOSTRA, A.DT_ENTREGA, T.ID_LAB_HIST_AMOSTRAS_TESTES,
       T.ID_LAB_PARAMETROS_ANALISES, PA.DESCRICAO AS PARAMETRO, T.DT_PREVISTA,
       DATEDIFF(T.DT_PREVISTA, CURDATE()) AS DIAS_PARA_PRAZO, CL.NOME AS CLIENTE_NOME, PR.DESCRICAO AS PRODUTO_DESCRICAO
  FROM LAB_HIST_AMOSTRAS A
  JOIN AMOSTRAS_STATUS ST ON ST.ID_AMOSTRAS_STATUS = A.ID_AMOSTRAS_STATUS AND ST.PERMITE_RESULTADO = 1
  JOIN LAB_HIST_AMOSTRAS_TESTES T ON T.ID_LAB_HIST_AMOSTRAS = A.ID_LAB_HIST_AMOSTRAS
  JOIN LAB_PARAMETROS_ANALISES PA ON PA.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
  JOIN ENTIDADES CL ON CL.ID_ENTIDADES = A.ID_ENTIDADES
  JOIN PRODUTOS PR ON PR.ID_PRODUTOS = A.ID_PRODUTOS
 WHERE A.ULTIMA_REVISAO = 1
   AND NOT EXISTS (SELECT 1 FROM LAB_RESULTADOS_PARAM RP
                     JOIN LAB_RESULTADOS_ANALISES R ON R.ID_LAB_RESULTADOS_ANALISES = RP.ID_LAB_RESULTADOS_ANALISES
                    WHERE R.ID_LAB_HIST_AMOSTRAS = A.ID_LAB_HIST_AMOSTRAS AND R.ULTIMA_REVISAO = 1
                      AND RP.ID_LAB_HIST_AMOSTRAS_TESTES = T.ID_LAB_HIST_AMOSTRAS_TESTES);
```
