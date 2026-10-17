# VW_LAB_RESULTADOS

Resultados por parâmetro com textos prontos (formatado, especificação, unidade, quantidade analisada, analista, dias de análise).

**Tipo:** view · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
`qry_Param` da tela de boletim

Comentário do script: _Resultados com textos prontos (base da tela de seleção de parâmetros do boletim — qry_Param do legado)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `R.ID_EMPRESAS` |
| `ID_LAB_RESULTADOS_ANALISES` | `R.ID_LAB_RESULTADOS_ANALISES` |
| `ID_LAB_HIST_AMOSTRAS` | `R.ID_LAB_HIST_AMOSTRAS` |
| `REVISAO` | `R.REVISAO` |
| `ULTIMA_REVISAO` | `R.ULTIMA_REVISAO` |
| `ID_LAB_RESULTADOS_PARAM` | `RP.ID_LAB_RESULTADOS_PARAM` |
| `ID_LAB_HIST_AMOSTRAS_TESTES` | `RP.ID_LAB_HIST_AMOSTRAS_TESTES` |
| `ID_LAB_PARAMETROS_ANALISES` | `PA.ID_LAB_PARAMETROS_ANALISES` |
| `PARAMETRO` | `PA.DESCRICAO` |
| `DESC_REDUZIDA` | `PA.DESC_REDUZIDA` |
| `AUDITADO` | `PA.AUDITADO` |
| `DT_INICIAL` | `RP.DT_INICIAL` |
| `DT_RESULTADO` | `RP.DT_RESULTADO` |
| `DIAS_ANALISE` | `DATEDIFF(RP.DT_RESULTADO, RP.DT_INICIAL)` |
| `TIPO_RESULTADO` | `RP.TIPO_RESULTADO` |
| `RESULTADO_FORMATADO` | `RP.RESULTADO_FORMATADO` |
| `ESPECIF_FORMATADA` | `RP.ESPECIF_FORMATADA` |
| `UNIDADE` | `U.SIGLA` |
| `QT_AMOSTRA_TXT` | `CONCAT_WS(' ', FN_FORMATA_DECIMAL(RP.QT_AMOSTRA), UA.SIGLA)` |
| `ID_ENTIDADES_FUNC_ANALISE` | `RP.ID_ENTIDADES_FUNC_ANALISE` |
| `ANALISTA_NOME` | `AN.NOME` |
| `VR_SATISFEITO` | `RP.VR_SATISFEITO` |

## Origem dos dados
- **Tabelas/views:** [`UNIDADES`](../tabelas/UNIDADES.md), [`ENTIDADES`](../tabelas/ENTIDADES.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md)
- **Funções:** [`FN_FORMATA_DECIMAL`](../rotinas/FN_FORMATA_DECIMAL.md)

## Telas que usam
- [T26 Resultados](../../../telas/T26-resultados.md)
- [T27 Boletim](../../../telas/T27-boletim.md)
- [T30-04 Parâmetros analisados](../../../telas/T30-04-rel-parametros-analisados.md)
- [T30-05 Tempo das análises](../../../telas/T30-05-rel-tempo-analises.md)
- [T30-06 Produção por período](../../../telas/T30-06-rel-producao.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- Grid da esquerda da seleção de parâmetros do boletim (filtrar a última revisão do resultado).

## Definição
```sql
CREATE OR REPLACE VIEW VW_LAB_RESULTADOS AS
SELECT R.ID_EMPRESAS, R.ID_LAB_RESULTADOS_ANALISES, R.ID_LAB_HIST_AMOSTRAS, R.REVISAO, R.ULTIMA_REVISAO,
       RP.ID_LAB_RESULTADOS_PARAM, RP.ID_LAB_HIST_AMOSTRAS_TESTES, PA.ID_LAB_PARAMETROS_ANALISES,
       PA.DESCRICAO AS PARAMETRO, PA.DESC_REDUZIDA, PA.AUDITADO, RP.DT_INICIAL, RP.DT_RESULTADO,
       DATEDIFF(RP.DT_RESULTADO, RP.DT_INICIAL) AS DIAS_ANALISE,
       RP.TIPO_RESULTADO, RP.RESULTADO_FORMATADO, RP.ESPECIF_FORMATADA, U.SIGLA AS UNIDADE,
       CONCAT_WS(' ', FN_FORMATA_DECIMAL(RP.QT_AMOSTRA), UA.SIGLA) AS QT_AMOSTRA_TXT,
       RP.ID_ENTIDADES_FUNC_ANALISE, AN.NOME AS ANALISTA_NOME, RP.VR_SATISFEITO
  FROM LAB_RESULTADOS_ANALISES R
  JOIN LAB_RESULTADOS_PARAM RP ON RP.ID_LAB_RESULTADOS_ANALISES = R.ID_LAB_RESULTADOS_ANALISES
  JOIN LAB_HIST_AMOSTRAS_TESTES T ON T.ID_LAB_HIST_AMOSTRAS_TESTES = RP.ID_LAB_HIST_AMOSTRAS_TESTES
  JOIN LAB_PARAMETROS_ANALISES PA ON PA.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
  LEFT JOIN UNIDADES U ON U.ID_UNIDADES = RP.ID_UNIDADES
  LEFT JOIN UNIDADES UA ON UA.ID_UNIDADES = RP.ID_UNIDADES_AMOSTRA
  LEFT JOIN ENTIDADES AN ON AN.ID_ENTIDADES = RP.ID_ENTIDADES_FUNC_ANALISE;
```
