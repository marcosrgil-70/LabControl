# T30-00 — Padrão dos Relatórios Gerenciais

**Fase:** 8 · **Rota:** `/Relatorios/{nome}` · **Permissões:** `RELATORIOS.CONSULTAR/IMPRIMIR` · **Criticidade:** ⚪
**Legado:** todos herdam de `Standards/un_RelatorioBasico.pas`: período (data inicial/final) com validação *"Período de datas inválido. Por favor, verificar."* (início e fim preenchidos e fim ≥ início), visualização FastReport (`Layout_Rel*.fr3`), exportação PDF/RTF (Word)/XLS (*"Deseja abrir o documento exportado?"*). O SQL era montado por **concatenação de strings**, com datas em `mm/dd/yyyy` e a empresa ativa.
**Processos:** [P20 Relatórios gerenciais](../processos/P20-relatorios-gerenciais.md) · **Regras:** [RN-09 Relatórios](../regras/RN-09-relatorios.md)

## Padrão web
- Filtros no topo (período obrigatório + filtros do relatório) e botão "Visualizar", que mostra a tabela na tela com totais. Botões **PDF** (QuestPDF; cabeçalho com logo e empresa, título, período e subtítulo dos filtros) e **CSV/Excel**.
- Período padrão: mês corrente.
- Consultas **parametrizadas** (nunca concatenar), sempre com o `ID_EMPRESAS` da empresa ativa.
- Contar **só a última revisão** de amostras e resultados (o legado contava revisões em dobro) — regras em [P20](../processos/P20-relatorios-gerenciais.md).
- Um `RelatorioService` por relatório, com teste que compara com o legado num período conhecido (diferenças esperadas documentadas).

## Relatórios
| Tela | Legado |
|---|---|
| [T30-01 Faturamento](T30-01-rel-faturamento.md) | `un_RelFaturamento` |
| [T30-02 Amostras por período](T30-02-rel-amostras-periodo.md) | `un_RelAmostrasPeriodo` |
| [T30-03 Amostras por cliente](T30-03-rel-amostras-cliente.md) | `un_RelAmostrasCliente` |
| [T30-04 Parâmetros analisados](T30-04-rel-parametros-analisados.md) | `un_RelParamAnalisados` |
| [T30-05 Tempo das análises](T30-05-rel-tempo-analises.md) | `un_RelTempoAnalises` |
| [T30-06 Produção por período](T30-06-rel-producao.md) | `un_RelProducao` |

Novos sugeridos (validar com o usuário): testes em atraso (prazo × resultado), boletins emitidos por responsável, pedidos com saldo a receber.

**Status:** ⬜ (v1 tem 2 relatórios parciais)
