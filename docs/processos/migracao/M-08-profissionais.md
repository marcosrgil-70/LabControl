# M-08 Profissionais

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-05](M-05-entidades.md), [M-03](M-03-auxiliares.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `ENTIDADES_FUNCIONARIOS` | 16 |
| `ENTIDADES_FUNC_ASSINATURAS` | 0 |

## Destino (MySQL `labcontrol_v2`)
[`ENTIDADES_FUNCIONARIOS`](../../banco/objetos/tabelas/ENTIDADES_FUNCIONARIOS.md)

## Regras de transformação
- `ID_CARGO_FUNCIONARIO` → `ID_CARGOS_FUNCIONARIOS` (dicionário).
- `ASSINA_BOLETIM = 1` para quem aparece como RESP/RESP2 em boletins; `EXECUTA_ANALISE = 1` para todos.
- Entidades marcadas como funcionário sem linha em `ENTIDADES_FUNCIONARIOS` (18 marcadas × 16 linhas): criar linha mínima e listar.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 16 profissionais + criados listados.

**Status:** ⬜
