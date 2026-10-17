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
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
