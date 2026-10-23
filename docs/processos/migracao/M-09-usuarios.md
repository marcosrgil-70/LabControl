# M-09 Usuários, empresas e perfis

**Parte:** 1 (fase 3.5) · **Criticidade:** 🔴 · **Depende de:** [M-08](M-08-profissionais.md), [M-04](M-04-parametros-empresa.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `USUARIO` | 15 |
| `SYS_*_ACESSOS` | 5.190 |

## Destino (MySQL `labcontrol_v2`)
[`USUARIOS`](../../banco/objetos/tabelas/USUARIOS.md), [`USUARIOS_EMPRESAS`](../../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`USUARIOS_PERFIS`](../../banco/objetos/tabelas/USUARIOS_PERFIS.md), [`PERFIS`](../../banco/objetos/tabelas/PERFIS.md)

## Regras de transformação
- `LOGIN = USUNOM` (maiúsculas, sem espaços); `USUCOD_LEGADO = USUCOD`.
- `SENHA_HASH` = PBKDF2 de uma senha provisória **aleatória por usuário** (exibida uma vez no relatório) e `TROCAR_SENHA = 1` (dúvida 7 do plano).
- `USUARIOS_EMPRESAS` (empresa 1) com `ID_ENTIDADES_FUNC` = funcionário mapeado.
- `USUADM = 1` → perfil ADMINISTRADOR; demais → perfil "OPERADOR" (criar com permissões a ajustar).
- `USUCOD = 1` (ADMINISTRADOR) → `ADMIN_GLOBAL = 1`. **Desenvolvimento/testes** (`Desenvolvimento:AdministradorComSenhaPadrao = true`, `appsettings.Development.json`): o ADMINISTRADOR fica com a senha `Admin123`, sem troca obrigatória e vinculado ao profissional "USUÁRIO ESPECIAL DO SISTEMA" (criado se não existir; a conferência de troca de senha desconta ele) (pedido do usuário, 23/10/2026 — CLAUDE.md); em produção recebe a senha provisória como os demais.
- `SYS_*_ACESSOS` não migram (substituídos por perfis); o relatório lista, por usuário, as telas que ele acessava para ajudar a montar os perfis.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 15 usuários; login de teste obriga troca de senha.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M09*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
