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
- Concluir: manual (`PEDIDOS_VENDA.ALTERAR`) ou automático (serviço, após emissão de boletim) quando todos os itens estiverem recebidos e todas as amostras com boletim emitido.
- Cancelar (`PEDIDOS_VENDA.CANCELAR`): motivo obrigatório; bloqueado se houver amostra; grava `DT_CANCELAMENTO`, `MOTIVO_CANCELAMENTO`, `AUDITORIA`.

## Cenários de teste
- Sem data de autorização → mensagem; data futura → "Data de Autorização não pode ser futura."
- Segundo pedido para a mesma proposta (qualquer revisão) → "Já existe Pedido de Venda para esta proposta."
- Amostra recebida antes do pedido passa a mostrar o nº do pedido depois da geração (roteiro doc 12 §7 item 4).
- Recebida × contratada correta com amostras em revisões diferentes da proposta.
