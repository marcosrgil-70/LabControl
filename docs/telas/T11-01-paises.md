# T11-01 — Países

**Fase:** 2 · **Rota:** `/Geografia/Paises` · **Permissão:** `GEOGRAFIA.*` (inclusão/alteração só ADMIN_GLOBAL) · **Criticidade:** ⚪
**Legado:** `Standards/un_Paises.pas` (menu *Cadastros → Cadastros Básicos → País*) — campos Nome, Sigla, Código BACEN. 1 registro (Brasil).
**Objetos do banco:** [`PAISES`](../banco/objetos/tabelas/PAISES.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Nome* | `NOME` | até 80, único |
| Sigla | `SIGLA` | até 5 (ex.: BR) |
| Código BACEN | `CODIGO_BACEN` | até 10 (Brasil = 1058) |

## Regras
- Tabela **global** (sem empresa): igual para todas as empresas. Usuários comuns só consultam.
- Seed: BRASIL (`ID_PAISES = 1`).
- Excluir só sem estados vinculados.

## Critérios de aceite
- [ ] Usuário sem ADMIN_GLOBAL não vê Incluir/Alterar.

**Status:** ⬜
