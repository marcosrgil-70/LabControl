# T20 — Propostas ⚠️ (tela com particularidades)

**Fase:** 6 (o usuário pediu começar por cadastros → recebimento → resultados → boletim; para receber amostras é preciso **proposta**: a fase 4 cria uma versão mínima e a fase 6 completa a tela)
**Rota:** `/Propostas` · **Permissões:** `PROPOSTAS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR/REVISAR`
**Legado:** `un_Propostas.pas` (2.138 linhas) + `undm_Propostas.pas`; relatórios `Layout_ImpProposta.fr3` e `Layout_ImpPedVenda.fr3`.
**Tabelas:** `LAB_PROPOSTAS`, `LAB_PROPOSTAS_TEXTOS`, `LAB_PROPOSTAS_PRODUTOS`, `LAB_PROPOSTAS_PRODANALISES`; views `VW_LAB_PROPOSTAS`, `VW_LAB_PROPOSTAS_ANALISES`; rotinas `SP_GERAR_REVISAO_PROPOSTA`, `SP_RECALCULAR_PROPOSTA` (trigger), `SP_TROCAR_PRODUTO_PROPOSTA`.

## 1. Cabeçalho — aba "Proposta"

### Grupo "Proposta nº" (Delphi `grp_1`)
| Campo | Coluna | Regra |
|---|---|---|
| Nº da proposta | `NR_PROPOSTA` | "INCLUSÃO" até gravar; depois `000123/26-R0` **devolvido pelo banco** (numeração anual com o ano do servidor — trigger) |
| Data solicitação* | `DT_SOLICITACAO` | padrão hoje |
| Data validade | `DT_VALIDADE` | sugerida = solicitação + `DIAS_VALIDADE_PROPOSTA`; ≥ solicitação |
| Status* | `ID_LAB_PROPOSTAS_STATUS` | padrão = status inicial; ao mudar para o status "Enviado ao cliente" grava `DT_ENVIO_CLIENTE` (trigger) |
| Data autorização | `DT_AUTORIZACAO` | **desabilitada** se já houver pedido de venda (Delphi `DBedt_dtAutoriz.Enabled`) |
| Data emissão (envio) | `DT_ENVIO_CLIENTE` | — |
| Nº pedido de venda | `VW_LAB_PROPOSTAS.NR_PEDIDO_FMT` (9 dígitos) | só leitura; botão **"Gerar Pedido"** habilitado se não houver pedido e a proposta estiver em edição (abre a T21) |

### Grupo "Dados Comerciais" (`grp_IDCliente`)
| Campo | Coluna | Regra |
|---|---|---|
| Comercial | `ID_ENTIDADES_COMERC` | entidades com `TIPO_VENDEDOR` ou profissionais (colunas Nome, Registro) |
| Cliente* | `ID_ENTIDADES` | colunas CPF/CNPJ + Nome; ao trocar recarrega contatos e endereços |
| Contato | `ID_ENT_CONTATO` | e-mails/contatos do cliente (colunas Contato, Departamento); mostra e-mail, departamento, CNPJ e fone principal (só leitura) |

### Grupo "Dados de Endereços" (`grp_DADOSEND`) — **particularidade**
Dois painéis independentes:

| Painel | Campos | Coluna | Obrigatório |
|---|---|---|---|
| **Endereço para emissão do(s) resultado(s)** | Endereço (colunas Tipo, Logradouro, Cidade) + nº, CEP, bairro, cidade, estado (só leitura) | `ID_ENT_END_LAUDO` | ✅ "Selecionar o endereço para Laudo" |
| | Contato do resultado | `ID_ENT_CONTATO_RESULTADO` | ✅ "Selecionar o Contato para Laudo" |
| **Endereço para emissão da Nota Fiscal** | **Entidade de cobrança** (pode ser outra empresa que paga) | `ID_ENT_COBRANCA` | ✅ "Selecionar a Empresa para Cobrança" |
| | Endereço da cobrança (endereços **da entidade de cobrança**) | `ID_ENT_END_COBRANCA` | ✅ |
| | Contato da NF (e-mails **da entidade de cobrança**) | `ID_ENT_CONTATO_NF` | ✅ "Selecionar o Contato para Cobrança" |

