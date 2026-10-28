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

## Como foi planejado (6.2)
Levantamento feito antes de começar: [F6 › 6.2 › Como fazer](../plano/F6-propostas-pedidos.md#como-fazer-levantado-em-27102026-antes-de-começar).

## Aceite
- [x] Sem data de autorização não gera ("É obrigatório o preenchimento da Data Autorização para gerar o Pedido de Venda") — `PedidosVendaTests.Gerar_exige_autorizacao_e_aceita_um_pedido_por_proposta` e navegador `Pedido_gerado_pela_proposta_conclui_reabre_imprime_e_cancela`.
- [x] Um pedido por proposta (todas as revisões) — mesmo teste (a R1 recusa) + roteiro SQL passo 11.
- [x] Recebida × contratada correta — `Amostra_recebida_antes_entra_no_pedido_e_ultimo_boletim_emitido_conclui` (2/2) e a consulta (`Cancelar_exige_motivo…`, 0/2).
- [x] Amostra recebida mostra o nº do pedido (e o boletim imprime) — amostra recebida antes do pedido passa a apontar para ele (`PedidosVendaTests`, roteiro passo 4); o boletim imprime o `PEDIDO_VENDA` desde a 5.3.

## Implementação (tarefa 6.2 — 27/10/2026)
**Decisões do usuário (27/10/2026):** a revisão de boletim/resultado/amostra **reabre** o pedido concluído (volta a em execução e se conclui de novo quando a revisão for emitida); botão **"Reabrir"** com motivo; PDF no modelo do `frx_PedVenda` agora, com o layout fino validado depois pelos modelos reais (dúvida 5).

**Banco (pendência 24, skill `/mudanca-banco`):**
- `PR_ATUALIZAR_SITUACAO_PEDIDO(p_id_pedido)` (procedure 27): EM_EXECUCAO → CONCLUIDO quando cada item recebeu ≥ o contratado (amostras revisão 0 do item em qualquer revisão da proposta, como a `VW_PEDIDOS_VENDA_SALDO`) **e** nada está pendente; CONCLUIDO → EM_EXECUCAO quando algo ficou pendente. Pendente = amostra vigente do pedido sem boletim vigente emitido (pela amostra raiz) ou boletim vigente não emitido — o mesmo critério da `M17Boletins.ConcluirPedidosAsync` (5.5), com consultas por pedido (a view inteira passava dos 30 s na migração). ABERTO e CANCELADO não mudam. Grava `AUDITORIA` com o motivo ("Concluído automaticamente…"/"Reaberto automaticamente…").
- Chamada no fim do `SP_EMITIR_BOLETIM`, `SP_GERAR_REVISAO_AMOSTRA`, `SP_GERAR_REVISAO_RESULTADO`, `SP_GERAR_REVISAO_BOLETIM`, `SP_EXCLUIR_REVISAO_BOLETIM` e `SP_GERAR_PEDIDO_VENDA`, dentro da transação de cada uma.
- `SP_GERAR_PEDIDO_VENDA`: quando liga amostras já recebidas, o pedido nasce **em execução** (antes ficava aberto).
- Roteiro SQL passo 15 (6 verificações): **71/71**. `labcontrol_v2` com as rotinas reaplicadas (sem apagar dados).
- `AmostraService`: amostra **nova** num pedido concluído o reabre (em execução), além do aberto → em execução de antes.

**Aplicação:**
- `Services/PedidoVendaService.cs`:
  - consulta (nº, cliente, proposta em qualquer revisão, situação, período; recebidas/contratadas numa consulta);
  - tela (itens da `VW_PEDIDOS_VENDA_SALDO`, amostras vigentes com a situação do boletim, revisão vigente da proposta);
  - geração (data obrigatória e não futura, com a mensagem do legado, depois `SP_GERAR_PEDIDO_VENDA`);
  - observação; atualizar para a revisão vigente (`SP_ATUALIZAR_PEDIDO_REVISAO`); concluir manual (aberto/em execução);
  - reabrir: UPDATE + evento de auditoria com o motivo, na mesma transação;
  - cancelar: motivo obrigatório, recusado com amostra, grava data e motivo;
  - PDF.
- `Controllers/PedidosVendaController.cs` (`/PedidosVenda`): Index, Editar, Gerar (GET partial do modal / POST), Observacao, AtualizarRevisao, Concluir, Reabrir (`ALTERAR`), Cancelar (`CANCELAR`), Imprimir (`IMPRIMIR`).
- `Views/PedidosVenda/{Index,Form,_Gerar}.cshtml` + `wwwroot/js/pedidos-venda.js`.
- `ViewModels/PedidoVendaVM.cs`: `SituacoesPedido` com nome e cor da situação.
- **PDF:** `PropostaPdf.GerarPedido` (+ `PedidoImpresso`). O `frx_PedVenda` é a proposta sem contrato e sem AS, com "P.V. nº", "Ref. proposta" e "Autorizado em" no cabeçalho. Pedido cancelado sai com a marca "PEDIDO CANCELADO". A marca d'água (também a "REVISÃO SUBSTITUÍDA" da proposta) passou para o **fundo** da página: por cima, cobria valores. PDF conferido visualmente.
- **T20:** botão "Gerar pedido" (`PEDIDOS_VENDA.GERAR`, revisão vigente sem pedido) abre o modal; o nº do pedido virou link.
- **Achado no teste do navegador:** os campos do modal tinham os mesmos ids da proposta (`DtAutorizacao`), o que quebrava o `label for` e os scripts. Agora usam ids próprios (`gerar…`).

**Testes:**
- `PedidosVendaTests` (4): autorização e um pedido por proposta + revisão vigente; amostra anterior ao pedido + conclusão no último boletim + reabertura pela revisão de boletim e de amostra; item faltando + concluir/reabrir manuais + amostra nova reabre; cancelar + PDF + consulta.
- Navegador `Pedido_gerado_pela_proposta_conclui_reabre_imprime_e_cancela`.

**Status:** ✅ concluída na tarefa 6.2 (27/10/2026); falta rodar a suíte inteira (pendência 29). O layout fino do PDF depende dos modelos reais (dúvida 5).
