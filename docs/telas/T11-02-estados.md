# T11-02 — Estados (UF)

**Fase:** 2 · **Rota:** `/Geografia/Estados` · **Permissão:** `GEOGRAFIA.*` (inclusão/alteração só ADMIN_GLOBAL) · **Criticidade:** ⚪
**Legado:** `Standards/un_Estados.pas` (menu *Cadastros Básicos → Estados*) — País, Nome, Sigla, Código IBGE. 28 registros.
**Objetos do banco:** [`ESTADOS`](../banco/objetos/tabelas/ESTADOS.md), [`PAISES`](../banco/objetos/tabelas/PAISES.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| País* | `ID_PAISES` | combo |
| Nome* | `NOME` | até 100 |
| Sigla* | `SIGLA` | 2 letras, única por país |
| Código IBGE | `CODIGO_IBGE` | 2 dígitos (SP = 35) |

## Regras
- Global; seed com as 27 UFs e códigos IBGE.
- A migração casa as UFs do legado **pela sigla** (o legado tinha 28 linhas — conferir a duplicada/extra no relatório).
- Excluir só sem cidades vinculadas.

**Status:** ⬜
