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
- `COD_BOLETIM` = número **base** da amostra (sem " Rnn"), calculado pela `TG_BOLETIM_BI` e conferido com o impresso; `REVISAO` renumerada 0..n por código (ordem `REVISAO`, ID); `ULTIMA_REVISAO` na maior; `ID_BOLETIM_ORIGEM`.
- Resultados com 2 boletins (revisão reaproveitando o resultado): todos migram.
- `LAB_BOLETIM_OBS` → `TXT_OBSERVACAO`, `TXT_LEGENDA`, `TXT_METODOLOGIA_UTIL`, `TXT_METODOLOGIA_ACRED` (texto puro no legado); 4 boletins com duas → a mais recente.
- `DH_GERACAO` nulo → `DT_BOLETIM`; **`DH_EMISSAO = DH_GERACAO`** em todos (histórico congelado, D-11); status = `EMPRESAS_PARAMETROS.ID_BOLETINS_STATUS_EMITIDO` ("ENVIADO AO CLIENTE").
- Boletim com amostra diferente da do resultado (a `TG_BOLETIM_BI` recusa mesmo na migração): fica na **amostra do resultado** e o código é recalculado (decisão do usuário, 5.1).
- Linhas: `RESULTADO_CONCAT` mantido **como estava impresso** (não recalcular); `ORDEM` = ordem do ID no legado; `DESC_PARAMETRO` do legado; `ID_LAB_RESULTADOS_PARAM` pelo dicionário (com os repetidos fundidos na M-16). `TIPO_RESULTADO_DESC`, `UNIDADE_SG`, `ESPECIFICACAO` (= `ESPECIF_FORMATADA`) e `ACREDITADO` (= `AUDITADO` do parâmetro) vêm do parâmetro vinculado — o legado lia isso ao vivo na impressão.
- O legado deixava no boletim as linhas de parâmetros **apagados e relançados** (e de outro resultado): a linha ligada ao próprio resultado tem a vez; a antiga só entra, religada ao equivalente (parâmetro + método, ou a descrição reduzida quando o parâmetro foi apagado), se ainda não estiver no boletim.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 5.312 boletins / 19.469 linhas (19.615 − 146 não migradas, todas nas anomalias); 1 `ULTIMA_REVISAO = 1` por código; **todos emitidos**; nenhum código diferente do impresso sem explicação.

## Implementação (tarefa 5.1, 27/10/2026)
- `Services/Migracao/Etapas/M17Boletins.cs`, INSERT direto em lote por camada de revisão + um `UPDATE … JOIN` para os textos congelados das linhas.
- **7** boletins passados para a amostra do resultado; **3** códigos diferentes do impresso, todos explicados (1 amostra corrigida, 1 amostra do ano 0 renumerada na M-14, 1 em que o legado imprimiu `03-01585-01/17` para a amostra `03-01584-01/17`).
- Linhas: **143** religadas ao parâmetro equivalente; **146** não migradas — **118** antigas já cobertas pela linha atual, **22** repetidas (parâmetros fundidos na M-16), **6** de parâmetro apagado sem equivalente (boletins 864, 1276, 1277, 1278 e 4347); **2** de outro resultado sem equivalente mantidas.
- ~70 s. Com a migração desligada, alterar um boletim migrado é recusado ("Este boletim foi emitido…" — `MigracaoTests`).

**Status:** ✅ implementada na tarefa 5.1 (27/10/2026); conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
