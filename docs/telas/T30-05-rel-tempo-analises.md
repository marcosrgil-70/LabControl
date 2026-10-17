# T30-05 — Tempo de Duração das Análises

**Fase:** 8 · **Rota:** `/Relatorios/TempoAnalises` · **Permissão:** `RELATORIOS.CONSULTAR` · **Padrão:** [T30-00](T30-00-relatorios-padrao.md)
**Legado:** `LabControl/un_RelTempoAnalises.pas`, menu *Relatórios → Tempo de duração das Análises* — filtro opcional de parâmetro.
**Objetos do banco:** [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md) (coluna `DIAS_ANALISE`), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md)

## SQL do legado (resumo)
`TEMPO_ANALISE = (COALESCE(DT_RESULTADO, DT_INICIAL) − DT_INICIAL) + 1` (dias **inclusivos**), para cada resultado com `DT_RESULTADO` no período; filtro opcional por `ID_LAB_PARAMETROS_ANALISES`; ordenado por data inicial.

## v2
- **Colunas:** data inicial, data do resultado, **tempo (dias)** = `DATEDIFF(DT_RESULTADO, DT_INICIAL) + 1` (manter a contagem inclusiva do legado), parâmetro, nº da amostra; **novo:** prazo previsto (`DT_PREVISTA`) e atraso (dias além do prazo).
- **Totais:** média, mínimo e máximo por parâmetro; média geral.
- **Filtros:** período* (data do resultado), parâmetro, tipo de análise.
- Só a última revisão do resultado.

**Status:** ⬜
