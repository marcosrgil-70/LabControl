# T20 — Propostas ⚠️ (tela com particularidades)

**Fase:** 6 (o usuário pediu começar por cadastros → recebimento → resultados → boletim; para receber amostras é preciso **proposta**: a fase 4 cria uma versão mínima e a fase 6 completa a tela)
**Rota:** `/Propostas` · **Permissões:** `PROPOSTAS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR/REVISAR`
**Legado:** `un_Propostas.pas` (2.138 linhas) + `undm_Propostas.pas`; relatórios `Layout_ImpProposta.fr3` e `Layout_ImpPedVenda.fr3`.
**Tabelas:** `LAB_PROPOSTAS`, `LAB_PROPOSTAS_TEXTOS`, `LAB_PROPOSTAS_PRODUTOS`, `LAB_PROPOSTAS_PRODANALISES`; views `VW_LAB_PROPOSTAS`, `VW_LAB_PROPOSTAS_ANALISES`; rotinas `SP_GERAR_REVISAO_PROPOSTA`, `SP_RECALCULAR_PROPOSTA` (trigger), `SP_TROCAR_PRODUTO_PROPOSTA`.

## Referências cruzadas
- **Delphi:** `LabControl/un_Propostas.pas` (2.138 linhas) + `undm_Propostas.pas` (1.094) + `Layout_ImpProposta.fr3`
- **Objetos do banco:** [`LAB_PROPOSTAS`](../banco/objetos/tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_TEXTOS`](../banco/objetos/tabelas/LAB_PROPOSTAS_TEXTOS.md), [`LAB_PROPOSTAS_PRODUTOS`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODANALISES.md), [`VW_LAB_PROPOSTAS`](../banco/objetos/views/VW_LAB_PROPOSTAS.md), [`VW_LAB_PROPOSTAS_ANALISES`](../banco/objetos/views/VW_LAB_PROPOSTAS_ANALISES.md), [`SP_GERAR_REVISAO_PROPOSTA`](../banco/objetos/rotinas/SP_GERAR_REVISAO_PROPOSTA.md), [`SP_RECALCULAR_PROPOSTA`](../banco/objetos/rotinas/SP_RECALCULAR_PROPOSTA.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../banco/objetos/rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md), [`TG_PROPOSTAS_BI`](../banco/objetos/triggers/TG_PROPOSTAS_BI.md), [`TG_PROPOSTAS_BU`](../banco/objetos/triggers/TG_PROPOSTAS_BU.md), [`TG_PRODAN_BI`](../banco/objetos/triggers/TG_PRODAN_BI.md), [`TG_PROPPROD_AU`](../banco/objetos/triggers/TG_PROPPROD_AU.md)
- **Processos:** [P05 Ciclo da proposta](../processos/P05-ciclo-proposta.md) · [P06 Valores da proposta](../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../processos/P07-revisao-proposta.md) · [P08 Pedido de venda](../processos/P08-pedido-venda.md)
- **Regras de negócio:** [RN-04 Propostas e pedidos](../regras/RN-04-propostas-pedidos.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

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
- [x] Totais conferem com o roteiro do doc 12 §7 (300,00 − 10% = 270,00) — `PropostasTests.Inclusao_numera_pelo_banco_e_calcula_os_valores_do_roteiro` e navegador.
- [x] Proposta nova recebe o número do banco (`00000N/AA-R0`, ano do servidor) — mesmos testes.
- [x] Seleção múltipla cria N linhas com método/idioma/prazo/tipo da linha base — 6.1: modal com filtro, padrões da última análise do produto e o método padrão de cada parâmetro (navegador `Proposta_selecao_multipla_contrato_pdf_e_revisao`).
- [x] Revisão nova com linhas antigas marcadas; revisão antiga somente leitura — 6.1 (`PropostasTests.Revisao_copia_tudo_marca_linhas_e_trava_a_anterior` + navegador).
- [x] Proposta com amostra recebida não pode ser alterada sem revisão — `PropostasTests.Com_amostra_fica_bloqueada_e_nao_exclui_sem_amostra_exclui`.
- [x] PDF da proposta — 6.1, com a página "Autorização de Serviço" (`PropostasTests.Contrato_padrao_troca_de_produto_e_pdf` + navegador); layout fino com os modelos reais (dúvida 5).

> **Cadastro rápido "+" (pendência 3):** feito na 4.2.
> - Método, idioma e prazo com `lc-rapido` no modal da análise.
> - Produto pelo `POST /Produtos/Rapido`, no modal do produto.

## Implementação (tarefa 4.2 — parte essencial, 23/10/2026)
- **Arquivos:**
  - `Controllers/PropostasController.cs` (`Index`, `Criar`, `Editar`, `Excluir`, `Entidade` JSON);
  - `Services/PropostaService.cs`, `ViewModels/PropostaVM.cs`;
  - `Views/Propostas/{Index,Form}.cshtml`, `wwwroot/js/propostas.js`;
  - `Infra/HtmlSimples.cs` (limpeza do HTML dos textos).
- **Edição como no Delphi:**
  - cabeçalho, textos, produtos e análises gravam **juntos, numa transação**;
  - produtos e análises vão num JSON (`ItensJson`) editado na página (modais de produto e de análise);
  - o servidor compara com o que está gravado: exclui o que saiu, altera o que mudou e inclui o novo.
- **Quem calcula:**
  - número/ano pela `TG_PROPOSTAS_BI` e item pela `TG_PROPPROD_BI`;
  - subtotal/desconto/total da linha pela `TG_PRODAN_BI/BU`;
  - quantidade do produto → análises pela `TG_PROPPROD_AU`;
  - totais pelo `SP_RECALCULAR_PROPOSTA`, chamado também no fim da gravação, para valer o desconto geral sem mexer nas análises;
  - a tela recalcula só para exibir; depois de gravar, abre a proposta relida.
- **Sugestões ao escolher o cliente** (`/Propostas/Entidade/{id}`):
  - endereço do resultado = finalidade CORRESPONDENCIA;
  - endereço da cobrança = COBRANCA, senão NOTA_FISCAL;
  - contato do resultado = `ENVIAR_LAUDO`, senão o principal;
  - contato da NF = `ENVIAR_NFE`, senão o principal;
  - empresa de cobrança = o próprio cliente, salvo se outra já tiver sido escolhida.
- **Parâmetro escolhido:** mostra o tipo de análise, sugere o valor unitário (se a linha estiver com 0) e o método padrão.
- **Validações:**
  - mensagens do legado para os obrigatórios (P05);
  - **novas:** contato/endereço precisam ser do cliente (resultado) ou da empresa de cobrança (NF), o que o legado não conferia; endereço da cobrança obrigatório; desconto geral não maior que o total; usuário sem profissional não inclui (a proposta guarda quem digitou).
- **Alteração direta só na revisão vigente sem pedido, sem amostra e não enviada.** Nos demais casos a tela fica somente leitura com o motivo, e "Gerar revisão" chega na 6.1. Excluir: só a revisão 0, sem revisões, pedido ou amostras.
- **Textos (itens solicitados/adquiridos, observação):**
  - editor simples (negrito, itálico, sublinhado, lista);
  - o HTML passa pelo `HtmlSimples.Limpar` ao gravar e ao exibir: só tags de formatação, sem atributos, sem script.
- **Testes:**
  - `PropostasTests` (4): roteiro de valores, obrigatórios, alteração com qtde propagada e desconto digitado, bloqueio/exclusão;
  - navegador `CrudNavegadorTelasTests.Proposta_crud_com_produto_analises_e_valores`.

## Implementação (tarefa 6.1 — proposta completa, 27/10/2026)
- **Decisões do usuário (27/10/2026):** importar contrato de **Word (.docx), HTML e TXT**; PDF **com a página "Autorização de Serviço (AS)"** como no legado.
- **Seleção múltipla** (`propostas.js` + modal `#modalSelecao`): lista de parâmetros ativos com filtro e "Marcar os visíveis"; os que já estão no produto vêm desabilitados; idioma, prazo e documento comuns (padrão = última análise do produto), método padrão de cada parâmetro (ou o escolhido, se o parâmetro não tiver); valor unitário do cadastro. Cria as linhas no JSON e grava com o "Gravar" de sempre (o servidor valida cada uma).
- **Revisão** (`PropostaService.GerarRevisaoAsync` → `SP_GERAR_REVISAO_PROPOSTA`): só da vigente, motivo obrigatório; abre a revisão nova. Botão no rodapé quando a proposta é editável e **no aviso de bloqueio** quando ela está enviada, com pedido ou com amostra. A pergunta Sim/Não/Cancelar do Delphi virou: "Gravar" altera sem revisão; "Gerar revisão" cria a nova a partir do que está gravado (o modal avisa que alterações não gravadas se perdem).
- **Aba Revisões:** todas as revisões (nº, status, validade, valor, motivo), a vigente marcada, link para abrir as outras.
- **Aba Contrato** (`TX_CONTRATO`): editor simples; "Usar contrato padrão" (`TEXTO_PADRAO_CONTRATO` da T05; pergunta antes de substituir); "Importar arquivo" (`POST /Propostas/ImportarContrato`, até 2 MB) por `Infra/ImportadorTexto.cs` — Word pelo pacote **DocumentFormat.OpenXml 3.5.1** (parágrafos, negrito, itálico, sublinhado, listas; tabelas e imagens ficam de fora, com aviso), HTML limpo pelo `HtmlSimples`, TXT em parágrafos.
- **PDF** (`Services/Propostas/PropostaPdf.cs`, QuestPDF): proposta técnica e comercial (empresa, nº, solicitação/validade, dados comerciais, dados do laudo e da nota fiscal, análises por produto — coluna de desconto só se houver —, totais, condição de pagamento, itens solicitados/adquiridos, observação, contrato com a formatação) + página **"Autorização de Serviço"** (proposta, cliente, "enviar correspondências aos cuidados de", "enviar amostras para", assinatura/carimbo, data, "ANEXAR NO RECIPIENTE DA AMOSTRA…"). Revisão substituída sai com marca d'água. Sem texto de autorização inventado e sem a linha do fornecedor do sistema antigo.
- **Trocar produto** (`SP_TROCAR_PRODUTO_PROPOSTA`): modal na revisão vigente (produto atual da proposta → novo ativo), troca na proposta e nas amostras recebidas por ela; o banco recusa amostra com boletim emitido.
- **Testes:** `PropostasTests` (+3: revisão, contrato/troca/PDF, HTML → PDF), `ImportadorTextoTests` (2) e navegador `Proposta_selecao_multipla_contrato_pdf_e_revisao`.

## Revisão (tarefa 6.3 — 28/10/2026)
- **Trocar produto** com amostra revisada nunca funcionava (a revisão antiga é somente leitura e o banco recusava a troca inteira): agora só a revisão vigente da amostra muda, e o item do pedido acompanha.
- **Contrato padrão** digitado como texto puro na T05 mantém parágrafos e quebras (`ImportadorTexto.DeTextoOuHtml`); o formulário só confere se existe (não lê o texto).
- **Importar contrato do Word:** listas numeradas viram `<ol>` (com subníveis) e o PDF numera "1.", "2.1."; texto dentro de controles de conteúdo (`SdtBlock`) entra; XML corrompido dá a mensagem amigável (não erro 500). **TXT/HTML em ANSI** (Windows-1252) mantém os acentos.

**Status:** ✅ concluída na tarefa 6.1 (27/10/2026) e revisada na 6.3 (28/10/2026); layout fino do PDF com os modelos reais (dúvida 5). Pedido de venda na 6.2 (T21).
