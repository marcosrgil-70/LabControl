# M-15 Testes, estoque, arquivo e log

**Parte:** 2 (fase 4.1) · **Criticidade:** 🔴 · **Depende de:** [M-14](M-14-amostras.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_HIST_AMOSTRAS_TESTES` | 20.551 |
| `LAB_MOV_AMOSTRAS` | 5.770 |
| `LAB_MOV_AMOSTRAS_PARAM` | 0 |
| `LAB_LOCAL_AMOSTRAS` | 2.686 |
| `LAB_HIST_AMOSTRAS_LOG` | 10.509 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_HIST_AMOSTRAS_TESTES`](../../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_MOV_AMOSTRAS`](../../banco/objetos/tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_MOV_AMOSTRAS_PARAM`](../../banco/objetos/tabelas/LAB_MOV_AMOSTRAS_PARAM.md), [`LAB_LOCAL_AMOSTRAS`](../../banco/objetos/tabelas/LAB_LOCAL_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_SALDO`](../../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_SALDO.md), [`LAB_HIST_AMOSTRAS_LOG`](../../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md)

## Regras de transformação
- Testes: método/idioma/prazo/status pelos dicionários; `ID_LAB_PROPOSTAS_PRODANALISES` quando encontrado; `ID_TESTE_ORIGEM` = teste equivalente da revisão anterior por (parâmetro, método) quando único. 18 amostras com parâmetro repetido: casar por ordem.
- Movimentos: `AMOSTRA_COMPLEMENTAR = 'A'` → `AUTOMATICA = 1`; **movimentos de revisões > 0 descartados** (eram entradas duplicadas); demais apontam para a raiz.
- Localização: `STATUS 0 → ARQUIVADA, 1 → DESCARTADA`; `DT_HR_DESCATE → DT_HR_DESCARTE`; aponta para a raiz; 2 registros para a mesma raiz → fica o mais recente.
- Depois: `CALL PR_ATUALIZA_SALDO(id)` para cada raiz.
- Log (opcional): 45 colunas → JSON, `ACAO` I/A/E.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 20.551 testes.
- [ ] Saldo das raízes = `LAB_HIST_AMOSTRAS_SALDO` do legado (diferenças só onde havia duplicidade).

**Status:** ⬜
