# RN-04 — Propostas e Pedidos de Venda

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-04.01 | Número `000123/26-R0` gerado pelo banco na inclusão (ano do servidor, sequência anual) | B | C | 🔴 | [P03](../processos/P03-numeracao-documentos.md) |
| RN-04.02 | Proposta nova nasce no status inicial da empresa, com digitador = profissional do usuário e validade = solicitação + dias padrão | S | L | 🟠 | [P05](../processos/P05-ciclo-proposta.md) |
| RN-04.03 | Validade não pode ser anterior à solicitação | B | N | ⚪ | [`TG_PROPOSTAS_BI`](../banco/objetos/triggers/TG_PROPOSTAS_BI.md) |
| RN-04.04 | Obrigatórios: status, cliente, entidade de cobrança, contato da NF, endereço do laudo, contato do resultado, condição de pagamento, ≥ 1 produto com ≥ 1 análise | B S T | L | 🔴 | [P05](../processos/P05-ciclo-proposta.md) |
| RN-04.05 | Entidade de cobrança pode ser outra; endereço e contato da NF são dela | S T | L | 🟠 | [T20](../telas/T20-propostas.md) |
| RN-04.06 | Mudar para o status "Enviado ao cliente" grava a data de envio | B | L (nunca funcionou) | 🟠 | [P05](../processos/P05-ciclo-proposta.md) |
| RN-04.07 | Status sem `PERMITE_AMOSTRA` impede recebimento de amostras | B | N | 🟠 | [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md) |
| RN-04.08 | Item numerado automaticamente; quantidade > 0; mudar a quantidade do produto muda a de todas as análises | B | L | 🟠 | [P06](../processos/P06-valores-proposta.md) |
| RN-04.09 | Linha: subtotal = qtde × unitário; % > 0 calcula desconto; total = subtotal − desconto; desconto ≤ subtotal | B | L | 🔴 | [P06](../processos/P06-valores-proposta.md) |
| RN-04.10 | Cabeçalho: somas das linhas; desconto geral (% ou valor); total = soma dos totais − desconto geral; recalculado pelo banco | B | L | 🔴 | [P06](../processos/P06-valores-proposta.md) |
| RN-04.11 | Seleção múltipla de parâmetros copia método, idioma, prazo e tipo de documento da linha base | S T | L | 🟠 | [T20](../telas/T20-propostas.md) |
| RN-04.12 | Com envio ao cliente, pedido ou amostra: alteração só por revisão; nas demais, pergunta Sim/Não/Cancelar | S T | L | 🔴 | [P05](../processos/P05-ciclo-proposta.md) |
| RN-04.13 | Revisão: motivo obrigatório, só da última, nasce no status inicial, sem datas de envio/autorização, linhas marcadas `ST_REV_ANTERIOR` | B | C | 🔴 | [P07](../processos/P07-revisao-proposta.md) |
| RN-04.14 | Revisão antiga somente leitura (cabeçalho, produtos, análises) | B | C | 🔴 | [`TG_PROPOSTAS_BU`](../banco/objetos/triggers/TG_PROPOSTAS_BU.md) |
| RN-04.15 | Excluir só revisão 0, sem revisões, sem pedido e sem amostras | S | L | 🟠 | [P05](../processos/P05-ciclo-proposta.md) |
| RN-04.16 | Excluir produto da proposta bloqueado se houver amostra recebida para o item | S | N | 🟠 | [T20](../telas/T20-propostas.md) |
| RN-04.17 | Trocar produto afeta só a proposta escolhida e as amostras dela | B | C | 🟠 | [`SP_TROCAR_PRODUTO_PROPOSTA`](../banco/objetos/rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md) |
| RN-04.18 | Pedido de venda: um por proposta (todas as revisões), gerado pela última revisão com análises e data de autorização ≤ hoje | B | L/C/D | 🔴 | [P08](../processos/P08-pedido-venda.md) |
| RN-04.19 | Número do pedido sequencial por empresa (não anual), 9 dígitos na exibição | B | L | 🟠 | [`TG_PEDVEN_BI`](../banco/objetos/triggers/TG_PEDVEN_BI.md) |
| RN-04.20 | Data de autorização desabilitada na proposta depois que há pedido | T | L | ⚪ | [T20](../telas/T20-propostas.md) |
| RN-04.21 | Amostras da proposta (recebidas antes ou depois) apontam para o pedido não cancelado | B | L | 🔴 | [P08](../processos/P08-pedido-venda.md) |
| RN-04.22 | Situação do pedido: ABERTO → EM_EXECUCAO (1ª amostra) → CONCLUIDO; CANCELADO com motivo e sem amostras | S | N | 🟠 | [P08](../processos/P08-pedido-venda.md) |
| RN-04.23 | Recebida acima da contratada gera aviso (não bloqueia) | S T | N | ⚪ | [`VW_PEDIDOS_VENDA_SALDO`](../banco/objetos/views/VW_PEDIDOS_VENDA_SALDO.md) |
| RN-04.24 | Pedido atualizável para a revisão vigente enquanto não concluído/cancelado | B | N | 🟠 | [`SP_ATUALIZAR_PEDIDO_REVISAO`](../banco/objetos/rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md) |
| RN-04.25 | Textos da proposta (itens solicitados/adquiridos, observação, contrato) em HTML; contrato padrão da empresa | S T | L | ⚪ | [T20](../telas/T20-propostas.md) |
