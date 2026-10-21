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

## Implementação (tarefa 2.3)
Aba Estados de `/Geografia` (`GeografiaService`): país obrigatório (combo; filtro por país na lista), nome em maiúsculas, sigla 2 letras (única por país → "Já existe um estado com esta sigla neste país." no campo Sigla), código IBGE 2 dígitos opcional; excluir só sem cidades. Só ADMIN_GLOBAL grava. Lista mostra o nº de cidades. Testes: `GeografiaTests`, navegador `Geografia_pais_estado_cidade_e_importacao_ibge`.

## Critérios de aceite
- [x] Usuário sem ADMIN_GLOBAL só consulta (mesmo teste da T11-01).
- [x] Sigla inválida/repetida e exclusão com cidades recusadas — `GeografiaTests.Pais_estado_e_cidade_com_validacoes_duplicidade_e_exclusao`.

**Status:** ✅ concluída na tarefa 2.3 (21/10/2026).
