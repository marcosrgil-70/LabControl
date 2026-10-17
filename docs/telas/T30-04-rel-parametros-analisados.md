# T30-04 — Nº de Parâmetros Analisados

**Fase:** 8 · **Rota:** `/Relatorios/ParametrosAnalisados` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelParamAnalisados.pas`, menu *Relatórios → Nr. Parâmetros analisados*.
**Objetos do banco:** [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`ENTIDADES`](../banco/objetos/tabelas/ENTIDADES.md)

## SQL do legado (resumo) — ⚠️ com erro grave
```sql
SELECT ... FROM LAB_RESULTADOS_PARAM RP
  JOIN LAB_RESULTADOS_ANALISES RA ON RA.ID_LAB_RESULTADOS_ANALISES = RP.ID_LAB_RESULTADOS_ANALISES
  JOIN LAB_HIST_AMOSTRAS HA ON HA.ID_LAB_HIST_AMOSTRAS = RA.ID_LAB_HIST_AMOSTRAS
  JOIN LAB_HIST_AMOSTRAS_TESTES HT ON HT.ID_LAB_PARAMETROS_ANALISES = RP.ID_LAB_HIST_AMOSTRA_TESTES
 WHERE HA.ID_EMPRESAS = :empresa AND RP.DT_RESULTADO BETWEEN :ini AND :fim
 GROUP BY teste, cliente, CNPJ/CPF, parametro
```
O último JOIN compara o **ID do parâmetro** com o **ID do teste** (colunas de significado diferente). Os números desse relatório no legado **não são confiáveis**.

## v2
- **Base:** resultados (`LAB_RESULTADOS_PARAM` da **última revisão** do resultado) com `DT_RESULTADO` no período ⨝ teste ⨝ parâmetro ⨝ amostra ⨝ cliente.
- **Colunas:** data do resultado, cliente (código, CPF/CNPJ, nome), parâmetro (código, nome), nº da amostra.
- **Totais:** por parâmetro (quantidade analisada) e geral; opção de agrupar por cliente.
- **Filtros:** período*, cliente, parâmetro, tipo de análise, acreditado (sim/não).

**Status:** ⬜
