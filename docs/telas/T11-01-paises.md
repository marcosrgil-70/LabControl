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
- [x] Usuário sem ADMIN_GLOBAL não vê Incluir/Alterar — `GeografiaTests.Telas_consulta_para_todos_e_gravacao_so_admin_global` (botões e modal ausentes; POST → 403).

## Implementação (tarefa 2.3)
Tela única `Views/Geografia/Lista.cshtml` com abas Países/Estados/Cidades (`GeografiaController` + `GeografiaService`). Consulta com `GEOGRAFIA.CONSULTAR`; gravar/excluir com `[SomenteAdminGlobal]`. Nome em maiúsculas (como o seed), sigla até 5, BACEN só dígitos (até 10); nome repetido → "Já existe um país com este nome." no campo Nome; excluir só sem estados ("O país tem estados cadastrados…"). Lista mostra o nº de estados. Testes: `GeografiaTests`, navegador `Geografia_pais_estado_cidade_e_importacao_ibge`.

**Status:** ✅ concluída na tarefa 2.3 (21/10/2026).
