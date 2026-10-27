# P15 — Revisão e Desfazer de Resultado

**Criticidade:** 🔴 · **Fase:** 5
**Telas:** [T26 Resultados](../telas/T26-resultados.md), [T27 Boletim](../telas/T27-boletim.md)
**Objetos:** [`SP_GERAR_REVISAO_RESULTADO`](../banco/objetos/rotinas/SP_GERAR_REVISAO_RESULTADO.md), [`PR_REVISAO_RESULTADO`](../banco/objetos/rotinas/PR_REVISAO_RESULTADO.md), [`PR_REVISAO_BOLETIM`](../banco/objetos/rotinas/PR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_RESULTADO`](../banco/objetos/rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md), [`TG_RESULT_BU`](../banco/objetos/triggers/TG_RESULT_BU.md), [`TG_RESULT_BD`](../banco/objetos/triggers/TG_RESULT_BD.md), [`TG_RESPAR_BU`](../banco/objetos/triggers/TG_RESPAR_BU.md)
**Regras:** [RN-07](../regras/RN-07-resultados.md) · Erros R1–R6 do legado: [banco/12 §4](../banco/12-PROCEDURES-REVISOES.md#4-sp_gerar_revisao_resultados--sp_gerar_revisao_resultado)

## Gerar revisão
1. "Criar revisão" (`RESULTADOS.REVISAR`) na última revisão do resultado; motivo obrigatório; checkbox **"Revisar também o boletim"** (padrão marcado).
2. `CALL SP_GERAR_REVISAO_RESULTADO(id, motivo, profissional, usuario, gerarBoletim, @novo, @rev, @bol)` fora de transação EF.
3. Banco: numeração **contínua por amostra física** (todas as revisões da raiz, com bloqueio); copia todas as linhas (`ID_PARAM_ORIGEM`); antigas `ULTIMA_REVISAO = 0`; se pedido e houver boletim, cria a revisão do boletim **apontando para o resultado novo** (o legado apontava para o antigo e listava valores antigos).
4. Tela abre a revisão nova em edição.

## Desfazer última revisão
- "Desfazer última revisão" (`RESULTADOS.DESFAZER_REVISAO`): `SP_EXCLUIR_REVISAO_RESULTADO(id)` — só a última e **sem boletim** ("Resultado possui boletim: exclua/revise o boletim primeiro."); reativa a anterior.
- Exclusão direta de revisão > 0 é recusada pelo banco ("Para excluir uma revisão use \"Desfazer revisão\".").

## Regras
- Revisão antiga do resultado: somente leitura.
- Com boletim emitido: o resultado não pode ser alterado; só por revisão.
- Se algum resultado não puder ser levado (teste removido), a revisão é cancelada com mensagem.
- `AUDITORIA` ação R com o motivo.

## Cenários de teste
- Roteiro doc 12 §7 item 9: resultado rev 2 + boletim rev 2.
- Desfazer com boletim → recusado; sem boletim → revisão anterior volta a ser a vigente.
- Alterar parâmetro de resultado com boletim emitido → "Resultado com boletim emitido: gere uma revisão do resultado."

## Implementação (tarefa 5.2, 27/10/2026)
`ResultadoService.GerarRevisaoAsync` (motivo + "Revisar também o boletim", padrão marcado) e `DesfazerRevisaoAsync` pelas procedures, fora da transação do EF; a tela abre a revisão nova (ou a anterior, ao desfazer). Cenários em `ResultadosTests.Boletim_emitido_exige_revisao…`. O item 9 do roteiro (cascata a partir da revisão da **amostra**) entra no `AceiteFase5Tests` (5.5). **Status:** ✅
