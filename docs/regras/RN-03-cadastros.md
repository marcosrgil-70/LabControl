# RN-03 — Cadastros

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-03.01 | Auxiliares por empresa, com descrição/código únicos na empresa | B | D | 🟠 | [T10-00](../telas/T10-00-crud-auxiliares.md) |
| RN-03.02 | Excluir só registro sem uso; com uso, inativar | B S | L | 🟠 | [T10-00](../telas/T10-00-crud-auxiliares.md) |
| RN-03.03 | `AMOSTRAS_TIPO.CODIGO` e `ANALISES_TIPO.CODIGO` (1–99) compõem o número da amostra e são **imutáveis depois de usados** | B S T | N | 🔴 | [T10-14](../telas/T10-14-tipos-amostra.md), [T10-16](../telas/T10-16-tipos-analise.md) |
| RN-03.04 | Ao menos um status de amostra com "permite resultado" | S | N | 🟠 | [T10-15](../telas/T10-15-status-amostra.md) |
| RN-03.05 | Tipo de endereço tem finalidade (NF, Correspondência, Entrega, Cobrança) que orienta as sugestões da proposta | S | C | 🟠 | [T10-02](../telas/T10-02-tipos-endereco.md) |
| RN-03.06 | Condição de pagamento com código obrigatório e único; parcelas, se houver, somam 100% | B S | L | ⚪ | [T10-11](../telas/T10-11-condicoes-pagamento.md) |
| RN-03.07 | Prazo em dias calcula a data prevista dos testes (entrega + dias) | B | L | 🟠 | [T10-12](../telas/T10-12-prazos.md) |
| RN-03.08 | Geografia é global; cidades identificadas pelo código IBGE; só ADMIN_GLOBAL altera | B S | N | ⚪ | [T11-03](../telas/T11-03-cidades.md) |
| RN-03.09 | Endereço com logradouro/bairro em texto e cidade IBGE (fim do cadastro prévio de bairro/logradouro) | B | D | ⚪ | [T11-04](../telas/T11-04-bairros-logradouros.md) |
| RN-03.10 | CPF/CNPJ com dígito verificador e único por empresa | B S T | L | 🟠 | [T12](../telas/T12-clientes.md) |
| RN-03.11 | Entidade tem ao menos um tipo de cadastro (cliente, fornecedor, funcionário, comercial, transportador) | S T | L | ⚪ | [T12](../telas/T12-clientes.md) |
| RN-03.12 | Um endereço ativo por tipo; um telefone e um e-mail principal por entidade; e-mail único por entidade | B S | L | ⚪ | [T12](../telas/T12-clientes.md) |
| RN-03.13 | E-mails são os contatos da proposta (comercial, resultado, NF); flags `ENVIAR_LAUDO`/`ENVIAR_NFE` sugerem os contatos | S | L/N | 🟠 | [T12](../telas/T12-clientes.md), [T20](../telas/T20-propostas.md) |
| RN-03.14 | Excluir entidade só sem movimento (proposta/amostra) | B S | L | 🟠 | [T12](../telas/T12-clientes.md) |
| RN-03.15 | Profissional que assina boletim precisa de tipo, nº e UF de registro; só ele aparece como responsável | S | N | 🔴 | [T13](../telas/T13-profissionais.md) |
| RN-03.16 | Só profissional com "executa análise" aparece como analista | S | N | 🟠 | [T13](../telas/T13-profissionais.md) |
| RN-03.17 | Assinatura do profissional (PNG ≤ 300 KB + SHA-256) impressa só no boletim emitido | S | N | 🟠 | [T13](../telas/T13-profissionais.md) |
| RN-03.18 | Produto sem código recebe código automático de 6 dígitos; código único por empresa | B | L | ⚪ | [T14](../telas/T14-produtos.md) |
| RN-03.19 | Parâmetro de análise: descrição única, **descrição reduzida obrigatória** (texto do boletim), tipo de análise obrigatório | B S | C | 🔴 | [T15](../telas/T15-parametros-analise.md) |
| RN-03.20 | Parâmetro acreditado (`AUDITADO`) separa metodologias e define a acreditação do boletim | B | L | 🔴 | [T15](../telas/T15-parametros-analise.md) |
| RN-03.21 | Método padrão do parâmetro filtra o combo de método na proposta; valor unitário é o preço sugerido | T | L | ⚪ | [T15](../telas/T15-parametros-analise.md) |
| RN-03.22 | Cadastro rápido "+" (produto, método, idioma, prazo, tipo de resultado) devolve o item ao combo de origem | T | L | ⚪ | [T10-00](../telas/T10-00-crud-auxiliares.md) |
