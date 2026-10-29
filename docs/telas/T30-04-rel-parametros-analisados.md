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

## Implementação (tarefa 8.1)
`RelatoriosService.ParametrosAnalisadosAsync` sobre a `VW_LAB_RESULTADOS` (só `ULTIMA_REVISAO`), com o parâmetro vindo do **teste** do resultado (corrige o JOIN do legado, RN-09.05) ⨝ amostra ⨝ cliente. Colunas: data do resultado, parâmetro, nº da amostra, cliente, CPF/CNPJ. Padrão: agrupado por parâmetro com o total de cada um; "Agrupar por cliente" troca para subtotal por cliente e acrescenta a tabela "Total por parâmetro". Filtros: cliente, parâmetro, tipo de análise (do parâmetro), acreditado. O "código" do parâmetro não existe no cadastro (só a descrição). Teste: `RelatoriosTests.Parametros_tempo_e_producao_so_da_ultima_revisao_do_resultado`.

### Comparação com o legado (2017)
O SQL do legado devolve **0 linhas** em 2017 (o JOIN compara o ID do parâmetro com o ID do teste e quase nunca casa); o v2 lista **9.816** parâmetros analisados. Não há o que comparar: o relatório do legado não funcionava.


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