Sugestões ao escolher o cliente: entidade de cobrança = o próprio cliente; endereço do laudo = endereço com finalidade CORRESPONDENCIA; endereço da cobrança = finalidade COBRANCA ou NOTA_FISCAL; contato da NF = e-mail com `ENVIAR_NFE`; contato do resultado = e-mail com `ENVIAR_LAUDO`.
> Bug do legado a não repetir: a view juntava `ID_END_ENT_COBRANCA` com e-mails, mas a tela gravava um endereço nesse campo.

### Grupo "Informações Comerciais e Valores" (`grp_totais`)
| Campo | Coluna | Regra |
|---|---|---|
| Valor das análises | `VR_SOMA_VR_ANALIS` | Σ subtotal (só leitura, banco) |
| Desconto das análises | `VR_SOMA_DESC_ANALIS` | Σ desconto das linhas |
| Total das análises | `VR_SOMA_VR_TOT_ANALIS` | Σ total das linhas |
| % desconto geral | `PORC_DESCONTO` | se > 0, recalcula o desconto geral |
| Desconto geral | `VR_DESCONTO` | editável quando % = 0 |
| **Valor total** | `VR_TOTAL_PROPOSTA` | = total das análises − desconto geral |
| Condição de pagamento* | `ID_CONDICOES_PAGTOS` | só ativas (colunas Código, Descrição) |
| Moeda | `ID_MOEDAS` | — |

## 2. Aba "Análises" — produtos × parâmetros (**particularidade**)

### Painel Produtos
Grid: código, descrição, unidade, qtde de amostras. Botões incluir/alterar/gravar/cancelar/excluir produto e "+" (cadastro rápido de produto).
- `ITEM` automático (1, 2, 3…). `QTDE_AMOSTRAS` > 0.
- **Alterar a quantidade de amostras do produto altera a quantidade de todas as análises dele** (trigger `TG_PROPPROD_AU`).
- Excluir produto exclui suas análises (cascade); bloqueado se já houver amostra recebida para o item.

### Painel Parâmetros para as análises (do produto selecionado)
Linha (`LAB_PROPOSTAS_PRODANALISES`):
| Campo | Regra |
|---|---|
| Parâmetro* | colunas código + descrição; ao escolher traz o tipo de análise, a descrição reduzida e o **valor unitário** (se a linha estiver com 0); **filtra o combo de método** pelo método padrão do parâmetro |
| Método* | + cadastro rápido |
| Idioma* | + cadastro rápido |
| Prazo* | + cadastro rápido |
| Tipo de documento* | radio **Boletim / Relatório** |
| Qtde de amostras | = qtde do produto |
| Vr. unitário, Subtotal, % desconto, Vr. desconto, **Vr. total** | subtotal = qtde × unitário; % > 0 calcula o desconto; total = subtotal − desconto (trigger; a tela recalcula na hora para exibir) |

**Seleção múltipla de parâmetros** (Delphi `Btn_SelParam` → `Btn_ConcluirSel` → `Inc_VariosParamProp`):
1. "Selecionar parâmetros" abre a lista de todos os parâmetros ativos (checkbox, parâmetro, tipo de análise, valor).
2. "Concluir seleção" leva os marcados para uma lista de "a incluir".
3. Incluir **o primeiro** parâmetro manualmente (com método, idioma, prazo e tipo de documento).
4. "Incluir os demais" cria as outras linhas **copiando prazo, idioma, método e tipo de documento da linha base**, cada uma com o seu valor unitário.
   - Na web: modal com a lista + campos padrão (método, idioma, prazo, tipo de documento) → grava todas as linhas de uma vez.

