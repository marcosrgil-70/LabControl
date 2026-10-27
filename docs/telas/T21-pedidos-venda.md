# T21 — Pedidos de Venda ⚠️ (tela nova, decisão do usuário)

**Fase:** 6 · **Rota:** `/PedidosVenda` · **Permissões:** `PEDIDOS_VENDA.CONSULTAR/IMPRIMIR/GERAR/ALTERAR/CANCELAR`
**Legado:** não havia tabela. Na proposta, o botão **Ped.Venda** (`Btn_PedVendaClick`) exigia **Data de Autorização** e gravava `ID_PEDIDOS_VENDAS = SP_GET_ULT_PED_VENDA` (sequência por empresa, **não anual**); a amostra copiava o número (`ID_LAB_PEDIDOS_VENDAS`) e o boletim o imprime (`PEDIDO_VENDA` 9 dígitos). Impressão: `frx_PedVenda`.
**Tabelas:** `PEDIDOS_VENDA`, `PEDIDOS_VENDA_ITENS`; views `VW_PEDIDOS_VENDA_SALDO`, `VW_LAB_PROPOSTAS`; rotinas `SP_GERAR_PEDIDO_VENDA`, `SP_ATUALIZAR_PEDIDO_REVISAO`.

## Referências cruzadas
- **Delphi:** `Btn_PedVendaClick` de `un_Propostas.pas` + `frx_PedVenda`
- **Objetos do banco:** [`PEDIDOS_VENDA`](../banco/objetos/tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../banco/objetos/tabelas/PEDIDOS_VENDA_ITENS.md), [`VW_PEDIDOS_VENDA_SALDO`](../banco/objetos/views/VW_PEDIDOS_VENDA_SALDO.md), [`SP_GERAR_PEDIDO_VENDA`](../banco/objetos/rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../banco/objetos/rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md), [`TG_PEDVEN_BI`](../banco/objetos/triggers/TG_PEDVEN_BI.md)
- **Processos:** [P08 Pedido de venda](../processos/P08-pedido-venda.md)
- **Regras de negócio:** [RN-04 Propostas e pedidos](../regras/RN-04-propostas-pedidos.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Gerar pedido (a partir da proposta)
Modal aberto pelo botão "Gerar Pedido" da T20 ou pela própria T21 ("Novo pedido" → escolher proposta):
| Campo | Regra |
|---|---|
| Proposta* | só **última revisão**, sem pedido, com análises |
| Data de autorização* | ≤ hoje (grava também `LAB_PROPOSTAS.DT_AUTORIZACAO`) |
| Autorizado por | nome da pessoa do cliente (sugere o contato da proposta) |
| Nº do pedido do cliente (PO) | opcional |
→ `SP_GERAR_PEDIDO_VENDA` → número `000000123` (9 dígitos), itens = produtos da proposta (qtde contratada e valor). Amostras já recebidas da proposta passam a apontar para o pedido.

## Tela do pedido
- Cabeçalho (só leitura, vem da proposta): nº, data, proposta (link), cliente, entidade de cobrança, condição de pagamento, moeda, valor total, autorização/autorizado por/PO.
- **Situação**: Aberto → Em execução (automático ao receber a 1ª amostra) → Concluído (manual, ou automático quando todos os itens estiverem recebidos e todos os boletins emitidos) → Cancelado (motivo obrigatório; bloqueado se houver amostra).
- **Itens**: produto, qtde contratada, **qtde recebida**, saldo a receber (`VW_PEDIDOS_VENDA_SALDO`) — o recebimento de amostras avisa quando a quantidade recebida passa da contratada.
- **Amostras do pedido**: lista com nº da amostra, data, status e situação do boletim.
- Observação (editável).
- **Atualizar para revisão vigente**: quando o cliente aprova uma revisão nova da proposta → `SP_ATUALIZAR_PEDIDO_REVISAO` (valores e itens da revisão atual).

## Consulta
Filtros: nº pedido, cliente, situação, período, nº proposta. Colunas: nº, data, proposta, cliente, valor, situação, recebidas/contratadas.

## Impressão
PDF "Pedido de Venda" (layout do `frx_PedVenda`): empresa, nº do pedido, cliente/CNPJ/contato/fone/e-mail, datas de solicitação/validade, produtos e análises (método, prazo, idioma, qtde, tipo de documento), totais e condição de pagamento.

## Como implementar (6.2)
Passo a passo detalhado, com o que já existe no banco e na aplicação, a pendência 24 (conclusão automática na emissão do boletim) e as perguntas para o usuário: [F6 › 6.2 › Como fazer](../plano/F6-propostas-pedidos.md#como-fazer-levantado-em-27102026-antes-de-começar).

## Aceite
- [ ] Sem data de autorização não gera ("É obrigatório o preenchimento da Data Autorização para gerar o Pedido de Venda").
- [ ] Um pedido por proposta (todas as revisões).
- [ ] Recebida × contratada correta.
- [ ] Amostra recebida mostra o nº do pedido (e o boletim imprime).

**Status:** ⬜
