# P11 — Revisão da Amostra (cascata amostra → resultado → boletim)

**Criticidade:** 🔴🔴 · **Fase:** 4 (amostra) / 5 (cascata completa)
**Telas:** [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T26 Resultados](../telas/T26-resultados.md), [T27 Boletim](../telas/T27-boletim.md)
**Objetos:** [`SP_GERAR_REVISAO_AMOSTRA`](../banco/objetos/rotinas/SP_GERAR_REVISAO_AMOSTRA.md), [`PR_REVISAO_AMOSTRA`](../banco/objetos/rotinas/PR_REVISAO_AMOSTRA.md), [`PR_REVISAO_RESULTADO`](../banco/objetos/rotinas/PR_REVISAO_RESULTADO.md), [`PR_REVISAO_BOLETIM`](../banco/objetos/rotinas/PR_REVISAO_BOLETIM.md), [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md), [`TG_MOV_BI`](../banco/objetos/triggers/TG_MOV_BI.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md) · Erros A1–A9 do legado: [banco/12 §3](../banco/12-PROCEDURES-REVISOES.md#3-sp_gerar_revisao_amostra--sp_gerar_revisao_amostra)

## Conceito (ISO/IEC 17025)
Documento com resultado lançado não é alterado: gera-se uma revisão, a anterior fica intacta e somente leitura, e o motivo fica registrado.

## Quando
- Menu "Criar revisão" da T22 (`AMOSTRAS.REVISAR`).
- **Só com resultados** (sem resultado a amostra é alterada diretamente): *"Não existe Resultados para esta Amostra. A Amostra pode ser alterada."*
- Só a **última** revisão pode ser revisada.

## Fluxo
1. Tela pede o **motivo** (obrigatório).
2. Serviço: `CALL SP_GERAR_REVISAO_AMOSTRA(id, motivo, profissional, usuario, @nova, @rev, @res, @bol)` fora de transação EF.
3. Banco (uma transação):
   1. bloqueia as revisões da amostra (empresa, ano, código) e calcula `REVISAO = máx + 1`;
   2. copia **todas** as colunas (inclusive coletor e data da coleta — o legado perdia em 61 revisões), com `ID_AMOSTRA_ORIGEM`, `ID_AMOSTRA_RAIZ` (= revisão 0), motivo; digitador = profissional do usuário; número `… R01`;
   3. marca as outras revisões `ULTIMA_REVISAO = 0`;
   4. copia os testes com `ID_TESTE_ORIGEM`;
   5. **não** cria entrada de estoque (estoque, saldo e localização ficam na raiz);
   6. se houver resultado: `PR_REVISAO_RESULTADO` leva a **última** revisão do resultado para a amostra nova (testes mapeados por `ID_TESTE_ORIGEM`), e se houver boletim: `PR_REVISAO_BOLETIM` cria a revisão **não emitida** apontando para o resultado novo, com os mesmos parâmetros escolhidos e metodologias recalculadas.
4. Tela abre a amostra `… R01` em edição e informa: "Revisão R01 criada; resultado rev N e boletim rev M também foram revisados".

## Depois da revisão
- A revisão nova pode ser alterada (exceto tipo/proposta/produto, pois há resultado).
- Resultados/boletim da revisão nova são editáveis até a nova emissão.
- Telas operacionais (Resultados, Boletim, Etiquetas, Movimentação) listam só `ULTIMA_REVISAO = 1`.

## Cenários de teste
- Roteiro doc 12 §7 item 8: amostra `… R01` (raiz = 1), resultado rev 1 na amostra nova, boletim rev 1 **não emitido** com 1 parâmetro apontando para o resultado novo; versões antigas com `ULTIMA_REVISAO = 0`.
- Revisar sem resultado → recusado; revisar revisão antiga → "Só é possível revisar a última revisão da amostra."
- Movimento lançado na R01 vai para a raiz; saldo único.
- Amostra com o mesmo parâmetro duas vezes (18 casos no legado) revisa sem erro.

## Implementação (tarefa 4.3, 23/10/2026)
- **T22:** "Gerar revisão" só com resultados e só na última revisão, motivo obrigatório.
- **Execução:** `SP_GERAR_REVISAO_AMOSTRA` pelo `Procedures`, **fora** da transação do EF; a tela abre a `… R01`.
- **Bloqueios:** revisão antiga e amostra com boletim emitido ficam somente leitura.
- **Testes:**
  - `AmostrasTests.Com_resultado_…`: R01 com coletor/lote, raiz = revisão 0, estoque só na raiz, recusas;
  - `AceiteFase4Tests`: datas parciais copiadas.
- **A fazer (5.2/5.3):** revisão do resultado e do boletim pelas telas próprias (T26/T27).
