# P08 — Pedido de Venda

**Criticidade:** 🔴 (liga proposta, amostra, boletim e faturamento) · **Fase:** 6 (geração mínima pode entrar na fase 4)
**Telas:** [T21 Pedidos de venda](../telas/T21-pedidos-venda.md), [T20 Propostas](../telas/T20-propostas.md), [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T27 Boletim](../telas/T27-boletim.md), [T25 Etiquetas](../telas/T25-etiquetas-amostras.md), [T30-01 Faturamento](../telas/T30-01-rel-faturamento.md)
**Objetos:** [`PEDIDOS_VENDA`](../banco/objetos/tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../banco/objetos/tabelas/PEDIDOS_VENDA_ITENS.md), [`VW_PEDIDOS_VENDA_SALDO`](../banco/objetos/views/VW_PEDIDOS_VENDA_SALDO.md), [`SP_GERAR_PEDIDO_VENDA`](../banco/objetos/rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../banco/objetos/rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md), [`TG_PEDVEN_BI`](../banco/objetos/triggers/TG_PEDVEN_BI.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md)
**Regras:** [RN-04](../regras/RN-04-propostas-pedidos.md) · **Decisão:** D-09 (pedido vira tabela, tela própria)

## Como era no legado
Botão "Ped.Venda" da proposta: exigia data de autorização (*"É obrigatório o preenchimento da Data Autorização para gerar o Pedido de Venda."*), gravava `ID_PEDIDOS_VENDAS = SP_GET_ULT_PED_VENDA` (sequência por empresa, não anual, sem bloqueio) **na revisão da proposta**; a revisão seguinte copiava o número (19 pedidos repetidos); a amostra copiava o número para `ID_LAB_PEDIDOS_VENDAS`; boletim e etiqueta imprimiam "PV".

## Estados
```
ABERTO ──(1ª amostra recebida)──► EM_EXECUCAO ──(itens recebidos + boletins emitidos, ou manual)──► CONCLUIDO
   └──────────────(motivo; sem amostras)──────────────► CANCELADO
```

## Fluxo: gerar
1. Origem: botão "Gerar pedido" da T20 ou "Novo pedido" da T21 (escolher proposta: última revisão, sem pedido, com análises).
2. Modal: data de autorização* (≤ hoje), autorizado por (sugere o contato da proposta), nº do pedido do cliente (PO).
3. `CALL SP_GERAR_PEDIDO_VENDA(...)` fora de transação EF → número sequencial (9 dígitos), itens = produtos da proposta (qtde contratada e valor), `LAB_PROPOSTAS.DT_AUTORIZACAO` gravada, amostras já recebidas da proposta passam a apontar para o pedido.
4. Tela mostra "Pedido de venda nº 000001472 gerado".

## Fluxo: amostra recebida
`TG_AMOSTRAS_BI` liga a amostra ao pedido não cancelado da proposta (qualquer revisão). O serviço do recebimento: se o pedido estiver ABERTO, muda para EM_EXECUCAO; se a quantidade recebida do item passar da contratada (`VW_PEDIDOS_VENDA_SALDO`), mostra **aviso** (não bloqueia).

## Fluxo: atualizar para revisão vigente
Cliente aprovou a R+1 → T21 "Atualizar" → `SP_ATUALIZAR_PEDIDO_REVISAO` (valor, cobrança, condição, moeda e itens da revisão atual); recusado se CONCLUIDO/CANCELADO.

## Fluxo: concluir / cancelar
- Concluir: manual (`PEDIDOS_VENDA.ALTERAR`) ou automático (banco, `PR_ATUALIZAR_SITUACAO_PEDIDO` na emissão do boletim) quando todos os itens estiverem recebidos e todas as amostras com boletim emitido. Revisão pendente reabre; "Reabrir" manual com motivo.
- Cancelar (`PEDIDOS_VENDA.CANCELAR`): motivo obrigatório; bloqueado se houver amostra; grava `DT_CANCELAMENTO`, `MOTIVO_CANCELAMENTO`, `AUDITORIA`.

## Cenários de teste
- Sem data de autorização → mensagem; data futura → "Data de Autorização não pode ser futura."
- Segundo pedido para a mesma proposta (qualquer revisão) → "Já existe Pedido de Venda para esta proposta."
- Amostra recebida antes do pedido passa a mostrar o nº do pedido depois da geração (roteiro doc 12 §7 item 4).
- Recebida × contratada correta com amostras em revisões diferentes da proposta.

## Implementação (6.2 — 27/10/2026)
- **Feito:** banco completo (tabelas, `TG_PEDVEN_BI`, `VW_PEDIDOS_VENDA_SALDO`, `SP_GERAR_PEDIDO_VENDA`, `SP_ATUALIZAR_PEDIDO_REVISAO`); ligação da amostra ao pedido e passagem ABERTO → EM_EXECUCAO no recebimento (4.3); conclusão dos pedidos migrados na M-17 (pendência 23, 5.5).
- **Feito na 6.2:** tela T21 (`PedidoVendaService`, `/PedidosVenda`), geração pelo modal da proposta, PDF (`PropostaPdf.GerarPedido`), conclusão e reabertura manuais (reabrir com motivo na auditoria), cancelamento.
- **Conclusão automática (pendência 24):** `PR_ATUALIZAR_SITUACAO_PEDIDO`, chamada pelo `SP_EMITIR_BOLETIM`, pelas revisões de amostra/resultado/boletim, pelo desfazer revisão de boletim e pelo `SP_GERAR_PEDIDO_VENDA`. O último boletim emitido conclui; uma revisão pendente reabre (decisão do usuário, 27/10/2026); desfazer a revisão conclui de novo. Amostra nova num pedido concluído também o reabre (`AmostraService`).
- **Revisão 6.3 (28/10/2026):** pedido **concluído à mão** volta a concluir sozinho depois da revisão que o reabriu (`CONCLUSAO_MANUAL`, decisão do usuário; "Reabrir" zera); "Atualizar para revisão vigente" recalcula a situação (contratado menor conclui); recebimento chama `PR_ATUALIZAR_SITUACAO_PEDIDO` (ABERTO → EM_EXECUCAO e reabertura com auditoria); amostra que troca de proposta passa para o pedido da proposta nova (`TG_AMOSTRAS_BU`); trocar produto atualiza o item do pedido (D-23/D-24).
- Detalhes e testes: [T21 › Implementação](../telas/T21-pedidos-venda.md#implementação-tarefa-62--27102026).
