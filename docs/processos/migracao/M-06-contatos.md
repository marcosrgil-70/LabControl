# M-06 Contatos (telefones e e-mails)

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-05](M-05-entidades.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `ENTIDADES_FONES` | 695 |
| `ENTIDADES_EMAILS` | 664 |

## Destino (MySQL `labcontrol_v2`)
[`ENTIDADES_FONES`](../../banco/objetos/tabelas/ENTIDADES_FONES.md), [`ENTIDADES_EMAILS`](../../banco/objetos/tabelas/ENTIDADES_EMAILS.md)

## Regras de transformação
- **Manter o dicionário de IDs dos e-mails**: são FKs de contato nas propostas (contato, contato do resultado, contato da NF).
- `PRINCIPAL`, `ENVIAR_NFE`, `INATIVO` → 1/0; `ENVIAR_LAUDO` (novo) = 1 para o e-mail usado como contato do resultado em propostas.
- E-mail repetido na mesma entidade: manter um e remapear o outro ID para ele.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 695 / 664.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M06*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
