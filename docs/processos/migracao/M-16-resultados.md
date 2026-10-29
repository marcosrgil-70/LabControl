# M-16 Resultados

**Parte:** 3 (fase 5.1) · **Criticidade:** 🔴 · **Depende de:** [M-15](M-15-testes-estoque-arquivo.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_RESULTADOS_ANALISES` | 5.345 |
| `LAB_RESULTADOS_PARAM` | 19.528 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_RESULTADOS_ANALISES`](../../banco/objetos/tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md)

## Regras de transformação
- `REVISAO` renumerada 0..n **por amostra física (raiz)** (ordem: revisão da amostra, revisão do legado, ID); `ULTIMA_REVISAO` na maior; `ID_RESULTADO_ORIGEM` = anterior.
- Amostras com mais de um resultado: todas as revisões migram e são listadas.
- Parâmetros: teste pelo dicionário; `ID_PARAM_ORIGEM` = parâmetro do resultado anterior com o mesmo (parâmetro, método); `VR_EXP_*` double → inteiro; `DT_INICIAL`/`DT_RESULTADO` → DATE; IDs 0 → NULL.
- `RESULTADO_FORMATADO`/`ESPECIF_FORMATADA`: calculados pela `TG_RESPAR_BI`, que formata **também** com `@LC_MIGRACAO = 1` (não precisa de `UPDATE` em lote; conferido).
- Correções de erros do legado (decisões do usuário, 5.1):
  - `DT_RESULTADO` antes de `DT_INICIAL` (o v2 recusa — `CK_RESPAR_DATAS`): **início = data do resultado**;
  - teste de **outra amostra** (vizinha, erro de digitação): ligado ao teste equivalente (parâmetro + método) da amostra do resultado; sem equivalente único, mantido;
  - o **mesmo teste duas vezes** no resultado (`UQ_RESPAR_TESTE`): outro teste equivalente livre da amostra; sem ele, **uma linha só** — a impressa no boletim, senão a mais recente; a descartada aponta para a que ficou (a M-17 usa).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 5.345 resultados / 19.497 parâmetros (19.528 − 31 repetidos fundidos); 1 `ULTIMA_REVISAO = 1` por amostra física; nenhum parâmetro com valor sem texto formatado.

## Implementação (tarefa 5.1, 27/10/2026)
- `Services/Migracao/Etapas/M16Resultados.cs`, INSERT direto em lote, camada a camada (as FKs de origem precisam da revisão anterior gravada).
- **190** amostras físicas com mais de um resultado (o `VW_DUPLICADO` do legado contava 124 por revisão da amostra).
- **63** datas iniciais corrigidas; **60** parâmetros com teste de outra amostra (**51** religados ao equivalente, **9** mantidos sem equivalente único); **31** testes repetidos no mesmo resultado fundidos (**6** com valores diferentes — o descartado está na anomalia).
- ~60 s.

## Ano digitado errado (29/10/2026, pendência 36 — decisão do usuário)
`Conversoes.AnoDigitadoErrado`, antes da regra das datas invertidas: se uma das datas do parâmetro tem ano impossível (antes de 2010 — o legado começa em 2013 — ou depois de hoje) e a outra não, troca pelo ano da outra (ou o vizinho, na virada do ano) quando o intervalo fica coerente (início ≤ resultado, até 60 dias); cada correção vira anomalia. Ano impossível sem como corrigir também vira anomalia ("corrigir pela T26"). Na migração da 9.1: **16** anos corrigidos (0171, 0201, 0207, 0217, 0218, 1971, 2007, 2107, 2201, 2917…) e **57** datas invertidas (eram 63: 6 eram ano errado); sobrou 1 caso com as duas datas em 0320. Testes: `MigracaoConversoesTests.Ano_digitado_errado_no_resultado`.

**Status:** ✅ implementada na tarefa 5.1 (27/10/2026); conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`); ano digitado errado na 9.1 (29/10/2026).
