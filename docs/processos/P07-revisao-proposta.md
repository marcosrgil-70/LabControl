# P07 — Revisão da Proposta

**Criticidade:** 🔴 · **Fase:** 6
**Telas:** [T20 Propostas](../telas/T20-propostas.md), [T21 Pedidos](../telas/T21-pedidos-venda.md)
**Objetos:** [`SP_GERAR_REVISAO_PROPOSTA`](../banco/objetos/rotinas/SP_GERAR_REVISAO_PROPOSTA.md), [`PR_REVISAO_PROPOSTA`](../banco/objetos/rotinas/PR_REVISAO_PROPOSTA.md), [`SP_RECALCULAR_PROPOSTA`](../banco/objetos/rotinas/SP_RECALCULAR_PROPOSTA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../banco/objetos/rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md), [`LAB_PROPOSTAS`](../banco/objetos/tabelas/LAB_PROPOSTAS.md) e filhas
**Regras:** [RN-04](../regras/RN-04-propostas-pedidos.md) · Erros P1–P9 do legado: [banco/12 §2](../banco/12-PROCEDURES-REVISOES.md#2-sp_gerar_revisao_propostas--sp_gerar_revisao_proposta)

## Quando
- Usuário escolhe "Gerar revisão" (permissão `PROPOSTAS.REVISAR`) — obrigatório quando a proposta foi enviada, tem pedido ou amostra; opcional nas demais (pergunta Sim/Não/Cancelar do Delphi).

## Fluxo
1. **Tela**: painel "Revisão" (Delphi `pnlRevisao`) lista as revisões da proposta (código, revisão, ano, situação, validade, valor); a origem padrão é a **última** (só ela pode ser revisada).
2. Usuário informa o **motivo** (obrigatório).
3. **Serviço**: `CALL SP_GERAR_REVISAO_PROPOSTA(id_origem, motivo, usuario, @id_nova, @rev)` **fora** de transação do EF.
4. **Banco** (transação da procedure):
   - bloqueia todas as revisões da proposta (`FOR UPDATE`) e calcula `REV = máx + 1`;
   - copia o cabeçalho com status **inicial**, `DT_ENVIO_CLIENTE` e `DT_AUTORIZACAO` **nulos**, `ID_LAB_PROPOSTAS_ORIGEM`, motivo;
   - marca as outras revisões `ULTIMA_REVISAO = 0` (passam a ser somente leitura);
   - copia textos (sem a autorização), produtos (nova chave, `ID_ORIGEM`) e análises remapeadas, com `ST_REV_ANTERIOR = 1`;
   - recalcula os totais.
5. **Tela**: abre a revisão nova em edição; linhas com `ST_REV_ANTERIOR = 1` em uma cor e linhas novas destacadas (Delphi `CustomDrawCell`).
6. Se houver pedido de venda e o cliente aprovar a revisão: T21 → "Atualizar para revisão vigente" (`SP_ATUALIZAR_PEDIDO_REVISAO`).

## Regras
- O pedido de venda **não** é copiado (um pedido por proposta, em `PEDIDOS_VENDA`); amostras continuam apontando para a revisão em que foram recebidas.
- Revisões antigas: cabeçalho, produtos e análises bloqueados pelas triggers.
- Amostras novas só podem usar a última revisão.
- `AUDITORIA` ação R com o motivo.

## Cenários de teste
- Roteiro doc 12 §7 item 10: `000001/26-R1`, 2 análises com `ST_REV_ANTERIOR = 1`, total recalculado.
- Tentar revisar uma revisão antiga → "Só é possível revisar…" (validação da tela) / origem inválida.
- Alterar análise da R0 depois da R1 → "Revisão antiga da proposta é somente leitura."
- Motivo vazio → "Informe o motivo da revisão."
