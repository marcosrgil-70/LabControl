# Fase 6 — Propostas Completas e Pedido de Venda

**Objetivo:** completar a proposta (seleção múltipla, revisão, contrato, PDF, trocar produto) e entregar a tela nova de pedidos de venda.
**Modelo/modo:** 🟢 Sonnet (📐 antes) · ✋ para revisão/pedido.
**Ler antes:** [P05](../processos/P05-ciclo-proposta.md) · [P06](../processos/P06-valores-proposta.md) · [P07](../processos/P07-revisao-proposta.md) · [P08](../processos/P08-pedido-venda.md) · [RN-04](../regras/RN-04-propostas-pedidos.md) · telas [T20](../telas/T20-propostas.md), [T21](../telas/T21-pedidos-venda.md)
**Depende de:** Fase 5 (pode ser antecipada depois da fase 4 se o comercial precisar).

---

### 6.1 T20 completa
> ✅ **Concluída em 27/10/2026** — ver [T20 › Implementação 6.1](../telas/T20-propostas.md#implementação-tarefa-61--proposta-completa-27102026); aceite em `PropostasTests`, `ImportadorTextoTests` e no navegador `Proposta_selecao_multipla_contrato_pdf_e_revisao`. Decisões: importar Word/HTML/TXT (permissão `PROPOSTAS.ALTERAR`); PDF com a Autorização de Serviço.
- **Passos:** seleção múltipla de parâmetros (modal com lista + campos padrão → N linhas); regra Alterar × Gerar revisão (bloqueio com envio/pedido/amostra; pergunta Sim/Não/Cancelar); **gerar revisão** (painel de revisões, motivo, `SP_GERAR_REVISAO_PROPOSTA`, destaque `ST_REV_ANTERIOR`); revisões antigas somente leitura; contrato (editor + importar arquivo + contrato padrão); **PDF da proposta**; trocar produto; excluir com regras.
- **Aceite:** [aceite da T20](../telas/T20-propostas.md#aceite).
- **Esforço:** G

### 6.2 T21 — Pedidos de venda
> ✅ **Concluída em 27/10/2026** — ver [T21 › Implementação](../telas/T21-pedidos-venda.md#implementação-tarefa-62--27102026); aceite em `PedidosVendaTests` e no navegador `Pedido_gerado_pela_proposta_conclui_reabre_imprime_e_cancela`; pendência 24 no banco (roteiro SQL passo 15, 71/71). Decisões: revisão reabre o pedido concluído; "Reabrir" com motivo; PDF no modelo do `frx_PedVenda`.
- **Passos:** gerar (modal a partir da proposta ou da T21; `SP_GERAR_PEDIDO_VENDA`); tela do pedido (cabeçalho, situação, itens contratado × recebido, amostras do pedido, observação); atualizar para revisão vigente; concluir (manual/automático após emissão de boletins); cancelar com motivo; **PDF do pedido**; consulta.
- **Aceite:** [aceite da T21](../telas/T21-pedidos-venda.md#aceite).
- **Esforço:** G · **Depende de:** 6.1

#### Como fazer (levantado em 27/10/2026, antes de começar)
**O que já existe e não precisa ser feito de novo:**
- **Banco:** tabelas `PEDIDOS_VENDA` (situação `ABERTO`/`EM_EXECUCAO`/`CONCLUIDO`/`CANCELADO`, `DT_CANCELAMENTO`, `MOTIVO_CANCELAMENTO`, `OBSERVACAO`; `UQ_PEDVEN_PROP` = um pedido por proposta em todas as revisões) e `PEDIDOS_VENDA_ITENS`; `TG_PEDVEN_BI` (número sempre do banco, sequência por empresa, não anual); view `VW_PEDIDOS_VENDA_SALDO` (contratado × recebido por item, contando amostras revisão 0); `SP_GERAR_PEDIDO_VENDA` (valida, numera, copia os produtos como itens, liga as amostras já recebidas, audita) e `SP_ATUALIZAR_PEDIDO_REVISAO` (recusa concluído/cancelado, troca a revisão, refaz os itens, audita).
- **Aplicação:** `Procedures.GerarPedidoVendaAsync` e `Procedures.AtualizarPedidoRevisaoAsync`; a trigger da amostra liga a amostra nova ao pedido não cancelado da proposta; o `AmostraService` passa o pedido de `ABERTO` a `EM_EXECUCAO` no recebimento e avisa quando o recebido passa do contratado (`AvisoContratadoAsync`); o boletim já imprime o nº do pedido (9 dígitos).
- **Permissões semeadas:** `PEDIDOS_VENDA.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR` (padrão) + `PEDIDOS_VENDA.GERAR` e `PEDIDOS_VENDA.CANCELAR`.
- **Migração:** M-13 traz os pedidos; a M-17 conclui os 974 com tudo recebido e emitido (pendência 23). A regra está em C# em `M17Boletins.ConcluirPedidosAsync` e serve de referência para a regra do banco.

**Passo a passo:**
1. **Pendência 24, primeiro (banco, skill `/mudanca-banco`):**
   - criar a `PR_ATUALIZAR_SITUACAO_PEDIDO(p_id_pedido)`: só mexe em pedido `EM_EXECUCAO`; vira `CONCLUIDO` quando cada item tem recebido ≥ contratado (amostras revisão 0 do item, como a view) **e** toda amostra vigente do pedido tem o boletim vigente emitido (mesmo critério da `M17Boletins.ConcluirPedidosAsync`). Usar consultas por pedido, não a view inteira: a view correlacionada estourou 30 s na migração;
   - chamar no fim do `SP_EMITIR_BOLETIM`, com o pedido da amostra;
   - decidir com o usuário se a revisão de um boletim (ou de uma amostra) de pedido concluído **reabre** o pedido para `EM_EXECUCAO`. Recomendação: sim, como na conclusão automática;
   - roteiro SQL: casos novos no passo dos pedidos; recriar `labcontrol_v2` (`-Dev`) e os bancos de teste; MDs gerados de novo (`tools/gerador-docs`);
   - opcional: a M-17 pode passar a chamar a procedure. Só trocar se o tempo da migração continuar aceitável; senão, manter o C#.
2. **Serviço `Services/PedidoVendaService.cs`:**
   - `ListarAsync` com os filtros nº, cliente, situação, período e nº da proposta; colunas recebidas/contratadas numa consulta agrupada, sem a view linha a linha;
   - `CarregarAsync`: cabeçalho, itens com o saldo, amostras do pedido (nº, data, status, boletim emitido ou não) e revisão vigente da proposta (para oferecer "Atualizar");
   - `GerarAsync`: validações na tela antes da procedure (data de autorização obrigatória e ≤ hoje, com a mensagem do legado do aceite) → `Procedures.GerarPedidoVendaAsync`;
   - `GravarObservacaoAsync`;
   - `AtualizarRevisaoAsync`;
   - `ConcluirAsync` (manual, só em execução ou aberto, com auditoria) e `ReabrirAsync` (concluído → em execução; confirmar com o usuário se entra);
   - `CancelarAsync`: motivo obrigatório; recusar se houver amostra ligada; grava `DT_CANCELAMENTO` e `MOTIVO_CANCELAMENTO`;
   - `DocumentoAsync` para o PDF.
   - As regras de situação que forem só de tela ficam no serviço. Se uma regra precisar valer também para o banco (ex.: cancelar com amostra), preferir trigger ou procedure, pelo `/mudanca-banco`.
3. **PDF `Services/Pedidos/PedidoVendaPdf.cs`** (QuestPDF, no padrão do `PropostaPdf`/`BoletimPdf`: licença no construtor estático, `PedidoVendaDocumento`):
   - layout do `frx_PedVenda` do legado (ler o `.dfm`/`.fr3` em `C:\Project\Laboratorio`): empresa (logo), nº do pedido, cliente/CNPJ/contato/fone/e-mail, datas de solicitação/validade, produtos e análises (método, prazo, idioma, quantidade, tipo de documento), totais e condição de pagamento;
   - pedido cancelado sai com a marca "CANCELADO";
   - reaproveitar a conversão de texto do `PropostaPdf` (`TextoSimples`);
   - conferir o PDF gerado visualmente. O layout fino fica para os modelos reais (dúvida 5).
4. **Controller `PedidosVendaController`** (`/PedidosVenda`):
   - actions `Index` (consulta), `Editar/{id}` (tela do pedido), `Gerar` (GET do modal com a proposta; POST com `PEDIDOS_VENDA.GERAR`), `Observacao` (`ALTERAR`), `AtualizarRevisao` (`ALTERAR`), `Concluir` (`ALTERAR`), `Cancelar` (`CANCELAR`) e `Imprimir` (`IMPRIMIR`);
   - todo POST com permissão de escrita: o `AutorizacaoActionsTests` recusa o contrário.
5. **Views `Views/PedidosVenda/{Index,Form,_Gerar}.cshtml` + `wwwroot/js/pedidos-venda.js`:**
   - o cabeçalho é só leitura, com link para a proposta;
   - a situação aparece como badge;
   - itens com contratado, recebido e saldo (saldo negativo em destaque);
   - aba com as amostras do pedido;
   - observação editável;
   - botões conforme a situação e a permissão; modais de cancelar (motivo) e de gerar (proposta, data de autorização, autorizado por — sugere o contato da proposta —, PO do cliente).
6. **Na proposta (T20):**
   - botão "Gerar pedido" (`PEDIDOS_VENDA.GERAR`) na revisão vigente sem pedido, abrindo o mesmo modal `_Gerar`;
   - com pedido, mostrar o link "Pedido 000000123".
   - Conferir se a proposta passa a ficar bloqueada para alteração sem revisão (a regra da 4.2 já trata "com pedido").
7. **Menu (T01):** item "Pedidos de venda", filtrado por `PEDIDOS_VENDA.CONSULTAR`, como as outras telas.
8. **Testes:**
   - `PedidosVendaTests`:
     - sem data de autorização não gera (mensagem do aceite);
     - um pedido por proposta, também a partir de outra revisão;
     - amostra recebida antes do pedido passa a apontar para ele;
     - recebido × contratado;
     - aberto → em execução no recebimento;
     - **conclusão automática ao emitir o último boletim** (pendência 24) e não conclusão com item faltando;
     - atualizar para a revisão vigente;
     - cancelar com e sem amostra;
     - PDF começa com `%PDF`.
   - Navegador: gerar pelo modal da proposta, abrir o pedido, ver o saldo, cancelar e imprimir.
   - Depois, a suíte inteira uma vez.
9. **Fechamento:**
   - `/concluir-tarefa 6.2`: T21 com a seção Implementação, o Status e o aceite marcado; P08; RN-04 se a regra mudar; F6; plano; matriz; Diário; READMEs;
   - pendência 24 riscada;
   - commit + push.

**Já decidido no [P08](../processos/P08-pedido-venda.md):** conclusão manual (`PEDIDOS_VENDA.ALTERAR`) além da automática; cancelamento com motivo obrigatório e **bloqueado se houver amostra**; mensagens "Data de Autorização não pode ser futura." e "Já existe Pedido de Venda para esta proposta.".

**Perguntas que estavam abertas (respondidas em 27/10/2026, todas com a recomendação):**
- a revisão de um boletim ou de uma amostra **reabre** o pedido concluído? Recomendação: sim, volta para em execução;
- haverá o botão "Reabrir" para o pedido concluído manualmente?
- o PDF do pedido segue o `frx_PedVenda` agora, com validação depois pelo modelo real (dúvida 5)?

### 6.3 Revisão
`/code-review high` de `98d43dd^..HEAD` (6.1 + 6.2); conferir números do pedido no boletim e nas etiquetas; olhar em especial a `PR_ATUALIZAR_SITUACAO_PEDIDO` (chamadas nas procedures de revisão) e o modal de geração.

## Definição de pronto
- [x] Proposta com revisão correta (sem copiar pedido/status) e PDF. — 6.1
> Dúvida 5 do plano (em aberto): pedir ao usuário os **modelos atuais (PDF/papel) da proposta e do pedido** antes de montar os PDFs.
- [x] Um pedido por proposta, recebido × contratado correto. — 6.2
