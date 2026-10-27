# P18 — Revisão e Desfazer de Boletim

**Criticidade:** 🔴 · **Fase:** 5
**Telas:** [T27 Boletim](../telas/T27-boletim.md)
**Objetos:** [`SP_GERAR_REVISAO_BOLETIM`](../banco/objetos/rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`PR_REVISAO_BOLETIM`](../banco/objetos/rotinas/PR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../banco/objetos/rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`FN_OBS_METODOS`](../banco/objetos/rotinas/FN_OBS_METODOS.md), [`TG_BOLETIM_BU`](../banco/objetos/triggers/TG_BOLETIM_BU.md), [`TG_BOLETIM_BD`](../banco/objetos/triggers/TG_BOLETIM_BD.md)
**Regras:** [RN-08](../regras/RN-08-boletim.md) · Erros B1–B9 do legado: [banco/12 §5](../banco/12-PROCEDURES-REVISOES.md#5-sp_gerar_revisao_boletim--sp_gerar_revisao_boletim)

## Gerar revisão
1. "Criar revisão" (`BOLETINS.REVISAR`) na última revisão (emitida ou não); motivo obrigatório.
2. `CALL SP_GERAR_REVISAO_BOLETIM(id, motivo, profissional, usuario, @novo, @rev)` fora de transação EF.
3. Banco:
   - origem identificada pelo **ID do boletim** (o legado buscava pelo resultado e quebrava com "multiple rows" — 120 casos);
   - se o resultado foi revisado depois do boletim, a revisão aponta para o **resultado vigente**;
   - `REVISAO` contínua por `COD_BOLETIM` (código constante, sem " Rnn");
   - nasce **não emitida**, data de hoje, gerador = profissional do usuário, status inicial, mesmos responsáveis e textos;
   - copia **só os parâmetros escolhidos** no boletim de origem (o legado copiava todos), na mesma ordem, com textos atualizados a partir do resultado vigente;
   - metodologias recalculadas por `FN_OBS_METODOS`;
   - anteriores `ULTIMA_REVISAO = 0`.
4. Tela abre a revisão nova em edição; reemissão segue [P17](P17-emissao-boletim.md).

## Revisão automática (cascata)
Também criada por [P11](P11-revisao-amostra.md) (revisão da amostra) e [P15](P15-revisao-resultado.md) (revisão do resultado, se marcado).

## Desfazer última revisão
`SP_EXCLUIR_REVISAO_BOLETIM(id)` (`BOLETINS.DESFAZER_REVISAO`): só a última e **não emitida**; reativa a anterior. Exclusão direta de revisão > 0 é recusada pelo banco.

## Cenários de teste
- Revisão de boletim emitido → rev+1 não emitida, editável, com os mesmos parâmetros.
- Desfazer revisão emitida → "Este boletim foi emitido. Não é permitido alterar ou excluir."
- Resultado revisado depois do boletim → revisão do boletim aponta para o resultado novo.
- Boletim migrado com 2 boletins para o mesmo resultado (120 casos) revisa sem erro.

## Implementação (tarefa 5.3, 27/10/2026)
`BoletimService.GerarRevisaoAsync`/`DesfazerRevisaoAsync` pelas procedures; a tela abre a revisão nova (não emitida) ou a anterior ao desfazer. Cobertos em `BoletinsTests.Revisao_nasce_nao_emitida…`: revisão de emitido, resultado revisado depois (aponta para o vigente), mesmos parâmetros, desfazer não emitida e recusa da emitida. O caso "migrado com 2 boletins para o mesmo resultado" fica para o `AceiteFase5Tests` (5.5). **Status:** ✅
