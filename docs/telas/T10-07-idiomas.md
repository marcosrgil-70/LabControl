# T10-07 — Idiomas

**Fase:** 2 · **Rota:** `/Auxiliares/IDIOMAS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadIdiomas.pas` · menu *Cadastros → Cadastros Básicos → Idiomas* · 4 registro(s) no legado
**Objetos do banco:** [`IDIOMAS`](../banco/objetos/tabelas/IDIOMAS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 40, única por empresa |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Cada análise da proposta tem idioma (obrigatório) e o teste da amostra herda.
- Cadastro rápido "+" na proposta (`Btn_IncCadIdioma`).
- O legado tem um idioma "---" (migra como está).
- Seed: Português, Inglês, Espanhol.

## Onde é usado
Proposta (T20), testes da amostra (T22).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ✅ concluída na tarefa 2.1 (20/10/2026) — primeiro auxiliar do CRUD genérico, visual aprovado pelo usuário; aceite em `AuxiliaresTests`.
