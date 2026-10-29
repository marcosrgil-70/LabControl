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

## Implementação (tarefa 8.1)
`RelatoriosService.TempoAnalisesAsync`: dias = resultado − início + 1 (RN-09.06), prazo previsto = `DT_PREVISTA` do teste, atraso = dias do resultado além do prazo (0 no prazo; vazio sem prazo). Ordem pela data inicial. Total: média geral; tabela "Por parâmetro (dias)" com análises, média, mínimo, máximo e quantas em atraso. Filtros: parâmetro e tipo de análise. Teste: `RelatoriosTests.Parametros_tempo_e_producao_so_da_ultima_revisao_do_resultado`.

### Comparação com o legado (2017)
Legado: 10.072 linhas, média 174,6 dias; v2: 9.816 (só a última revisão do resultado), média 212,3 dias. As duas médias são distorcidas por **datas digitadas erradas no legado**: em 2017, 5 resultados vigentes têm a data inicial 0171-10-04, 0201-10-02 (2) e 1971-10-09 (2) — sem eles a média de 2017 é **5,0 dias**. No banco todo são 9 parâmetros em 8 amostras (também resultados em 2107 e 2201). Correção a decidir: **pendência 36**.


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
