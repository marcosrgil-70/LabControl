# Objetos do banco v2 — um MD por objeto

> Banco `labcontrol_v2` (MySQL 8). Fonte da verdade: `Scripts/v2/01..05`. Modelo e decisões: [../11-MODELO-MYSQL.md](../11-MODELO-MYSQL.md).
> Cada arquivo traz: finalidade, origem no legado, colunas, chaves, relacionamentos, triggers, quem usa, telas, processos, regras de negócio, cuidados no C# e o código SQL.
> Legenda: 🔴 crítico · 🟠 importante · ⚪ normal.

**Totais:** 59 tabelas · 10 views · 8 funções · 27 procedures · 48 triggers.

## Tabelas

### Núcleo global e segurança
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`PAISES`](tabelas/PAISES.md) | ⚪ | Países (dado global, igual para todas as empresas). | `PAISES` (1) |
| [`ESTADOS`](tabelas/ESTADOS.md) | ⚪ | Unidades federativas (globais, códigos IBGE). | `ESTADOS` (28 (seed v2: 27 UFs)) |
| [`CIDADES`](tabelas/CIDADES.md) | ⚪ | Municípios (globais, código IBGE de 7 dígitos). Base do componente de endereço com busca por CEP. | `CIDADES` (+ `CIDADES_BAIRROS`, `LOGRADOUROS` descontinuadas) (190 (legado, sem código IBGE); 5.571 do IBGE no seed `06_cidades.sql`) |
| [`EMPRESAS`](tabelas/EMPRESAS.md) | 🟠 | Empresas usuárias do sistema (o laboratório). Dados do cabeçalho do laudo, logo e nº de acreditação. | `EMPRESAS` + entidade marcada como `TIPO_EMPRESA_USUARIA` (`ENTIDADES`/`ENTIDADES_PJ`/endereço) (1) |
| [`EMPRESAS_PARAMETROS`](tabelas/EMPRESAS_PARAMETROS.md) | 🔴 | Parâmetros de funcionamento de cada empresa: status padrão, limites de dias, textos padrão. | `EMPRESAS_PARAMETROS` (os `ULT_COD_*` foram para `SEQUENCIAS_DOCUMENTOS`) (1) |
| [`SEQUENCIAS_DOCUMENTOS`](tabelas/SEQUENCIAS_DOCUMENTOS.md) | 🔴 | Último número usado por (empresa, tipo de documento, ano). É a base da numeração segura de amostras, propostas, pedidos, produtos e relatórios de ensaio. | `EMPRESAS_PARAMETROS.ULT_COD_AMOSTRA/ULT_COD_PROPOSTA/ULT_PED_VENDA/ULT_COD_BOLETIM/ULT_COD_RE` + `SYS_TABELAS_ID` (— (criada na migração, M-18)) |
| [`USUARIOS`](tabelas/USUARIOS.md) | 🟠 | Usuários do sistema (login global). A senha é PBKDF2. | `USUARIO` (`USUCOD`, `USUNOM`, `USUSEN` com hash proprietário, `USUADM`) (15) |
| [`USUARIOS_EMPRESAS`](tabelas/USUARIOS_EMPRESAS.md) | 🔴 | Empresas que o usuário acessa e o **profissional** que ele representa em cada empresa (digitador/gerador dos registros). | `USUARIO.ID_ENTIDADES_FUNC` + direitos de empresa em `SYS_MODULOS_ACESSOS` (—) |
| [`PERMISSOES`](tabelas/PERMISSOES.md) | 🟠 | Catálogo global de permissões `TELA.ACAO` (carregado pelo `05_seed.sql`). | `SYS_MODULOS/SYS_MENUS/SYS_FORMS/SYS_COMPONENTES` (catálogo de telas e componentes) (109 (seed)) |
| [`PERFIS`](tabelas/PERFIS.md) | ⚪ | Perfis de acesso por empresa (ex.: ADMINISTRADOR, Recepção, Analista). | — (o legado dava direito usuário × empresa × componente) (—) |
| [`PERFIS_PERMISSOES`](tabelas/PERFIS_PERMISSOES.md) | ⚪ | Permissões de cada perfil (matriz da tela de perfis). | `SYS_*_ACESSOS` (3.690 linhas por componente) (—) |
| [`USUARIOS_PERFIS`](tabelas/USUARIOS_PERFIS.md) | ⚪ | Perfis do usuário em cada empresa (as permissões efetivas são a união dos perfis). | — (—) |
| [`AUDITORIA`](tabelas/AUDITORIA.md) | 🟠 | Trilha de auditoria gravada pela aplicação (antes/depois em JSON). | — (só havia o log de amostras) (—) |

