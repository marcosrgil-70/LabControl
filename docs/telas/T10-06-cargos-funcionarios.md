# T10-06 — Cargos de Funcionários

**Fase:** 2 · **Rota:** `/Auxiliares/CARGOS_FUNCIONARIOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_Cargo_Funcionarios.pas` · menu *Cadastros → Cadastros Básicos → Cargos de Funcionários* · 9 registro(s) no legado
**Objetos do banco:** [`CARGOS_FUNCIONARIOS`](../banco/objetos/tabelas/CARGOS_FUNCIONARIOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 50, única por empresa |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Impresso abaixo do nome do responsável no boletim (ex.: "Farmacêutica Responsável").

## Onde é usado
Profissionais (T13) e boletim (T27).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ✅ concluída em 20/10/2026 — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1), antecipada para os cadastros com contatos tipados ([P25](../processos/P25-cadastro-entidades-contatos.md)); padrão testado em `AuxiliaresTests`, tela em `CadastrosTelasTests`.
