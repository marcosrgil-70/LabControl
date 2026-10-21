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
| Dias úteis | `DIAS_UTEIS` | **novo**, padrão não (cálculo em dias corridos, como o legado) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- `DT_PREVISTA` do teste = data de entrega + `QTDE_DIAS` (`SP_AMOSTRA_GERAR_TESTES` / `TG_TESTES_BI`).
- Se `DIAS_UTEIS` for usado no futuro, o cálculo passa para a aplicação/rotina (hoje o banco soma dias corridos).
- Cadastro rápido "+" na proposta.

## Onde é usado
Proposta (T20), testes da amostra (T22), fila de análises (T26).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Campos `Descricao`, `QtdeDias` (inteiro 0–3650) e `DiasUteis` (lógico, com aviso de que o cálculo ainda é em dias corridos). Teste: `Prazo_grava_dias_e_dias_uteis`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
