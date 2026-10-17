# M-17 Boletins

**Parte:** 3 (fase 5.1) · **Criticidade:** 🔴 · **Depende de:** [M-16](M-16-resultados.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_BOLETIM_ANALISES` | 5.312 |
| `LAB_BOLETIM_OBS` | 5.311 |
| `LAB_BOLETIM_PARAMETROS` | 19.615 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_BOLETIM_ANALISES`](../../banco/objetos/tabelas/LAB_BOLETIM_ANALISES.md), [`LAB_BOLETIM_PARAMETROS`](../../banco/objetos/tabelas/LAB_BOLETIM_PARAMETROS.md)

## Regras de transformação
- `COD_BOLETIM` = número **base** da amostra (sem " Rnn"); `REVISAO` renumerada 0..n por código (ordem `REVISAO`, ID); `ULTIMA_REVISAO` na maior; `ID_BOLETIM_ORIGEM`.
- 120 resultados com 2 boletins (revisão reaproveitando o resultado): migrar todos; listar.
- `LAB_BOLETIM_OBS` → `TXT_OBSERVACAO`, `TXT_LEGENDA`, `TXT_METODOLOGIA_UTIL`, `TXT_METODOLOGIA_ACRED`.
- `DH_GERACAO` nulo → `DT_BOLETIM`; **`DH_EMISSAO = DH_GERACAO`** em todos (histórico congelado, D-11); status = "ENVIADO AO CLIENTE".
- Parâmetros: `RESULTADO_CONCAT` mantido **como estava impresso** (não recalcular); `ID_LAB_RESULTADOS_PARAM` pelo dicionário; `ACREDITADO` do parâmetro.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 5.312 / 19.615; 1 `ULTIMA_REVISAO = 1` por código; todos emitidos.

**Status:** ⬜
