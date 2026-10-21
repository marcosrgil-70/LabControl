# Fase 6 — Propostas Completas e Pedido de Venda

**Objetivo:** completar a proposta (seleção múltipla, revisão, contrato, PDF, trocar produto) e entregar a tela nova de pedidos de venda.
**Modelo/modo:** 🟢 Sonnet (📐 antes) · ✋ para revisão/pedido.
**Ler antes:** [P05](../processos/P05-ciclo-proposta.md) · [P06](../processos/P06-valores-proposta.md) · [P07](../processos/P07-revisao-proposta.md) · [P08](../processos/P08-pedido-venda.md) · [RN-04](../regras/RN-04-propostas-pedidos.md) · telas [T20](../telas/T20-propostas.md), [T21](../telas/T21-pedidos-venda.md)
**Depende de:** Fase 5 (pode ser antecipada depois da fase 4 se o comercial precisar).

---

### 6.1 T20 completa
- **Passos:** seleção múltipla de parâmetros (modal com lista + campos padrão → N linhas); regra Alterar × Gerar revisão (bloqueio com envio/pedido/amostra; pergunta Sim/Não/Cancelar); **gerar revisão** (painel de revisões, motivo, `SP_GERAR_REVISAO_PROPOSTA`, destaque `ST_REV_ANTERIOR`); revisões antigas somente leitura; contrato (editor + importar arquivo + contrato padrão); **PDF da proposta**; trocar produto; excluir com regras.
- **Aceite:** [aceite da T20](../telas/T20-propostas.md#aceite).
- **Esforço:** G

### 6.2 T21 — Pedidos de venda
- **Passos:** gerar (modal a partir da proposta ou da T21; `SP_GERAR_PEDIDO_VENDA`); tela do pedido (cabeçalho, situação, itens contratado × recebido, amostras do pedido, observação); atualizar para revisão vigente; concluir (manual/automático após emissão de boletins); cancelar com motivo; **PDF do pedido**; consulta.
- **Aceite:** [aceite da T21](../telas/T21-pedidos-venda.md#aceite).
- **Esforço:** G · **Depende de:** 6.1

### 6.3 Revisão
`/code-review`; conferir números do pedido no boletim e nas etiquetas.

## Definição de pronto
- [ ] Proposta com revisão correta (sem copiar pedido/status) e PDF.
> Dúvida 5 do plano (em aberto): pedir ao usuário os **modelos atuais (PDF/papel) da proposta e do pedido** antes de montar os PDFs.
- [ ] Um pedido por proposta, recebido × contratado correto.