Grid das análises: parâmetro, tipo de análise, método, qtde, unitário, subtotal, %, desconto, total, prazo, idioma, tipo de documento. **Linhas que vieram da revisão anterior** (`ST_REV_ANTERIOR = 1`) com uma cor e **as novas destacadas** (Delphi `DBGrd_Grid2DBTableView1CustomDrawCell`).

## 3. Aba "Informações Gerais"
Três editores de texto rico: **Itens solicitados**, **Itens adquiridos**, **Observação** (`LAB_PROPOSTAS_TEXTOS`). O legado guardava RTF; o novo guarda **HTML** (migração converte RTF → HTML ou texto puro).

## 4. Aba "Contrato"
Editor rico do contrato (`TX_CONTRATO`), botão "Importar arquivo" (.html/.txt/.docx → HTML) e "Usar contrato padrão" (`EMPRESAS_PARAMETROS.TEXTO_PADRAO_CONTRATO`).

## 5. Aba "Consultar"
Filtros: cliente, nº da proposta (completa com zeros até 6 dígitos ao teclar Enter), nº do pedido (9 dígitos), status, período, "só última revisão" (padrão marcado).
Grid: nº proposta, status (cor), CPF/CNPJ, cliente, nº pedido, valor total. Navegação primeiro/anterior/próximo/último.

## 6. Ações

| Ação | Regra |
|---|---|
| **Incluir** | status inicial, digitador = profissional do usuário, data hoje; foco no Comercial |
| **Gravar** | valida obrigatórios (status, cobrança, contato da NF, endereço do laudo, contato do resultado, condição de pagamento) e ≥ 1 produto com ≥ 1 análise; mostra "Nr. da Proposta: …" |
| **Alterar** | se a proposta já foi **enviada ao cliente**, tem **pedido** ou tem **amostra**, a alteração direta fica bloqueada e o sistema pede **Gerar revisão**. Senão pergunta como no Delphi: [Sim] gerar revisão / [Não] alterar sem revisão / [Cancelar] |
| **Gerar revisão** | escolher a revisão de origem (lista código/revisão/ano/situação/validade/valor — Delphi `pnlRevisao`), informar o **motivo** → `SP_GERAR_REVISAO_PROPOSTA` → abre a nova revisão em edição. Revisões antigas ficam somente leitura (o banco bloqueia) |
| **Excluir** | só a revisão 0 sem revisões posteriores, sem pedido e sem amostras ("Existe revisão para a Proposta nº… não será permitido") |
| **Imprimir proposta** | PDF: cabeçalho da empresa, cliente/contato, produtos e análises (método, prazo, idioma, qtde, tipo de doc., valores conforme flag), totais, itens solicitados/adquiridos, contrato, condição de pagamento |
| **Imprimir pedido de venda** | só com pedido (link para a T21) |
| **Trocar produto** | utilitário: produto antigo → novo na proposta e nas suas amostras (`SP_TROCAR_PRODUTO_PROPOSTA`) |

## 7. Erros do legado a não repetir
- `COD_PROPOSTA = ID` provisório (12 propostas ficaram assim) → número só pelo banco.
- Revisão copiava o pedido de venda e o status; `ST_REV_ANTERIOR` calculado errado (doc 12 §2).
- Revisões antigas editáveis.
- A view principal fazia INNER JOIN na cobrança (propostas sumiam da lista).

## Aceite
- [ ] Totais conferem com o roteiro do doc 12 §7 (300,00 − 10% = 270,00).
- [ ] Seleção múltipla cria N linhas com método/idioma/prazo/tipo da linha base.
- [ ] Revisão nova com linhas antigas marcadas; revisão antiga somente leitura.
- [ ] Proposta com amostra recebida não pode ser alterada sem revisão.
- [ ] PDF da proposta.

**Status:** 🟨 v1 existe com modelo errado (sem tabela de produtos) — **refazer**
