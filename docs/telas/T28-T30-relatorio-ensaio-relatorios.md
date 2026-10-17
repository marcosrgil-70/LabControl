# T28 — Relatório de Ensaio · T30 — Relatórios Gerenciais

**Fase:** 8

---

## T28 — Relatório de Ensaio
**Rota:** `/RelatorioEnsaio` · **Permissão:** `RELATORIO_ENSAIO.*` · **Legado:** `un_Relatorio_Ensaio.pas` (editor RTF livre, tabela vazia — nunca usado em produção) · **Tabela:** `LAB_RELATORIO_ENSAIO`
- Para amostras cujo item da proposta tem `TIPO_DOCUMENTO = RELATORIO` (em vez de boletim).
- Cabeçalho: nº da amostra*, tipo de amostra, tipo de análise, nome da amostra (só leitura); nº do RE (anual, gerado pelo banco).
- Corpo: editor HTML com modelo inicial (dados da amostra, testes e resultados prontos para editar).
- Emitir (mesma lógica do boletim: PDF + data de emissão + bloqueio).
- Consulta: cliente, proposta, amostra.
- **Confirmar com o usuário se ainda é necessário** antes de implementar (nunca foi usado).

---

## T30 — Relatórios Gerenciais
**Rota:** `/Relatorios/{nome}` · **Permissão:** `RELATORIOS.CONSULTAR/IMPRIMIR` · **Legado:** `un_RelatorioBasico` + 6 relatórios. Todos com período (data inicial/final), filtro da empresa ativa, visualização em tela, PDF e Excel (CSV).

| Relatório (menu Delphi) | Legado | Filtro de data | Colunas |
|---|---|---|---|
| Faturamento | `un_RelFaturamento` | **Data de autorização** da proposta | proposta, pedido, cliente, cobrança, condição de pagamento, valor total (agrupar por cliente, total geral) |
| Nº de amostras por período | `un_RelAmostrasPeriodo` | data de entrega | cliente, qtde de entrada, qtde analisada (com resultado) |
| Nº de amostras por cliente | `un_RelAmostrasCliente` | data de entrega + cliente | código, descrição (produto), entrada, analisada |
| Nº de parâmetros analisados | `un_RelParamAnalisados` | **data de finalização** (DT_RESULTADO) | data do resultado, cliente (código, CNPJ/CPF, nome), parâmetro (código, nome) — com totais por parâmetro |
| Tempo de duração das análises | `un_RelTempoAnalises` | data de finalização + parâmetro | data inicial, data do resultado, **tempo (dias)**, parâmetro — média por parâmetro |
| Produção por período | `un_RelProducao` | data de finalização | data, parâmetro, produto, qtde de amostra |

Novos sugeridos (validar com o usuário): testes em atraso (prazo × resultado), boletins emitidos por responsável, pedidos com saldo a receber.

Contar sempre só a **última revisão** de amostras/resultados (o legado contava revisões em dobro).

**Status:** 🟨 v1 tem 2 relatórios parciais — refazer
