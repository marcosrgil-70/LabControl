# T10-05 — Registros Profissionais

**Fase:** 2 · **Rota:** `/Auxiliares/TIPOS_REG_PROFISSIONAL` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadRegProfissional.pas` · menu *Cadastros → Cadastros Básicos → Registros Profissionais* · 2 registro(s) no legado
**Objetos do banco:** [`TIPOS_REG_PROFISSIONAL`](../banco/objetos/tabelas/TIPOS_REG_PROFISSIONAL.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 15, única (ex.: CRF, CRQ, CRBio) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Impresso no boletim junto do nº e da UF do registro do responsável ("CRF-SP 12345").
- Seed: CRF, CRQ.

## Onde é usado
Profissionais (T13) e boletim (T27).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