### Cadastros auxiliares
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`TIPOS_LOGRADOURO`](tabelas/TIPOS_LOGRADOURO.md) | ⚪ | Tipos de logradouro (Rua, Avenida…) com abreviatura. | `LOGRADOUROS_TIPOS` (10) |
| [`ENDERECOS_TIPOS`](tabelas/ENDERECOS_TIPOS.md) | ⚪ | Tipos de endereço com **finalidade** (Geral, NF, Correspondência, Entrega, Cobrança). | `ENDERECOS_TIPOS` (IDs fixos no código: 2 = Correspondência) (4) |
| [`FONES_TIPOS`](tabelas/FONES_TIPOS.md) | ⚪ | Tipos de telefone. | `FONES_TIPOS` (5) |
| [`EMAILS_TIPOS`](tabelas/EMAILS_TIPOS.md) | ⚪ | Tipos de e-mail. | `EMAILS_TIPOS` (1) |
| [`TIPOS_REG_PROFISSIONAL`](tabelas/TIPOS_REG_PROFISSIONAL.md) | ⚪ | Conselhos de registro profissional (CRF, CRQ…), impressos no boletim. | `TIPOS_REG_PROFISSIONAL` (2) |
| [`CARGOS_FUNCIONARIOS`](tabelas/CARGOS_FUNCIONARIOS.md) | ⚪ | Cargos dos profissionais (impresso abaixo da assinatura no boletim). | `CARGO_FUNCIONARIOS` (9) |
| [`IDIOMAS`](tabelas/IDIOMAS.md) | ⚪ | Idioma do laudo de cada análise da proposta. | `IDIOMAS` (4) |
| [`UNIDADES`](tabelas/UNIDADES.md) | ⚪ | Unidades de medida (sigla impressa no boletim, ex.: UFC/g). | `UNIDADES` (27) |
| [`EMBALAGENS_TIPOS`](tabelas/EMBALAGENS_TIPOS.md) | ⚪ | Tipos de embalagem da amostra recebida. | `EMBALAGENS_TIPOS` (19) |
| [`LAB_TIPOS_RESULTADOS`](tabelas/LAB_TIPOS_RESULTADOS.md) | ⚪ | Resultados descritivos ("Ausência em 25 g", "Satisfatório"…). | `LAB_TIPOS_RESULTADOS` (61) |
| [`CONDICOES_PAGTOS`](tabelas/CONDICOES_PAGTOS.md) | ⚪ | Condições de pagamento da proposta/pedido. | `CONDICOES_PAGTOS` (`INATIVO` invertido para `ATIVO`) (112) |
| [`CONDICOES_PAGTOS_PARC`](tabelas/CONDICOES_PAGTOS_PARC.md) | ⚪ | Parcelas de uma condição de pagamento (opcional; nunca usado no legado). | `CONDICOES_PAGTOS_PARC` (vazia) (0) |
| [`PRAZOS`](tabelas/PRAZOS.md) | ⚪ | Prazos de entrega das análises (com quantidade de dias). | `PRAZOS` (`QTDE` → `QTDE_DIAS`) (17) |
| [`MOEDAS`](tabelas/MOEDAS.md) | ⚪ | Moedas da proposta. | `MOEDAS` (2) |
| [`AMOSTRAS_TIPO`](tabelas/AMOSTRAS_TIPO.md) | 🔴 | Tipos de amostra. O **CODIGO** compõe o número da amostra (TT de `TT-CCCCC-AA/YY`). | `AMOSTRAS_TIPO` (o ID era usado no número) (13) |
| [`AMOSTRAS_STATUS`](tabelas/AMOSTRAS_STATUS.md) | ⚪ | Status da amostra (Liberada, Bloqueada, Reprovada…), com cor e flag "permite resultado". | `AMOSTRAS_STATUS` (cor `TColor`) (4) |
| [`ANALISES_TIPO`](tabelas/ANALISES_TIPO.md) | 🔴 | Tipos de análise. O **CODIGO** compõe o número da amostra (AA de `TT-CCCCC-AA/YY`). | `ANALISES_TIPO` (13) |
| [`ANALISES_STATUS`](tabelas/ANALISES_STATUS.md) | ⚪ | Status de cada teste da amostra (Aguardando, Em análise, Concluída). | `ANALISES_STATUS` (3) |
| [`ANALISES_METODOS`](tabelas/ANALISES_METODOS.md) | ⚪ | Métodos de análise (vão para "Metodologias" do boletim). | `ANALISES_METODOS` (176) |
| [`BOLETINS_STATUS`](tabelas/BOLETINS_STATUS.md) | ⚪ | Status do boletim (Em elaboração, Em revisão, Enviado ao cliente). | `BOLETINS_STATUS` (3) |
| [`LAB_PROPOSTAS_STATUS`](tabelas/LAB_PROPOSTAS_STATUS.md) | ⚪ | Status da proposta, com cor e flag "permite receber amostra". | `LAB_PROPOSTAS_STATUS` (3) |

### Cadastros
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`ENTIDADES`](tabelas/ENTIDADES.md) | 🟠 | Pessoa física ou jurídica: clientes, fornecedores, profissionais (funcionários), comerciais e transportadores. | `ENTIDADES` + `ENTIDADES_PF` + `ENTIDADES_PJ` + `ENTIDADES_OBSERVACOES` (527 (44 PF / 483 PJ)) |
| [`ENTIDADES_ENDERECOS`](tabelas/ENTIDADES_ENDERECOS.md) | 🟠 | Endereços da entidade (logradouro/bairro em texto + cidade IBGE). | `ENTIDADES_ENDERECOS` ⨝ `LOGRADOUROS` ⨝ `CIDADES_BAIRROS` ⨝ `CIDADES` (538) |
| [`ENTIDADES_FONES`](tabelas/ENTIDADES_FONES.md) | ⚪ | Telefones da entidade (cliente, fornecedor, profissional, empresa usuária), com tipo. | `ENTIDADES_FONES` (695) |
| [`ENTIDADES_EMAILS`](tabelas/ENTIDADES_EMAILS.md) | 🟠 | E-mails da entidade — são também os **contatos** usados na proposta (contato comercial, do resultado e da NF). | `ENTIDADES_EMAILS` (664) |
| [`ENTIDADES_FUNCIONARIOS`](tabelas/ENTIDADES_FUNCIONARIOS.md) | 🟠 | Dados de profissional (funcionário): registro, cargo, flags de assinatura/análise e imagem da assinatura. | `ENTIDADES_FUNCIONARIOS` + `ENTIDADES_FUNC_ASSINATURAS` (vazia) (16) |
| [`PRODUTOS`](tabelas/PRODUTOS.md) | ⚪ | Produtos = nomes das amostras do cliente (ex.: "QUEIJO MINAS"). Usados na proposta, amostra, etiqueta e boletim. | `PRODUTOS` (só a aba Principal era usada; código por `SYS_TABELAS_ID`) (1.915) |
| [`LAB_PARAMETROS_ANALISES`](tabelas/LAB_PARAMETROS_ANALISES.md) | 🟠 | Catálogo de parâmetros de análise (o que o laboratório mede), com preço, método padrão e acreditação. | `LAB_PARAMETROS_ANALISES` (462 (52 acreditados)) |

