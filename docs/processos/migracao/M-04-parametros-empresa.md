# M-04 Parâmetros da empresa

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-03](M-03-auxiliares.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `EMPRESAS_PARAMETROS` | 1 |

## Destino (MySQL `labcontrol_v2`)
[`EMPRESAS_PARAMETROS`](../../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md)

## Regras de transformação
- Mapear os IDs de status pelos dicionários da M-03.
- `ID_LAB_PROPOSTAS_STATUS_ENVCLI` está **nulo** no legado → apontar para "ENVIADO AO CLIENTE" (ID 3).
- `ULT_*` **não** migram aqui (vão para `SEQUENCIAS_DOCUMENTOS` na M-18).
- Defaults v2: `DIAS_RETROATIVOS_ENTREGA = 7`, `DIAS_VALIDADE_PROPOSTA = 30`, `DIAS_PESQUISA_PADRAO = 365`.
- Ao final: `CALL SP_INICIALIZAR_EMPRESA(1)` (cria o que faltar sem duplicar, cria o perfil ADMINISTRADOR).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] Parâmetros apontam para status existentes da empresa 1.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M04*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoParte1Tests`).
