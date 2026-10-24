# P12 — Estoque e Saldo das Amostras

**Criticidade:** 🟠 · **Fase:** 4 (entrada automática) / 7 (movimentação manual)
**Telas:** [T23 Movimentação](../telas/T23-movimentacao-amostras.md), [T22 Recebimento](../telas/T22-recebimento-amostras.md)
**Objetos:** [`LAB_MOV_AMOSTRAS`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_MOV_AMOSTRAS_PARAM`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS_PARAM.md), [`LAB_HIST_AMOSTRAS_SALDO`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_SALDO.md), [`PR_ATUALIZA_SALDO`](../banco/objetos/rotinas/PR_ATUALIZA_SALDO.md), [`TG_AMOSTRAS_AI`](../banco/objetos/triggers/TG_AMOSTRAS_AI.md), [`TG_AMOSTRAS_AU`](../banco/objetos/triggers/TG_AMOSTRAS_AU.md), [`TG_MOV_BI`](../banco/objetos/triggers/TG_MOV_BI.md), [`TG_MOV_BD`](../banco/objetos/triggers/TG_MOV_BD.md), [`TG_MOV_AI`](../banco/objetos/triggers/TG_MOV_AI.md)
**Regras:** [RN-06](../regras/RN-06-estoque-arquivo.md) · **Decisão:** D-12 (estoque na raiz)

## Tipos de movimento
| Tipo | Legado (`AMOSTRA_COMPLEMENTAR`) | v2 | Origem |
|---|---|---|---|
| Entrada automática | `A` | `E_S = 'E'`, `AUTOMATICA = 1` | `TG_AMOSTRAS_AI` no recebimento (revisão 0) |
| Entrada complementar | `C` | `E_S = 'E'`, `AUTOMATICA = 0` | T23 (cliente enviou mais amostra) |
| Saída | `M` | `E_S = 'S'`, `AUTOMATICA = 0` | T23 (consumo para análise, envio a outro laboratório, devolução) |

## Saldo
`SALDO_ATUAL = Σ entradas − Σ saídas` da amostra **raiz**, recalculado por `PR_ATUALIZA_SALDO` após cada inclusão/alteração/exclusão de movimento (tabela separada porque o MySQL não deixa a trigger de movimento alterar a amostra).

## Fluxo
1. Recebimento grava a amostra → entrada automática com `QTDE_EMBALAGENS_ENTREGUE` → saldo.
2. Alterar a quantidade entregue no recebimento → atualiza a entrada automática (`TG_AMOSTRAS_AU`) — **bloqueado** se já houver movimento manual ("Não é permitido alterar a quantidade de amostras com movimentação.").
3. T23: saída com quantidade ≤ saldo e parâmetros atendidos → `TG_MOV_BI` redireciona para a raiz e recusa saldo insuficiente ("Saldo insuficiente da amostra para esta saída.") → saldo.
4. Excluir movimento manual → saldo recalculado; a automática não pode ser excluída.

## Erros do legado corrigidos
- Cada revisão da amostra gerava **nova entrada automática** (estoque em dobro) — agora só a revisão 0.
- Saldo negativo permitido.
- `LAB_HIST_AMOSTRAS_AU0` bloqueava **qualquer** alteração da amostra com movimento (até trocar o status) — agora só a quantidade.

## Migração (M-15)
Movimentos de revisões > 0 são **descartados** (eram entradas duplicadas); depois `CALL PR_ATUALIZA_SALDO` para cada raiz; conferir com `LAB_HIST_AMOSTRAS_SALDO` do legado.

## Cenários de teste
- Roteiro doc 12 §7 item 5: saldo 3 após receber 3.
- Saída de 5 com saldo 3 → recusada; saída de 2 → saldo 1.
- Movimento na R01 aparece na raiz.
- Excluir a entrada automática → "A entrada automática do recebimento não pode ser excluída."

## Implementação (até a 4.6, 24/10/2026)
- **Já funciona pelo banco:** entrada automática no recebimento (T22, `TG_AMOSTRAS_AI`) e saldo da raiz (`TG_MOV_AI` → `PR_ATUALIZA_SALDO`). Teste: saldo 3 após receber 3 (`AmostrasTests`).
- **Migração (M-15):**
  - 5.699 movimentos da amostra física;
  - as entradas automáticas duplicadas das revisões são descartadas;
  - movimento manual de revisão vai para a raiz (code-review 4.6);
  - saldos iguais aos do legado.
- **A fazer (7.1, T23):** tela de movimentação (saídas, saldo insuficiente, entrada automática não excluível).