### Propostas e pedidos
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`LAB_PROPOSTAS`](tabelas/LAB_PROPOSTAS.md) | 🔴 | Cabeçalho da proposta comercial. Cada revisão é uma linha nova (`REV_PROPOSTA`). | `LAB_PROPOSTAS` (2.358) |
| [`LAB_PROPOSTAS_TEXTOS`](tabelas/LAB_PROPOSTAS_TEXTOS.md) | ⚪ | Textos longos da proposta (itens solicitados/adquiridos, observação, contrato, autorização) — 1:1 com a proposta. | `LAB_PROPOSTAS_CONTRATOS` (2.219), `_OBSERVACAO` (284), `_ITENS_SOL` (10), `_ITENS_ADQ` (12), `_AUTORIZACAO` (0) — RTF (≈2.300) |
| [`LAB_PROPOSTAS_PRODUTOS`](tabelas/LAB_PROPOSTAS_PRODUTOS.md) | 🔴 | Produtos (itens) da proposta, com quantidade de amostras. | `LAB_PROPOSTAS_PRODUTOS` (PK composta proposta+empresa+item) (5.761) |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | 🔴 | Parâmetros/análises de cada produto da proposta, com método, idioma, prazo, tipo de documento e valores. | `LAB_PROPOSTAS_PRODANALISES` (19.252) |
| [`PEDIDOS_VENDA`](tabelas/PEDIDOS_VENDA.md) | 🔴 | Pedido de venda gerado a partir da proposta aprovada (1 por proposta, considerando todas as revisões). | Não era tabela: número em `LAB_PROPOSTAS.ID_PEDIDOS_VENDAS` (gerado por `SP_GET_ULT_PED_VENDA`) e copiado para a amostra (≈1.435 (1.454 propostas com pedido − repetições)) |
| [`PEDIDOS_VENDA_ITENS`](tabelas/PEDIDOS_VENDA_ITENS.md) | 🟠 | Itens contratados do pedido (produto × quantidade × valor). | — (—) |

