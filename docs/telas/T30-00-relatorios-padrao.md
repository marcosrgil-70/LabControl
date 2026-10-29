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

## Implementação (tarefa 8.1)
- **Arquivos:** `Controllers/RelatoriosController.cs` (uma action por relatório + `Pdf/{nome}` e `Csv/{nome}`), `Services/Relatorios/RelatoriosService.cs` (catálogo dos 6 relatórios, filtros, regras de contagem), `RelatorioDados.cs` (tabelas com linhas de dado/subtotal/total, CSV), `RelatorioPdf.cs` (QuestPDF), `ViewModels/RelatorioVM.cs`, `Views/Relatorios/Relatorio.cshtml` (uma view para os seis).
- **Um conjunto de dados, três saídas (RN-09.09):** o serviço devolve as células já formatadas em pt-BR; a tela, o PDF e o CSV mostram o mesmo texto.
- **Filtros:** um só VM para os seis; cada relatório mostra os seus (`FiltrosRelatorio`). Cliente, cobrança, produto, parâmetro, tipo de análise e analista são texto livre (nome ou CPF/CNPJ, código ou descrição), como nas outras consultas do v2. Período padrão: mês corrente (relógio do banco). Sem "Visualizar" a tela abre só com os filtros.
- **Permissões:** ver na tela = `RELATORIOS.CONSULTAR`; PDF e CSV = `RELATORIOS.IMPRIMIR` (botões só para quem tem). Os botões PDF/CSV enviam o formulário atual (os filtros digitados valem mesmo sem visualizar antes).
- **PDF:** A4 deitado, logo e razão social da empresa ativa, título, linha com o período e os filtros, tabelas com subtotal/total destacados, notas da regra de contagem, "Página x de y". **CSV:** UTF-8 com BOM e `;` (abre direto no Excel em português), uma linha em branco entre as tabelas.
- **Desvio do P20 (paginação no servidor):** os relatórios agrupam e totalizam o período inteiro, então a consulta traz tudo e a **tela corta em 1.000 linhas** (mantendo o total geral, com aviso); PDF e CSV levam tudo. 2017 inteiro (9.816 parâmetros) sai em menos de 1 s.
- **Desempenho:** amostras por período/cliente direto nas tabelas (agrupar o ano pela `VW_LAB_AMOSTRAS` levava 3,6 s por consulta; assim, 0,15 s).
- **Testes:** `RelatoriosTests` (5) e o de navegador `Relatorios_visualizam_filtram_e_exportam`.

## Comparação com o legado — ano de 2017 (`DBMAIS.FDB` × `labcontrol_migracao`)
| Relatório | Legado | v2 | Diferença explicada |
|---|---|---|---|
| T30-01 Faturamento | 620 linhas, 555 pedidos, R$ 344.743,13 | 555 pedidos, R$ 316.678,61 | linhas repetidas pela view (38 pedidos 2 ou 3 vezes) e valor da revisão antiga em 2 pedidos — ver T30-01 |
| T30-02/03 Amostras | 2.481 entradas, 2.373 analisadas | 2.455 / 2.347 (com pedido); 2.458 / 2.350 (todas) | revisões contadas como entradas e analisadas; filtro de pedido por cliente — ver T30-02 |
| T30-04 Parâmetros analisados | **0 linhas** (JOIN errado) | 9.816 | o do legado não funciona |
| T30-05 Tempo | 10.072 linhas, média 174,6 dias | 9.816, média 212,3 dias (5,0 sem os erros de digitação) | revisões + datas erradas no legado (pendência 36) |
| T30-06 Produção | 10.072 linhas | 9.816 | só a última revisão do resultado |


**Status:** ✅ concluída na tarefa 8.1 (29/10/2026).
