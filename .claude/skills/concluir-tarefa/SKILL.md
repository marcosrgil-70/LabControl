---
name: concluir-tarefa
description: Fecha uma tarefa do plano do LabControl (ex.: 1.6, 2.1, 4.3) — confere build e testes, marca os checklists do plano e da fase, atualiza o Status/Implementação do MD da tela, a matriz Delphi → C#, o Diário e o ponto de retorno, e prepara o commit. Use quando uma tarefa de docs/00-PLANO-EXECUCAO.md terminar ("concluir a tarefa", "fechar a 1.7", "marcar como feita", "atualizar o diário"), ou quando o usuário digitar /concluir-tarefa <número>.
---

# Concluir uma tarefa do plano

Argumento: o número da tarefa (ex.: `1.6`). Sem argumento, use a primeira tarefa não marcada (`- [ ]`) em `docs/00-PLANO-EXECUCAO.md` §5 e confirme com o usuário qual é.

Responda em português do Brasil. Datas no formato `dd/mm/aaaa` (data de hoje).

## 1. Conferir antes de marcar
1. `dotnet build` sem erros e **sem avisos novos**.
2. `dotnet test tests/LabControl.Tests` (a suíte inteira, ~5 min; o 1º run após mudar `Scripts/v2` leva ~3 min a mais). Anote o total `N/N`.
   - Falhou? **Não marque nada.** Mostre a saída ao usuário e corrija antes.
3. Se a tarefa criou/alterou tela:
   - toda action de escrita tem `[Permissao("TELA.ACAO")]` (ou `[SomenteAdminGlobal]`);
   - o item da tela existe em `Infra/Menu.cs` com o mesmo controller/action e permissão;
   - os **critérios de aceite** do MD da tela (`docs/telas/T*.md` › "Critérios de aceite") têm teste automatizado ou foram verificados — liste qual teste cobre cada um.
4. Mudou o banco nesta tarefa? Então a skill `mudanca-banco` precisa ter sido seguida (roteiro SQL, MDs dos objetos, entidades regeneradas).

## 2. Atualizar a documentação (sempre os 5 lugares)
| Onde | O que fazer |
|---|---|
| `docs/00-PLANO-EXECUCAO.md` §5 | `- [ ] X.Y …` → `- [x] X.Y …` |
| `docs/00-PLANO-EXECUCAO.md` › "▶ PONTO DE RETORNO" | data do título = hoje; acrescente a linha `- **Fase N:** X.Y concluída — <resumo de 1 linha>; testes N/N.`; troque a linha **Próxima tarefa** pela próxima não marcada, com link para a seção do arquivo da fase |
| `docs/00-PLANO-EXECUCAO.md` §9 Diário | nova linha **no fim da tabela**: `\| dd/mm/aaaa \| X.Y \| <o que foi feito: arquivos/serviços principais, decisões, achados e bugs evitados, total de testes> \| <modelo> \|` |
| `docs/plano/F<n>-*.md` | logo abaixo do título `### X.Y …`: `> ✅ **Concluída em dd/mm/aaaa** — ver [Tnn › Implementação](../telas/Tnn-….md#implementação-tarefa-xy); aceite em `<ClasseDeTeste>`.` |
| `docs/telas/Tnn-*.md` | marque `[x]` os critérios de aceite atendidos; acrescente `## Implementação (tarefa X.Y)` antes do Status (arquivos, regras aplicadas, decisões, classe de teste); `**Status:** ✅ concluída na tarefa X.Y (dd/mm/aaaa).` (ou 🟨 com o que falta e em qual tarefa) |
| `docs/plano/MATRIZ-DELPHI-CSHARP.md` | coluna de status da linha da tela: ⬜ → ✅ (ou 🟨 parcial) |

Regras:
- Use o Edit (ou heredoc `cat >>` para o Diário). **Não há Python** nesta máquina.
- Decisões do usuário e achados importantes vão no Diário **e** no MD da tela/processo correspondente.
- Dúvida da §7 do plano respondida? Risque-a com a resposta e a data.
- Tarefa de revisão (`/code-review`, `/security-review`): no Diário, número de achados, quais foram corrigidos e o que ficou para depois (e onde foi registrado).

## 3. Commit (só se o usuário pedir ou já tiver autorizado nesta sessão)
- Confira `git status` e `git diff --stat`; não inclua segredos (`appsettings*.json` sem senha; senha só em user-secrets).
- Mensagem no padrão do histórico: `<área>: <Tnn resumo curto> (tarefa X.Y)` — ex.: `parâmetros: T05 com numeração auditada e ajuste só ADMIN_GLOBAL (tarefa 1.6)`; corpo opcional com os pontos principais; termine com a linha de coautoria pedida pelo sistema.
- Branch: o projeto trabalha direto na `master` (histórico) — mantenha, a menos que o usuário peça outra coisa.

## 4. Fechamento de fase
Se a tarefa concluída for a **última da fase** (ou for a tarefa "Revisão da fase"), lembre o usuário dos comandos embutidos previstos no plano (§3 › Comandos úteis), sem rodá-los por conta própria:
- `/code-review` ao fim de cada fase (`high` nas fases 0, 4 e 5);
- `/security-review` ao fim da fase 1 e antes da virada;
- `/simplify` depois de fases grandes;
- `/run` para ver a tela funcionando.
E confira a "Definição de pronto" no fim do arquivo da fase.

## 5. Resposta ao usuário
Curta: o que foi concluído, total de testes, os arquivos de doc atualizados, a próxima tarefa (com o modelo/modo recomendado na §3/§5 do plano) e se há commit pendente.
