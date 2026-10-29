# P20 — Relatórios Gerenciais (regras de contagem)

**Criticidade:** 🟠 (números para decisão e faturamento) · **Fase:** 8
**Telas:** [T30-00](../telas/T30-00-relatorios-padrao.md) a [T30-06](../telas/T30-06-rel-producao.md)
**Objetos:** [`VW_LAB_PROPOSTAS`](../banco/objetos/views/VW_LAB_PROPOSTAS.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`PEDIDOS_VENDA`](../banco/objetos/tabelas/PEDIDOS_VENDA.md)
**Regras:** [RN-09](../regras/RN-09-relatorios.md)

## Regras de contagem (valem para todos os relatórios)
| Conceito | Definição v2 | Erro do legado |
|---|---|---|
| Amostra recebida | amostra **física** = revisão 0 (`REVISAO = 0`) com `DT_ENTREGA` no período | revisões contavam como novas entradas; view duplicava linhas |
| Amostra analisada | amostra física com resultado cuja última revisão tem algum `DT_RESULTADO` no período (conta 1 vez) | contava por `NR_AMOSTRA` (com " R01" = outra amostra) |
| Parâmetro analisado | linha de `LAB_RESULTADOS_PARAM` da **última revisão** do resultado | JOIN entre ID de parâmetro e ID de teste (T30-04) |
| Tempo de análise | `DATEDIFF(DT_RESULTADO, DT_INICIAL) + 1` (inclusivo, como o legado) | — |
| Faturamento | 1 linha por pedido não cancelado, autorizado no período, valor da revisão vigente | pedido repetido em várias revisões contado várias vezes |
| Empresa | sempre a empresa ativa | ok (havia filtro) |

## Fluxo técnico
1. Filtros validados (período obrigatório, fim ≥ início — *"Período de datas inválido. Por favor, verificar."*).
2. `RelatorioXService` monta a consulta parametrizada (LINQ ou SQL com parâmetros) sobre as views.
3. Resultado em tela (paginação no servidor) com totais calculados no banco.
4. PDF (QuestPDF) e CSV a partir do mesmo DTO.

## Validação na homologação
Para cada relatório, rodar um período fechado (ex.: 2017) no legado e no v2; diferenças devem ser explicadas pelas correções acima (anotar no MD da tela).

## Implementação (tarefa 8.1, 29/10/2026)
`Services/Relatorios/RelatoriosService.cs` aplica as regras acima; a tela corta em 1.000 linhas (PDF/CSV com tudo) em vez de paginar, porque os relatórios totalizam o período inteiro. Comparação de 2017 com o legado e as diferenças explicadas em [T30-00](../telas/T30-00-relatorios-padrao.md#comparação-com-o-legado--ano-de-2017-dbmaisfdb--labcontrol_migracao). "Somente com pedido" (RN-09.08) vale por amostra — no legado era por cliente.
