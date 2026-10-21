# T10-11 — Condições de Pagamento

**Fase:** 2 · **Rota:** `/Auxiliares/CONDICOES_PAGTOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `Standards/un_Condicoes_Pagtos.pas` · menu *Cadastros → Cadastros Básicos → Condições de Pagamento* · 112 registro(s) no legado
**Objetos do banco:** [`CONDICOES_PAGTOS`](../banco/objetos/tabelas/CONDICOES_PAGTOS.md), [`CONDICOES_PAGTOS_PARC`](../banco/objetos/tabelas/CONDICOES_PAGTOS_PARC.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Código* | `CODIGO` | até 10, **único** por empresa — "O Código da Condição de Pagamento é obrigatório." |
| Descrição* | `DESCRICAO` | até 250 |
| Ativo | `ATIVO` | o legado tinha `INATIVO` (invertido na migração) |
| Parcelas (grid opcional) | ``CONDICOES_PAGTOS_PARC`` | nº, dias para vencimento, % — Σ % = 100 (nunca usado no legado) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- A proposta lista só as condições ativas (Delphi: `INATIVO = 0`).
- No Delphi, Enter no código vazio mostrava a mensagem de obrigatório.

## Onde é usado
Proposta (T20) e Pedido de venda (T21).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Código (maiúsculas) e descrição no CRUD genérico; código repetido → mensagem no campo Código (`UQ_CONDPAG`). Botão **Parcelas** na linha (`AcaoLinhaAuxiliar`, novo na definição) abre a tela própria `Views/CondicoesPagamento/Parcelas.cshtml` (`CondicoesPagamentoController` + `CondicaoPagamentoService`): grade com adicionar/remover, **dividir igualmente** e total ao vivo; servidor valida dias 0–3650, % > 0 com até 4 casas e Σ = 100% (vírgula ou ponto); sem parcelas é permitido; parcelas regravadas **no lugar** (o nº é `UNIQUE`). Consulta com `AUXILIARES.CONSULTAR`, gravação com `AUXILIARES.ALTERAR`. Testes: `Parcelas_somam_100_e_sao_regravadas_no_lugar`, `Tela_de_parcelas_e_permissoes`, navegador `Condicao_de_pagamento_com_parcelas`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1) — código + descrição no `CatalogoAuxiliares`; **parcelas** em tela própria (`/Auxiliares/CONDICOES_PAGTOS/Parcelas/{id}`, `CondicaoPagamentoService`: dias 0–3650, % > 0 com até 4 casas, Σ = 100%, sem parcelas permitido, regravadas no lugar). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
