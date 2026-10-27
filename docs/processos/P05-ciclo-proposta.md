# P05 — Ciclo de Vida da Proposta

**Criticidade:** 🔴 · **Fases:** 4 (essencial) e 6 (completa)
**Telas:** [T20 Propostas](../telas/T20-propostas.md), [T21 Pedidos](../telas/T21-pedidos-venda.md), [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T12 Clientes](../telas/T12-clientes.md)
**Objetos:** [`LAB_PROPOSTAS`](../banco/objetos/tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_TEXTOS`](../banco/objetos/tabelas/LAB_PROPOSTAS_TEXTOS.md), [`LAB_PROPOSTAS_PRODUTOS`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODANALISES.md), [`LAB_PROPOSTAS_STATUS`](../banco/objetos/tabelas/LAB_PROPOSTAS_STATUS.md), [`ENTIDADES_EMAILS`](../banco/objetos/tabelas/ENTIDADES_EMAILS.md), [`ENTIDADES_ENDERECOS`](../banco/objetos/tabelas/ENTIDADES_ENDERECOS.md), [`TG_PROPOSTAS_BI`](../banco/objetos/triggers/TG_PROPOSTAS_BI.md), [`TG_PROPOSTAS_BU`](../banco/objetos/triggers/TG_PROPOSTAS_BU.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../banco/objetos/rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
**Regras:** [RN-04](../regras/RN-04-propostas-pedidos.md)

## Estados
```
[Em elaboração] ──► [Aguardando informação do cliente] ──► [Enviado ao cliente] ──(data de autorização)──► Pedido de venda (T21)
       ▲                                                          │
       └────────────── Gerar revisão (R+1 nasce no status inicial) ◄┘   (alteração com pedido/amostra/envio = só por revisão)
```
- Status inicial = `EMPRESAS_PARAMETROS.ID_LAB_PROPOSTAS_STATUS_INICIAL`.
- Entrar no status `ID_LAB_PROPOSTAS_STATUS_ENVCLI` grava `DT_ENVIO_CLIENTE = hoje` (trigger) se estiver vazia. **No legado o parâmetro estava nulo — a regra nunca funcionou** (migração deve apontar o status "Enviado ao cliente").
- `PERMITE_AMOSTRA = 0` no status impede recebimento de amostras.

## Fluxo: incluir
1. Incluir: status inicial, digitador = profissional do usuário, `DT_SOLICITACAO` = hoje, `DT_VALIDADE` = hoje + `DIAS_VALIDADE_PROPOSTA`; foco no Comercial.
2. Cliente → carrega contatos (e-mails) e endereços; sugestões: cobrança = o próprio cliente; endereço do laudo = finalidade CORRESPONDENCIA; endereço da cobrança = COBRANCA/NOTA_FISCAL; contato do resultado = e-mail com `ENVIAR_LAUDO`; contato da NF = e-mail com `ENVIAR_NFE`.
3. Produtos e análises ([P06](P06-valores-proposta.md)).
4. Gravar: valida (mensagens do legado) status ("Selecionar o Status para a Proposta."), empresa de cobrança ("Selecionar a Empresa para Cobrança."), contato da NF ("Selecionar o Contato para Cobrança."), endereço do laudo ("Selecionar o endereço para Laudo."), contato do resultado ("Selecionar o Contato para Laudo."), condição de pagamento, ≥ 1 produto com ≥ 1 análise.
5. INSERT cabeçalho (número pelo banco, [P03](P03-numeracao-documentos.md)) + textos + produtos + análises numa transação; relê e mostra "Nr. da Proposta: 000123/26-R0".

## Fluxo: alterar
| Situação da proposta | Comportamento |
|---|---|
| Revisão antiga (`ULTIMA_REVISAO = 0`) | somente leitura (banco recusa: "Revisão antiga da proposta é somente leitura.") |
| Enviada ao cliente, com pedido de venda ou com amostra recebida | alteração direta bloqueada → botão **Gerar revisão** ([P07](P07-revisao-proposta.md)) |
| Demais | pergunta como no Delphi: **[Sim]** gerar revisão · **[Não]** alterar sem revisão · **[Cancelar]** |

- Data de autorização desabilitada se já houver pedido.
- Excluir produto com amostra recebida para o item → bloqueado.

## Fluxo: excluir
Só a revisão 0, sem revisões posteriores ("Existe revisão para a Proposta nº … não será permitido"), sem pedido e sem amostras. Cascade apaga textos, produtos e análises.

## Utilitário "Trocar produto"
Produto antigo → novo na proposta e nas amostras **dessa** proposta (`SP_TROCAR_PRODUTO_PROPOSTA`); o legado (`SP_TROCAPRODUTO`) alterava amostras de todas as propostas.

## Cenários de teste
- Gravar sem contato da NF → mensagem do legado.
- Proposta com amostra: "Alterar" leva a "Gerar revisão".
- Mudar status para "Enviado ao cliente" grava a data de envio.
- Proposta em status com `PERMITE_AMOSTRA = 0` não aparece no recebimento.

## Implementação (tarefa 4.2 — parte essencial, 23/10/2026)
- **Já existe:**
  - consulta (só a última revisão por padrão);
  - inclusão e alteração sem revisão (cabeçalho, textos, produtos e análises numa transação), com as mensagens do legado nos obrigatórios;
  - sugestões do cliente (endereço por finalidade, contato por `ENVIAR_LAUDO`/`ENVIAR_NFE`, cobrança = o próprio cliente);
  - exclusão (revisão 0, sem revisões/pedido/amostras).
- **Novas validações:** contato/endereço do cliente ou da cobrança; endereço da cobrança obrigatório; desconto geral ≤ total; usuário sem profissional não inclui.
- **Bloqueio:** revisão antiga, com pedido, com amostra ou enviada → somente leitura com o motivo.
- **6.1 (27/10/2026):** "Gerar revisão" (P07), seleção múltipla, contrato (padrão e importar Word/HTML/TXT), PDF com a Autorização de Serviço e trocar produto. Ver [T20 › Implementação 6.1](../telas/T20-propostas.md#implementação-tarefa-61--proposta-completa-27102026).
