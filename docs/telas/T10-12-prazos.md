# T10-12 — Prazos

**Fase:** 2 · **Rota:** `/Auxiliares/PRAZOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadPrazos.pas` · menu *Cadastros → Cadastros Básicos → Prazos* · 17 registro(s) no legado
**Objetos do banco:** [`PRAZOS`](../banco/objetos/tabelas/PRAZOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 150, única (ex.: "15 DIAS") |
| Qtde. de dias* | `QTDE_DIAS` | ≥ 0 (legado `QTDE`) |
| Dias úteis | `DIAS_UTEIS` | **novo**, padrão não. Marcado = conta só dias úteis; desmarcado = dias corridos, como o legado (decisão do usuário, 30/10/2026) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- `DT_PREVISTA` do teste = data de entrega + `QTDE_DIAS`, calculada pelo banco em [`FN_DATA_PREVISTA`](../banco/objetos/rotinas/FN_DATA_PREVISTA.md) (chamada por `SP_AMOSTRA_GERAR_TESTES` / `TG_TESTES_BI`) — D-29:
  - **"Dias úteis" marcado:** conta só dias úteis — pula sábado e domingo (feriados não entram: não há cadastro de feriados). Ex.: sexta 30/10 + 5 úteis = sexta 06/11; entrega no sábado conta a partir de segunda.
  - **Desmarcado:** dias corridos, como o legado (sexta 30/10 + 10 = segunda 09/11).
- A migração traz todos os prazos como dias corridos (o legado não tinha o campo).
- Cadastro rápido "+" na proposta.

## Onde é usado
Proposta (T20), testes da amostra (T22), fila de análises (T26).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Campos `Descricao`, `QtdeDias` (inteiro 0–3650) e `DiasUteis` (lógico; a ajuda explica úteis × corridos). Teste: `Prazo_grava_dias_e_dias_uteis`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

> **Dúvida 8 do plano — decidida pelo usuário em 30/10/2026:** se o prazo escolhido tiver "Dias úteis" marcado, a data prevista conta dias úteis; senão, dias corridos. Roteiro SQL passo 5 (3 verificações: sexta e sábado + 5 úteis, sexta + 10 corridos).

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