### Amostras e estoque
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | 🔴 | **Amostra recebida** (o nome "histórico" é do legado). Guarda o número `TT-CCCCC-AA/YY [Rnn]`, os dados do recebimento e as revisões. | `LAB_HIST_AMOSTRAS` + `LAB_HIST_AMOSTRAS_OBSERVACAO` (5.770) |
| [`LAB_HIST_AMOSTRAS_TESTES`](tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | 🔴 | Testes (parâmetros) a analisar na amostra, copiados das análises do item da proposta. | `LAB_HIST_AMOSTRAS_TESTES` (20.551) |
| [`LAB_HIST_AMOSTRAS_LOG`](tabelas/LAB_HIST_AMOSTRAS_LOG.md) | ⚪ | Log de inclusão/alteração/exclusão da amostra (JSON), gravado pelas triggers. | `LAB_HIST_AMOSTRAS_LOG` (cópia das 45 colunas) (10.509) |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | 🟠 | Movimentação física (entradas/saídas) da amostra — sempre na amostra **raiz**. | `LAB_MOV_AMOSTRAS` (`AMOSTRA_COMPLEMENTAR`: A = automática, C = entrada complementar, M = saída manual) (5.770 (só entradas automáticas)) |
| [`LAB_MOV_AMOSTRAS_PARAM`](tabelas/LAB_MOV_AMOSTRAS_PARAM.md) | ⚪ | Parâmetros (testes) atendidos por uma saída de amostra. | `LAB_MOV_AMOSTRAS_PARAM` (0) |
| [`LAB_HIST_AMOSTRAS_SALDO`](tabelas/LAB_HIST_AMOSTRAS_SALDO.md) | 🟠 | Saldo físico atual da amostra raiz (mantido por trigger). | `LAB_HIST_AMOSTRAS_SALDO` (5.770) |
| [`LAB_LOCAL_AMOSTRAS`](tabelas/LAB_LOCAL_AMOSTRAS.md) | ⚪ | Arquivamento (armário/prateleira/caixa) e descarte da amostra física (1 por amostra raiz). | `LAB_LOCAL_AMOSTRAS` (`STATUS` 0/1, `DT_HR_DESCATE`) (2.686) |

### Resultados, boletins e relatório de ensaio
| Tabela | Crit. | Finalidade | Legado (registros) |
|---|---|---|---|
| [`LAB_RESULTADOS_ANALISES`](tabelas/LAB_RESULTADOS_ANALISES.md) | 🔴 | Cabeçalho do resultado de uma amostra (uma linha por revisão do resultado). | `LAB_RESULTADOS_ANALISES` (5.345) |
| [`LAB_RESULTADOS_PARAM`](tabelas/LAB_RESULTADOS_PARAM.md) | 🔴 | Resultado de cada teste: numérico (símbolo, valor × 10^exp, unidade) ou descritivo, mais a especificação. | `LAB_RESULTADOS_PARAM` (19.528) |
| [`LAB_BOLETIM_ANALISES`](tabelas/LAB_BOLETIM_ANALISES.md) | 🔴 | Cabeçalho do boletim (laudo): resultado vinculado, responsáveis, textos, emissão e PDF. | `LAB_BOLETIM_ANALISES` + `LAB_BOLETIM_OBS` (5.312) |
| [`LAB_BOLETIM_PARAMETROS`](tabelas/LAB_BOLETIM_PARAMETROS.md) | 🔴 | Parâmetros **escolhidos** para o boletim, com o texto do resultado congelado e a ordem de impressão. | `LAB_BOLETIM_PARAMETROS` (19.615) |
| [`LAB_RELATORIO_ENSAIO`](tabelas/LAB_RELATORIO_ENSAIO.md) | ⚪ | Relatório de ensaio livre (alternativa ao boletim quando o item da proposta tem `TIPO_DOCUMENTO = RELATORIO`). | `LAB_RELATORIO_ENSAIO` (vazia — tela existe, nunca usada) (0) |

## Views
| View | Crit. | Finalidade |
|---|---|---|
| [`VW_CLIENTES`](views/VW_CLIENTES.md) | ⚪ | Lista de clientes (entidades com `TIPO_CLIENTE = 1`) com telefone e e-mail principais. |
| [`VW_ENTIDADES_ENDERECOS`](views/VW_ENTIDADES_ENDERECOS.md) | ⚪ | Endereço completo e formatado em duas linhas (tipo, finalidade, cidade, UF, país). |
| [`VW_PROFISSIONAIS`](views/VW_PROFISSIONAIS.md) | 🟠 | Profissionais com registro completo, cargo, flags e usuário vinculado. |
| [`VW_LAB_PROPOSTAS`](views/VW_LAB_PROPOSTAS.md) | 🟠 | Consulta de propostas com cliente, contatos, cobrança, status (cor), totais e pedido de venda. |
| [`VW_LAB_PROPOSTAS_ANALISES`](views/VW_LAB_PROPOSTAS_ANALISES.md) | ⚪ | Itens de análise da proposta com produto, parâmetro, tipo de análise, método, idioma, prazo e valores. |
| [`VW_LAB_AMOSTRAS`](views/VW_LAB_AMOSTRAS.md) | 🔴 | Consulta completa de amostras (número, tipos, cliente, proposta, pedido, produto, datas parciais, status, endereço do laudo, acreditação, saldo, arquivo, flags de resultado/boletim). |
| [`VW_LAB_TESTES_PENDENTES`](views/VW_LAB_TESTES_PENDENTES.md) | 🟠 | **Fila de análises** (nova): testes da última revisão da amostra, com status que permite resultado, ainda sem resultado na última revisão do resultado. |
| [`VW_LAB_RESULTADOS`](views/VW_LAB_RESULTADOS.md) | 🟠 | Resultados por parâmetro com textos prontos (formatado, especificação, unidade, quantidade analisada, analista, dias de análise). |
| [`VW_LAB_BOLETINS`](views/VW_LAB_BOLETINS.md) | 🟠 | Consulta de boletins (código, revisão, datas de geração/emissão, flag emitido, status, amostra, cliente, responsáveis). |
| [`VW_PEDIDOS_VENDA_SALDO`](views/VW_PEDIDOS_VENDA_SALDO.md) | 🟠 | Pedido de venda: quantidade contratada × recebida por item (casamento por proposta + ITEM em todas as revisões; conta só amostras revisão 0). |

## Funções e procedures
| Rotina | Tipo | Crit. | Finalidade |
|---|---|---|---|
| [`FN_FORMATA_DECIMAL`](rotinas/FN_FORMATA_DECIMAL.md) | Função | ⚪ | Decimal → texto com vírgula, sem zeros à direita; 0/NULL → vazio. |
| [`FN_FORMATA_RESULTADO`](rotinas/FN_FORMATA_RESULTADO.md) | Função | 🟠 | Monta o texto do resultado/especificação para o boletim: numérico `"< 1,0e2 UFC/g"` ou descritivo (descrição do tipo de resultado). |
| [`FN_NR_AMOSTRA`](rotinas/FN_NR_AMOSTRA.md) | Função | 🔴 | Formata o número da amostra `TT-CCCCC-AA/YY[ Rnn]` a partir dos códigos. |
| [`FN_NR_AMOSTRA_ID`](rotinas/FN_NR_AMOSTRA_ID.md) | Função | 🔴 | Igual a `FN_NR_AMOSTRA`, mas recebe os IDs dos tipos e resolve os `CODIGO`s. |
| [`FN_NR_PROPOSTA`](rotinas/FN_NR_PROPOSTA.md) | Função | 🟠 | Formata o número da proposta `000123/26-R0`. |
| [`FN_DATA_PARCIAL`](rotinas/FN_DATA_PARCIAL.md) | Função | 🟠 | Data parcial para exibição: `dd/mm/aaaa`, `mm/aaaa` ou `aaaa` (fabricação/validade). |
| [`FN_ACREDITADO`](rotinas/FN_ACREDITADO.md) | Função | 🟠 | 0 = nenhum parâmetro acreditado, 1 = todos, 2 = misto, NULL = sem testes. |
| [`FN_OBS_METODOS`](rotinas/FN_OBS_METODOS.md) | Função | 🟠 | Texto das metodologias do boletim ("DESCR_METODO: método"), separado por acreditado (1) e não acreditado (0). |
| [`SP_PROXIMO_NUMERO`](rotinas/SP_PROXIMO_NUMERO.md) | Pública | 🔴 | Numeração segura por (empresa, tipo, ano): `INSERT … ON DUPLICATE KEY UPDATE` com trava exclusiva até o COMMIT (sem deadlock em gravações simultâneas). |
| [`PR_AUDITAR`](rotinas/PR_AUDITAR.md) | Interna | 🔴 | Grava um evento em `AUDITORIA` (usuário `@LC_ID_USUARIOS`, IP `@LC_IP`) dentro da transação da procedure que a chama: revisão R, emissão M, desfazer revisão D, pedido I/A, troca de produto A. |
| [`PR_CONFERIR_ENDERECO`](rotinas/PR_CONFERIR_ENDERECO.md) | Interna | 🟠 | Recusa (45000) um segundo endereço **ativo** do mesmo tipo na entidade; endereço inativo (histórico) é aceito. |
| [`PR_CONFERIR_FONE`](rotinas/PR_CONFERIR_FONE.md) | Interna | ⚪ | Recusa (45000) um segundo telefone principal na entidade. |
| [`PR_CONFERIR_EMAIL`](rotinas/PR_CONFERIR_EMAIL.md) | Interna | ⚪ | Recusa (45000) um segundo e-mail principal na entidade. |
| [`PR_CONFERIR_TIPOS_ENTIDADE`](rotinas/PR_CONFERIR_TIPOS_ENTIDADE.md) | Interna | 🟠 | Recusa (45000) entidade "empresa usuária" marcada também com outro tipo (cliente, fornecedor, funcionário, comercial, transportador). |
| [`PR_CONFERIR_EMPRESA`](rotinas/PR_CONFERIR_EMPRESA.md) | Interna | 🔴 | Recusa (45000 "Registro pertence a outra empresa.") um filho gravado com `ID_EMPRESAS` diferente da empresa do pai; 0/NULL = não informado. |
| [`PR_CRIAR_PERFIL`](rotinas/PR_CRIAR_PERFIL.md) | Interna | 🟠 | Cria um perfil sugerido da empresa com as permissões listadas (códigos ou TELA.*), só se ele ainda não existir — reinicializar não devolve permissão que a empresa retirou. |
| [`PR_ATUALIZA_SALDO`](rotinas/PR_ATUALIZA_SALDO.md) | Interna | 🟠 | Recalcula o saldo físico da amostra (Σ entradas − Σ saídas). |
| [`SP_RECALCULAR_PROPOSTA`](rotinas/SP_RECALCULAR_PROPOSTA.md) | Pública | 🔴 | Recalcula os totais da proposta a partir das análises e do desconto geral. |
| [`SP_AMOSTRA_GERAR_TESTES`](rotinas/SP_AMOSTRA_GERAR_TESTES.md) | Pública | 🔴 | Apaga e recria os testes da amostra a partir das análises do item da proposta (com data prevista). Recusa se houver resultado. |
| [`PR_REVISAO_PROPOSTA`](rotinas/PR_REVISAO_PROPOSTA.md) | Interna | 🔴 | Núcleo da revisão de proposta: copia cabeçalho, textos, produtos e análises para REV+1 e recalcula. |
| [`PR_REVISAO_BOLETIM`](rotinas/PR_REVISAO_BOLETIM.md) | Interna | 🔴 | Núcleo da revisão de boletim: nova revisão não emitida, apontando para o resultado novo/vigente, só com os parâmetros escolhidos e metodologias recalculadas. |
| [`PR_REVISAO_RESULTADO`](rotinas/PR_REVISAO_RESULTADO.md) | Interna | 🔴 | Núcleo da revisão de resultado (na mesma amostra ou na amostra revisada), com mapeamento por `ID_TESTE_ORIGEM` e revisão opcional do boletim. |
| [`PR_REVISAO_AMOSTRA`](rotinas/PR_REVISAO_AMOSTRA.md) | Interna | 🔴 | Núcleo da revisão de amostra: cópia completa (inclusive coletor), testes com `ID_TESTE_ORIGEM` e cascata resultado → boletim. |
| [`SP_GERAR_REVISAO_PROPOSTA`](rotinas/SP_GERAR_REVISAO_PROPOSTA.md) | Pública | 🔴 | Gera revisão da proposta (motivo obrigatório). |
| [`SP_GERAR_REVISAO_AMOSTRA`](rotinas/SP_GERAR_REVISAO_AMOSTRA.md) | Pública | 🔴 | Gera revisão da amostra (só com resultados) + revisão do resultado + do boletim. |
| [`SP_GERAR_REVISAO_RESULTADO`](rotinas/SP_GERAR_REVISAO_RESULTADO.md) | Pública | 🔴 | Gera revisão do resultado (e do boletim, se `p_gerar_boletim = 1`). |
| [`SP_GERAR_REVISAO_BOLETIM`](rotinas/SP_GERAR_REVISAO_BOLETIM.md) | Pública | 🔴 | Gera revisão do boletim apontando para o resultado vigente. |
| [`SP_EXCLUIR_REVISAO_RESULTADO`](rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md) | Pública | 🟠 | Desfaz a última revisão do resultado (sem boletim) e reativa a anterior. |
| [`SP_EXCLUIR_REVISAO_BOLETIM`](rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md) | Pública | 🟠 | Desfaz a última revisão do boletim (não emitida) e reativa a anterior; trava só o boletim (D-23) e recalcula a situação do pedido. |
| [`SP_EMITIR_BOLETIM`](rotinas/SP_EMITIR_BOLETIM.md) | Pública | 🔴 | Emite (congela) o boletim: grava data de emissão, emissor, status emitido, caminho e hash do PDF; o último boletim do pedido o conclui (`PR_ATUALIZAR_SITUACAO_PEDIDO`). |
| [`PR_ATUALIZAR_SITUACAO_PEDIDO`](rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md) | Interna | 🟠 | Situação automática do pedido (pendência 24): ABERTO → EM_EXECUCAO quando tem amostra; EM_EXECUCAO → CONCLUIDO quando toda amostra vigente tem o boletim vigente emitido e cada item recebeu o contratado (dispensado se `CONCLUSAO_MANUAL = 1` — concluído à mão, decisão do usuário 28/10/2026); CONCLUIDO → EM_EXECUCAO quando algo ficou pendente (revisão, amostra nova). CANCELADO não muda. Grava `AUDITORIA` com o motivo. |
| [`SP_GERAR_PEDIDO_VENDA`](rotinas/SP_GERAR_PEDIDO_VENDA.md) | Pública | 🔴 | Gera o pedido de venda da proposta (um por proposta), com itens, e liga as amostras já recebidas (com amostra, o pedido nasce em execução). |
| [`SP_ATUALIZAR_PEDIDO_REVISAO`](rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md) | Pública | 🟠 | Atualiza o pedido para a revisão vigente da proposta (valores, cobrança, condição, itens) e recalcula a situação (`PR_ATUALIZAR_SITUACAO_PEDIDO`: contratado menor pode concluir — 6.3). |
| [`SP_TROCAR_PRODUTO_PROPOSTA`](rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md) | Pública | 🟠 | Troca um produto por outro na proposta, na revisão vigente das amostras **dessa** proposta (as antigas são somente leitura) e nos itens do pedido (6.3). |
| [`SP_INICIALIZAR_EMPRESA`](rotinas/SP_INICIALIZAR_EMPRESA.md) | Pública | 🟠 | Cria os cadastros padrão de uma empresa (status, idiomas, moedas, tipos de endereço/logradouro/telefone/e-mail/registro, parâmetros, perfis) e a **entidade da empresa** (`EMPRESAS.ID_ENTIDADES`, onde ficam endereços/telefones/e-mails). Idempotente. |

## Triggers (por tabela)
| Tabela | Trigger | Momento | O que faz |
|---|---|---|---|
| [`EMPRESAS`](tabelas/EMPRESAS.md) | [`TG_EMPRESAS_AU`](triggers/TG_EMPRESAS_AU.md) | AFTER UPDATE | Espelha a identidade da empresa (razão social, fantasia, CNPJ, IE, IM, ativa) na entidade da empresa (`ID_ENTIDADES`). |
| [`ENTIDADES`](tabelas/ENTIDADES.md) | [`TG_ENT_BI`](triggers/TG_ENT_BI.md) | BEFORE INSERT | Empresa usuária não coexiste com outro tipo de cadastro. |
| [`ENTIDADES`](tabelas/ENTIDADES.md) | [`TG_ENT_BU`](triggers/TG_ENT_BU.md) | BEFORE UPDATE | Empresa usuária não coexiste com outro tipo e não deixa de ser empresa usuária enquanto `EMPRESAS` apontar para ela. |
| [`ENTIDADES_ENDERECOS`](tabelas/ENTIDADES_ENDERECOS.md) | [`TG_ENTEND_BI`](triggers/TG_ENTEND_BI.md) | BEFORE INSERT | Endereço herda `ID_EMPRESAS` da entidade; um endereço ativo por tipo (`PR_CONFERIR_ENDERECO`). |
| [`ENTIDADES_ENDERECOS`](tabelas/ENTIDADES_ENDERECOS.md) | [`TG_ENTEND_BU`](triggers/TG_ENTEND_BU.md) | BEFORE UPDATE | Endereço não muda de entidade/empresa; um endereço ativo por tipo. |
| [`ENTIDADES_FONES`](tabelas/ENTIDADES_FONES.md) | [`TG_ENTFON_BI`](triggers/TG_ENTFON_BI.md) | BEFORE INSERT | Telefone herda `ID_EMPRESAS` da entidade; inativo não é principal; um principal por entidade (`PR_CONFERIR_FONE`). |
| [`ENTIDADES_FONES`](tabelas/ENTIDADES_FONES.md) | [`TG_ENTFON_BU`](triggers/TG_ENTFON_BU.md) | BEFORE UPDATE | Telefone não muda de entidade/empresa; inativo não é principal; um principal por entidade. |
| [`ENTIDADES_EMAILS`](tabelas/ENTIDADES_EMAILS.md) | [`TG_ENTEML_BI`](triggers/TG_ENTEML_BI.md) | BEFORE INSERT | E-mail/contato herda `ID_EMPRESAS` da entidade; minúsculas sem espaços; inativo não é principal; um principal por entidade (`PR_CONFERIR_EMAIL`). |
| [`ENTIDADES_EMAILS`](tabelas/ENTIDADES_EMAILS.md) | [`TG_ENTEML_BU`](triggers/TG_ENTEML_BU.md) | BEFORE UPDATE | E-mail não muda de entidade/empresa; minúsculas; inativo não é principal; um principal por entidade. |
| [`ENTIDADES_FUNCIONARIOS`](tabelas/ENTIDADES_FUNCIONARIOS.md) | [`TG_ENTFUN_BI`](triggers/TG_ENTFUN_BI.md) | BEFORE INSERT | Profissional herda `ID_EMPRESAS` da entidade. |
| [`PRODUTOS`](tabelas/PRODUTOS.md) | [`TG_PRODUTOS_BI`](triggers/TG_PRODUTOS_BI.md) | BEFORE INSERT | Código automático do produto (6 dígitos) quando vazio. |
| [`LAB_PROPOSTAS`](tabelas/LAB_PROPOSTAS.md) | [`TG_PROPOSTAS_BI`](triggers/TG_PROPOSTAS_BI.md) | BEFORE INSERT | Numeração da proposta (ano do servidor + sequência anual), data de solicitação padrão, validade ≥ solicitação e `NR_PROPOSTA`. |
| [`LAB_PROPOSTAS`](tabelas/LAB_PROPOSTAS.md) | [`TG_PROPOSTAS_BU`](triggers/TG_PROPOSTAS_BU.md) | BEFORE UPDATE | Número/revisão/empresa imutáveis; revisão antiga somente leitura; status "Enviado ao cliente" grava `DT_ENVIO_CLIENTE`; recalcula `NR_PROPOSTA`. |
| [`LAB_PROPOSTAS_PRODUTOS`](tabelas/LAB_PROPOSTAS_PRODUTOS.md) | [`TG_PROPPROD_BI`](triggers/TG_PROPPROD_BI.md) | BEFORE INSERT | Item herda a empresa da proposta; bloqueia revisão antiga; numera o `ITEM`. |
| [`LAB_PROPOSTAS_PRODUTOS`](tabelas/LAB_PROPOSTAS_PRODUTOS.md) | [`TG_PROPPROD_BU`](triggers/TG_PROPPROD_BU.md) | BEFORE UPDATE | Bloqueia alteração de item em revisão antiga. |
| [`LAB_PROPOSTAS_PRODUTOS`](tabelas/LAB_PROPOSTAS_PRODUTOS.md) | [`TG_PROPPROD_AU`](triggers/TG_PROPPROD_AU.md) | AFTER UPDATE | Quantidade do produto propaga para todas as análises do item. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_BI`](triggers/TG_PRODAN_BI.md) | BEFORE INSERT | Análise herda empresa/proposta do item; bloqueia revisão antiga; calcula subtotal, desconto e total da linha. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_BU`](triggers/TG_PRODAN_BU.md) | BEFORE UPDATE | Recalcula os valores da linha; bloqueia revisão antiga. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_BD`](triggers/TG_PRODAN_BD.md) | BEFORE DELETE | Bloqueia exclusão de análise em revisão antiga. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_AI`](triggers/TG_PRODAN_AI.md) | AFTER INSERT | Recalcula os totais da proposta após incluir análise. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_AU`](triggers/TG_PRODAN_AU.md) | AFTER UPDATE | Recalcula os totais da proposta após alterar análise. |
| [`LAB_PROPOSTAS_PRODANALISES`](tabelas/LAB_PROPOSTAS_PRODANALISES.md) | [`TG_PRODAN_AD`](triggers/TG_PRODAN_AD.md) | AFTER DELETE | Recalcula os totais da proposta após excluir análise. |
| [`PEDIDOS_VENDA`](tabelas/PEDIDOS_VENDA.md) | [`TG_PEDVEN_BI`](triggers/TG_PEDVEN_BI.md) | BEFORE INSERT | Número sequencial do pedido de venda (por empresa, não anual) quando não informado. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_BI`](triggers/TG_AMOSTRAS_BI.md) | BEFORE INSERT | **Regra central do recebimento**: consistência com a proposta (empresa, cliente, item, última revisão, status), proteção da data de entrega, número com ano do servidor, pedido de venda, datas parciais e `NR_AMOSTRA`. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_BU`](triggers/TG_AMOSTRAS_BU.md) | BEFORE UPDATE | Travas de alteração da amostra (revisão antiga, boletim emitido, resultados, movimentação), datas parciais e `NR_AMOSTRA`. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_BD`](triggers/TG_AMOSTRAS_BD.md) | BEFORE DELETE | Exclusão só da revisão 0, sem resultados e sem movimentação manual. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_AI`](triggers/TG_AMOSTRAS_AI.md) | AFTER INSERT | Entrada automática no estoque (só revisão 0) + log de inclusão. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_AU`](triggers/TG_AMOSTRAS_AU.md) | AFTER UPDATE | Atualiza a quantidade da entrada automática + log de alteração. |
| [`LAB_HIST_AMOSTRAS`](tabelas/LAB_HIST_AMOSTRAS.md) | [`TG_AMOSTRAS_AD`](triggers/TG_AMOSTRAS_AD.md) | AFTER DELETE | Log de exclusão da amostra. |
| [`LAB_HIST_AMOSTRAS_TESTES`](tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | [`TG_TESTES_BI`](triggers/TG_TESTES_BI.md) | BEFORE INSERT | Teste herda a empresa da amostra e calcula a data prevista (entrega + prazo). |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | [`TG_MOV_BI`](triggers/TG_MOV_BI.md) | BEFORE INSERT | Movimento vai sempre para a amostra raiz; saída sem saldo é recusada, com a linha do saldo travada até o fim da transação (duas saídas ao mesmo tempo não passam juntas — D-25). |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | [`TG_MOV_BD`](triggers/TG_MOV_BD.md) | BEFORE DELETE | A entrada automática do recebimento não pode ser excluída; a entrada complementar também não, se o saldo ficaria negativo sem ela (RN-06.15, linha do saldo travada — D-25). |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | [`TG_MOV_AI`](triggers/TG_MOV_AI.md) | AFTER INSERT | Recalcula o saldo após incluir movimento. |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | [`TG_MOV_AU`](triggers/TG_MOV_AU.md) | AFTER UPDATE | Recalcula o saldo após alterar movimento. |
| [`LAB_MOV_AMOSTRAS`](tabelas/LAB_MOV_AMOSTRAS.md) | [`TG_MOV_AD`](triggers/TG_MOV_AD.md) | AFTER DELETE | Recalcula o saldo após excluir movimento. |
| [`LAB_RESULTADOS_ANALISES`](tabelas/LAB_RESULTADOS_ANALISES.md) | [`TG_RESULT_BI`](triggers/TG_RESULT_BI.md) | BEFORE INSERT | Resultado herda a empresa da amostra. |
| [`LAB_RESULTADOS_ANALISES`](tabelas/LAB_RESULTADOS_ANALISES.md) | [`TG_RESULT_BU`](triggers/TG_RESULT_BU.md) | BEFORE UPDATE | Revisão antiga somente leitura; amostra/revisão imutáveis. |
| [`LAB_RESULTADOS_ANALISES`](tabelas/LAB_RESULTADOS_ANALISES.md) | [`TG_RESULT_BD`](triggers/TG_RESULT_BD.md) | BEFORE DELETE | Exclusão só da última revisão, sem boletim; revisão > 0 só por "Desfazer revisão". |
| [`LAB_RESULTADOS_PARAM`](tabelas/LAB_RESULTADOS_PARAM.md) | [`TG_RESPAR_BI`](triggers/TG_RESPAR_BI.md) | BEFORE INSERT | Teste da mesma amostra; revisão antiga bloqueada; numérico × descritivo exclusivos; zeros → NULL; textos formatados. |
| [`LAB_RESULTADOS_PARAM`](tabelas/LAB_RESULTADOS_PARAM.md) | [`TG_RESPAR_BU`](triggers/TG_RESPAR_BU.md) | BEFORE UPDATE | Bloqueia revisão antiga e resultado com boletim emitido; mantém exclusividade e textos formatados. |
| [`LAB_RESULTADOS_PARAM`](tabelas/LAB_RESULTADOS_PARAM.md) | [`TG_RESPAR_BD`](triggers/TG_RESPAR_BD.md) | BEFORE DELETE | Bloqueia exclusão em revisão antiga ou de parâmetro usado em boletim. |
| [`LAB_BOLETIM_ANALISES`](tabelas/LAB_BOLETIM_ANALISES.md) | [`TG_BOLETIM_BI`](triggers/TG_BOLETIM_BI.md) | BEFORE INSERT | Resultado da mesma amostra; `COD_BOLETIM` = número base da amostra; data padrão. |
| [`LAB_BOLETIM_ANALISES`](tabelas/LAB_BOLETIM_ANALISES.md) | [`TG_BOLETIM_BU`](triggers/TG_BOLETIM_BU.md) | BEFORE UPDATE | Boletim emitido congelado (só o status muda); revisão antiga somente leitura; código/revisão/amostra imutáveis. |
| [`LAB_BOLETIM_ANALISES`](tabelas/LAB_BOLETIM_ANALISES.md) | [`TG_BOLETIM_BD`](triggers/TG_BOLETIM_BD.md) | BEFORE DELETE | Emitido não se exclui; revisão > 0 só por "Desfazer revisão". |
| [`LAB_BOLETIM_PARAMETROS`](tabelas/LAB_BOLETIM_PARAMETROS.md) | [`TG_BOLPAR_BI`](triggers/TG_BOLPAR_BI.md) | BEFORE INSERT | Parâmetro do mesmo resultado; bloqueia boletim emitido. |
| [`LAB_BOLETIM_PARAMETROS`](tabelas/LAB_BOLETIM_PARAMETROS.md) | [`TG_BOLPAR_BU`](triggers/TG_BOLPAR_BU.md) | BEFORE UPDATE | Bloqueia alteração de parâmetro de boletim emitido. |
| [`LAB_BOLETIM_PARAMETROS`](tabelas/LAB_BOLETIM_PARAMETROS.md) | [`TG_BOLPAR_BD`](triggers/TG_BOLPAR_BD.md) | BEFORE DELETE | Bloqueia exclusão de parâmetro de boletim emitido. |
| [`LAB_RELATORIO_ENSAIO`](tabelas/LAB_RELATORIO_ENSAIO.md) | [`TG_RE_BI`](triggers/TG_RE_BI.md) | BEFORE INSERT | Número anual do relatório de ensaio. |

## Objetos do legado que não existem no v2
- **Tabelas vazias não migradas:** módulos de Compras (`CMP_*`), Fiscal (`FSC_*`), Contábil e acessórios de produto — ver [../10-ANALISE-FIREBIRD.md §2.4](../10-ANALISE-FIREBIRD.md).
- **Tabelas incorporadas:** `ENTIDADES_PF/PJ/OBSERVACOES` → `ENTIDADES`; 5 textos da proposta → `LAB_PROPOSTAS_TEXTOS`; `LAB_BOLETIM_OBS` → `LAB_BOLETIM_ANALISES`; `LAB_HIST_AMOSTRAS_OBSERVACAO` → `LAB_HIST_AMOSTRAS.TX_OBSERVACAO`; `LOGRADOUROS`/`CIDADES_BAIRROS` → texto em `ENTIDADES_ENDERECOS`; `SYS_TABELAS_ID` e `EMPRESAS_PARAMETROS.ULT_*` → `SEQUENCIAS_DOCUMENTOS`; `SYS_*`/`ACESSO`/`ACOES`/`FORMS` → `PERMISSOES`/`PERFIS`.
- **Procedures descartadas:** `SP_GEN_*` (auto-increment), `SP_ULT_REVISAO_*` (lógica nas procedures de desfazer), `VAI` (manutenção pontual).
- **Views descartadas:** `VW_DUPLICADO` (diagnóstico), `VW_LAB_AMOSTRAS_PROP`, `VW_FORNECEDORES`, `VW_TRANSPORTADOR`.
