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
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
