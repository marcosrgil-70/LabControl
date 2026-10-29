# 11 — Modelo MySQL v2

> Scripts: `Scripts/v2/01_schema.sql` → `02_funcoes_procedures.sql` → `03_triggers.sql` → `04_views.sql` → `05_seed.sql` → `06_cidades.sql`
> Banco: **`labcontrol_v2`** (convive com o `labcontrol` antigo até a virada). MySQL 8.0+, `utf8mb4_0900_ai_ci`.
> **Validado em 16/10/2026** no MySQL 8.0.46 local (banco temporário, depois removido): 69 tabelas/views, 8 funções, 19 procedures (22 desde 19/10/2026: `PR_AUDITAR`, `PR_CONFERIR_EMPRESA` na revisão da Fase 0 e `PR_CRIAR_PERFIL` na T04), 42 triggers (48 desde 20/10/2026: empresa usuária como entidade e regras dos contatos — D-19/D-20; procedures 26; 27 desde 27/10/2026 com a `PR_ATUALIZAR_SITUACAO_PEDIDO`, pendência 24), 109 permissões, 5.571 cidades IBGE (D-21); fluxo completo proposta → pedido → amostra → resultado → boletim → emissão → revisões (amostra/resultado/proposta) e travas negativas testados.

## 1. Decisões de modelagem

| ID | Decisão | Motivo |
|---|---|---|
| D-01 | **Tudo por empresa**: toda tabela de cadastro e movimento tem `ID_EMPRESAS` (decisão do usuário) | Multiempresa total. Globais: `EMPRESAS`, `PAISES/ESTADOS/CIDADES` (IBGE), `USUARIOS`, `PERMISSOES` |
| D-02 | Nomes de tabelas/colunas do Firebird preservados | Rastreabilidade com o Delphi e com a migração |
| D-03 | Typos corrigidos: `TEMPERATURA_VERFIFICACAO`→`TEMPERATURA_VERIFICACAO`, `ACOMPANHA_PADRA_ANALITICO`→`…_PADRAO_…`, `DT_HR_DESCATE`→`DT_HR_DESCARTE`, `ID_LAB_RESULTADO_ANALISES`→`ID_LAB_RESULTADOS_ANALISES`, `ID_LAB_HIST_AMOSTRA_TESTES`→`ID_LAB_HIST_AMOSTRAS_TESTES`, `ID_STATUS_PROPOSTAS`→`ID_LAB_PROPOSTAS_STATUS`, `COLETOR`→`NOME_COLETOR`, `DATAHORACOLETA`→`DT_HR_COLETA` | Clareza |
| D-04 | Chaves `INT AUTO_INCREMENT`; números de negócio em `SEQUENCIAS_DOCUMENTOS` | Generators → auto-increment; numeração anual segura |
| D-05 | `ENTIDADES_PF` + `ENTIDADES_PJ` incorporadas em `ENTIDADES` (`CPF_CNPJ`, `RG_IE`, `NOME_FANTASIA` = sobrenome no PF) | Remove 2 joins e o `UNION` das views |
| D-06 | Tabelas 1:1 de texto unidas: `LAB_PROPOSTAS_TEXTOS`; `LAB_BOLETIM_OBS` → colunas do boletim; `LAB_HIST_AMOSTRAS_OBSERVACAO` → `TX_OBSERVACAO` | Simplicidade |
| D-07 | Endereço desnormalizado (`LOGRADOURO`, `BAIRRO` em texto + `ID_CIDADES` IBGE). `LOGRADOUROS`/`CIDADES_BAIRROS` não migram como tabela | Permite busca por CEP (ViaCEP) e acaba com o cadastro prévio de logradouro |
| D-08 | `LAB_PROPOSTAS_PRODUTOS` com chave própria + `ITEM` | O legado usava PK composta e a amostra guardava só o número do item |
| D-09 | **Pedido de Venda vira tabela** (`PEDIDOS_VENDA` + `_ITENS`), 1 por proposta (todas as revisões) | Decisão do usuário (tela própria) |
| D-10 | Revisões com `ID_*_ORIGEM`, `ULTIMA_REVISAO`, `MOTIVO_REVISAO`; revisões antigas **somente leitura** (trigger) | Rastreabilidade ISO 17025; corrige as procedures |
| D-11 | Boletim **emitido** (`DH_EMISSAO`) fica congelado; mudança só via revisão. Migração congela os boletins antigos | No legado a trava nunca funcionou |
| D-12 | Estoque físico sempre na **amostra raiz** (revisão 0): `ID_AMOSTRA_RAIZ` | O legado criava nova "entrada" a cada revisão |
| D-13 | Ano de amostra/proposta com 4 dígitos (`SMALLINT`), exibido com 2 | Fim da ambiguidade |
| D-14 | Valores `DECIMAL`; booleanos `TINYINT(1)`; cores `#RRGGBB` (legado: `TColor` BGR) | Precisão e web |
| D-15 | `@LC_MIGRACAO = 1` desliga numeração/validações nas triggers | Carga do legado com os números originais |
| D-16 | Senhas com PBKDF2 (`PasswordHasher` do ASP.NET Core Identity) em `USUARIOS.SENHA_HASH` | O SHA-256 sem salt da v1 é inseguro |
| D-17 | Permissões por **perfil**, por empresa (`PERFIS`, `USUARIOS_PERFIS`), com catálogo `PERMISSOES` (`TELA.ACAO`) | Substitui `SYS_*_ACESSOS` (3.690 linhas por componente) |
| D-18 | Usuário ↔ Profissional por empresa: `USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC` | O mesmo usuário pode ser profissional em várias empresas |
| D-19 | **Empresa usuária = entidade** (`EMPRESAS.ID_ENTIDADES` → `ENTIDADES.TIPO_EMPRESA_USUARIA`, criada por `SP_INICIALIZAR_EMPRESA`): endereços, telefones e e-mails da empresa nas mesmas `ENTIDADES_*` de clientes e profissionais; `EMPRESAS` mantém a identidade global e `TG_EMPRESAS_AU` a espelha na entidade (20/10/2026, decisão do usuário) | Como o legado (`un_Entidades` em modo `teEMP`): vários contatos tipados; a 1ª versão tinha um endereço/fone/e-mail em colunas de `EMPRESAS` |
| D-20 | Regras dos contatos no banco: um endereço **ativo** por tipo, um telefone e um e-mail principal, contato não muda de entidade, e-mail em minúsculas (`PR_CONFERIR_ENDERECO/FONE/EMAIL`, triggers `BI/BU`) | O Firebird usava `UNIQUE (entidade, tipo)` (não guardava o endereço antigo) e o "principal" ficava só na tela |
| D-21 | Municípios do IBGE no seed `06_cidades.sql` (5.571, código de 7 dígitos) | Base da busca de CEP (ViaCEP casa pelo código IBGE); o legado tinha 190 cidades sem código |
| D-22 | `UQ_ENT_DOC` ignora a entidade da empresa usuária (parte funcional `CASE WHEN TIPO_EMPRESA_USUARIA = 1 THEN NULL ELSE CPF_CNPJ END`): o mesmo CNPJ pode ser empresa usuária, cliente e fornecedor (21/10/2026, decisão do usuário; code-review 2.5) | A entidade da empresa espelha o CNPJ (D-19) e, com a UNIQUE simples, recusava um cliente com o CNPJ do próprio laboratório (sem registro visível) e dava 1062 ao trocar o CNPJ da empresa para o de um cliente |
| D-23 | Travas das rotinas só nas linhas do próprio documento: a revisão de resultado trava a **amostra raiz** (`FOR UPDATE` por ID) e lê o `MAX(REVISAO)` sem trava; amostra física filtrada por `(ID_AMOSTRA_RAIZ = r OR ID_LAB_HIST_AMOSTRAS = r)` (índices das FKs), nunca `COALESCE(...) = r`; testes dentro de procedure com `SELECT … INTO`, não `SET v = EXISTS(…)` nem `CALL p((SELECT …))` (28/10/2026, revisão 6.3) | `COALESCE(...) = r` com `FOR UPDATE` percorria e travava a tabela inteira de amostras, e `SET`/`CALL` com subconsulta leem com trava compartilhada: deadlock entre a emissão de um boletim e a revisão de resultado de outro pedido |
| D-27 | **Amostra com boletim emitido aceita a troca só do status** (`TG_AMOSTRAS_BU` compara as outras 56 colunas com `<=>`; `ALTERADO_EM`/`ALTERADO_POR` ficam de fora) — 29/10/2026, decisão do usuário, pendência 34 | O descarte por reprovação (T24) acontece depois da análise, quando o boletim já foi emitido; antes só uma revisão da amostra mudava o status. O boletim emitido não muda; a troca fica na auditoria e no log da amostra |
| D-26 | **Envio de e-mail configurado por empresa** em `EMPRESAS_PARAMETROS` (`SMTP_SERVIDOR/PORTA/SEGURANCA/USUARIO/SENHA`, `EMAIL_REMETENTE(_NOME)`, `EMAIL_COPIA_OCULTA`, modelos `EMAIL_ASSUNTO_BOLETIM`/`EMAIL_TEXTO_BOLETIM`), editado pelo usuário na T05 › E-mail. `SMTP_SENHA` guarda a senha **cifrada pela aplicação** (ASP.NET Data Protection; a auditoria mostra `***`). Cada envio de boletim é um evento `N` em `AUDITORIA` (29/10/2026, pedido do usuário, tarefa 8.3) | Dúvida 2: em vez de SMTP fixo no `appsettings`, o laboratório configura e troca sozinho, cada empresa com o seu remetente. A senha não fica em texto puro no banco nem no backup |
| D-25 | Saldo da amostra protegido no próprio banco: `TG_MOV_BI` lê o saldo da raiz com `FOR UPDATE` antes de aceitar a saída e `TG_MOV_BD` recusa excluir a entrada complementar que deixaria o saldo negativo ("Esta entrada não pode ser excluída: o saldo da amostra ficaria negativo."), também com a linha travada. A trava é pela chave (uma linha do `LAB_HIST_AMOSTRAS_SALDO`), como pede a D-23. Perfil sugerido RECEPÇÃO ganha `LOCALIZACAO.CONSULTAR/INCLUIR/ALTERAR` (28/10/2026, pendências 31 e 32, decisão do usuário) | Na 7.1, duas saídas simultâneas liam o mesmo saldo e passavam as duas (saldo negativo); a T23 já travava no serviço, mas gravações por fora da aplicação ficavam sem proteção. A RECEPÇÃO movimentava as amostras mas não podia arquivá-las |
| D-24 | `PEDIDOS_VENDA.CONCLUSAO_MANUAL`: o pedido concluído à mão (mesmo com item faltando) volta a concluir sozinho quando a revisão que o reabriu for emitida; "Reabrir" pelo usuário zera a marca. `PR_ATUALIZAR_SITUACAO_PEDIDO` também passa ABERTO → EM_EXECUCAO quando o pedido tem amostra (com auditoria) e é chamada no recebimento, no "Atualizar para revisão vigente" e para o pedido antigo da amostra que troca de proposta (`TG_AMOSTRAS_BU` religa ao pedido da proposta nova) (28/10/2026, revisão 6.3, decisão do usuário) | Antes a conclusão manual se perdia na primeira revisão, a reabertura pelo recebimento era um UPDATE do C# sem auditoria e a amostra que trocava de proposta ficava no pedido antigo (nº errado na etiqueta e no boletim) |

## 2. Tabelas v2 (69 objetos)

### Globais
`EMPRESAS`, `EMPRESAS_PARAMETROS`, `SEQUENCIAS_DOCUMENTOS`, `PAISES`, `ESTADOS`, `CIDADES`, `USUARIOS`, `USUARIOS_EMPRESAS`, `PERMISSOES`, `PERFIS`, `PERFIS_PERMISSOES`, `USUARIOS_PERFIS`, `AUDITORIA`.

### Auxiliares (por empresa)
`TIPOS_LOGRADOURO`, `ENDERECOS_TIPOS` (com `FINALIDADE`), `FONES_TIPOS`, `EMAILS_TIPOS`, `TIPOS_REG_PROFISSIONAL`, `CARGOS_FUNCIONARIOS`, `IDIOMAS`, `UNIDADES`, `EMBALAGENS_TIPOS`, `LAB_TIPOS_RESULTADOS`, `CONDICOES_PAGTOS` (+`_PARC`), `PRAZOS`, `MOEDAS`, `AMOSTRAS_TIPO` (**`CODIGO`**), `AMOSTRAS_STATUS` (`PERMITE_RESULTADO`), `ANALISES_TIPO` (**`CODIGO`**), `ANALISES_STATUS`, `ANALISES_METODOS`, `BOLETINS_STATUS`, `LAB_PROPOSTAS_STATUS` (`PERMITE_AMOSTRA`).

> `AMOSTRAS_TIPO.CODIGO` e `ANALISES_TIPO.CODIGO` substituem o ID no número da amostra (com tudo por empresa, o ID auto-increment não serve). São únicos por empresa, de 1 a 99, e **não podem mudar depois de usados** (regra da tela).

### Cadastros
`ENTIDADES`, `ENTIDADES_ENDERECOS`, `ENTIDADES_FONES`, `ENTIDADES_EMAILS` (contatos; `ENVIAR_LAUDO`), `ENTIDADES_FUNCIONARIOS` (registro, UF, cargo, `ASSINA_BOLETIM`, `EXECUTA_ANALISE`, assinatura), `PRODUTOS`, `LAB_PARAMETROS_ANALISES`.

### Laboratório
`LAB_PROPOSTAS`, `LAB_PROPOSTAS_TEXTOS`, `LAB_PROPOSTAS_PRODUTOS`, `LAB_PROPOSTAS_PRODANALISES`, `PEDIDOS_VENDA`, `PEDIDOS_VENDA_ITENS`, `LAB_HIST_AMOSTRAS`, `LAB_HIST_AMOSTRAS_TESTES`, `LAB_HIST_AMOSTRAS_LOG`, `LAB_HIST_AMOSTRAS_SALDO`, `LAB_MOV_AMOSTRAS`, `LAB_MOV_AMOSTRAS_PARAM`, `LAB_LOCAL_AMOSTRAS`, `LAB_RESULTADOS_ANALISES`, `LAB_RESULTADOS_PARAM`, `LAB_BOLETIM_ANALISES`, `LAB_BOLETIM_PARAMETROS`, `LAB_RELATORIO_ENSAIO`.

### Views
`VW_CLIENTES`, `VW_ENTIDADES_ENDERECOS`, `VW_PROFISSIONAIS`, `VW_LAB_PROPOSTAS`, `VW_LAB_PROPOSTAS_ANALISES`, `VW_LAB_AMOSTRAS`, `VW_LAB_TESTES_PENDENTES` (fila de análises — nova), `VW_LAB_RESULTADOS`, `VW_LAB_BOLETINS`, `VW_PEDIDOS_VENDA_SALDO` (contratado × recebido — nova).

## 3. Rotinas

| Rotina | Tipo | Uso pela aplicação |
|---|---|---|
| `SP_PROXIMO_NUMERO(emp, tipo, ano, OUT n)` | Proc (sem transação) | Interna/triggers. Tipos: `AMOSTRA`, `PROPOSTA` (anuais), `PEDIDO_VENDA`, `PRODUTO` (ano 0), `RELATORIO_ENSAIO` (anual) |
| `SP_AMOSTRA_GERAR_TESTES(id_amostra)` | Proc (sem transação) | Depois de gravar a amostra (dentro da transação da aplicação) |
| `SP_RECALCULAR_PROPOSTA(id)` | Proc (sem transação) | Chamada pelas triggers das análises |
| `SP_GERAR_REVISAO_PROPOSTA` / `_AMOSTRA` / `_RESULTADO` / `_BOLETIM` | Proc **com transação** | Botão "Gerar revisão" (motivo obrigatório) |
| `SP_EXCLUIR_REVISAO_RESULTADO` / `_BOLETIM` | Proc com transação | "Desfazer última revisão" |
| `SP_EMITIR_BOLETIM(id, func, arquivo, hash)` | Proc com transação | Botão "Emitir" (após gerar o PDF) |
| `SP_GERAR_PEDIDO_VENDA` / `SP_ATUALIZAR_PEDIDO_REVISAO` | Proc com transação | Tela de Pedido de Venda |
| `SP_TROCAR_PRODUTO_PROPOSTA` | Proc com transação | Utilitário da proposta |
| `SP_INICIALIZAR_EMPRESA(id)` | Proc | Ao criar empresa nova e na migração |
| `PR_AUDITAR(emp, tabela, id, ação, antes, depois)` | Proc interna | Chamada pelas `SP_*` com transação: o evento (R, M, D, I/A) fica na transação da procedure |
| `PR_CRIAR_PERFIL(emp, nome, descrição, permissões)` | Proc interna | Chamada pelo `SP_INICIALIZAR_EMPRESA`: perfis sugeridos (só se ainda não existirem) |
| `PR_CONFERIR_EMPRESA(informada, do_pai)` | Proc interna | Triggers dos filhos: recusa `ID_EMPRESAS` diferente da empresa do pai |
| `FN_NR_AMOSTRA`, `FN_NR_AMOSTRA_ID`, `FN_NR_PROPOSTA`, `FN_DATA_PARCIAL`, `FN_FORMATA_DECIMAL`, `FN_FORMATA_RESULTADO`, `FN_ACREDITADO`, `FN_OBS_METODOS` | Funções | Views, triggers, boletim |

**Regra de transação:** procedures `SP_*` com transação **não** podem ser chamadas dentro de uma transação já aberta pelo EF Core (o `START TRANSACTION` faria commit implícito). Na aplicação, chamar com `ExecuteSqlRaw`/`DbCommand` fora de `BeginTransaction`.

**Variáveis de sessão** (definir na conexão, por request): `SET @LC_ID_USUARIOS = ?` (log). `@LC_PERMITE_RETROATIVO = 1` somente no request em que o usuário confirmou e tem `AMOSTRAS.ENTREGA_RETROATIVA`. Como o pool de conexões reutiliza sessões, o interceptor de conexão do EF deve **sempre** fazer `SET @LC_PERMITE_RETROATIVO = 0, @LC_MIGRACAO = 0` ao abrir.

## 4. Mapeamento Firebird → MySQL (colunas que mudam)

### LAB_HIST_AMOSTRAS
| Firebird | MySQL v2 | Transformação |
|---|---|---|
| `ANO_AMOSTRA` (2 díg.) | `ANO_AMOSTRA` (4 díg.) | `2000 + ANO`; se 0 → `YEAR(DT_ENTREGA)` |
| `ID_AMOSTRAS_TIPO` / `ID_ANALISES_TIPO` | idem (novo ID) | por mapa `CODIGO = ID antigo` |
| — | `NR_AMOSTRA` | `FN_NR_AMOSTRA(...)` (texto igual ao impresso) |
| `ID_LAB_PROPOSTAS_PRODUTOS` (nº item) | FK para `LAB_PROPOSTAS_PRODUTOS.ID` | busca por (proposta, item) |
| `ID_LAB_PEDIDOS_VENDAS` (número) | `ID_PEDIDOS_VENDA` (FK) | busca o pedido pelo número |
| `DT_ENTREGA` + `HR_ENTREGA` (timestamps) | `DATE` + `TIME` | `CAST` |
| `ACOMPANHA_*`, `ENVIAR_OUTRO_LABORATORIO` 'S'/'N' | `TINYINT` | `'S'→1` |
| `TEMPERATURA_VERFIFICACAO` | `TEMPERATURA_VERIFICACAO` | — |
| `COLETOR`, `DATAHORACOLETA` | `NOME_COLETOR`, `DT_HR_COLETA` | revisões vazias: copiar da revisão 0 |
| `LAB_HIST_AMOSTRAS_OBSERVACAO.TX_OBSERVACAO` | `TX_OBSERVACAO` | RTF mantido (conversão RTF→HTML na fase 6) |
| — | `ID_AMOSTRA_RAIZ`, `ID_AMOSTRA_ORIGEM`, `ULTIMA_REVISAO` | calculados por (empresa, ano, código) ordenados pela revisão |

### LAB_PROPOSTAS
| Firebird | MySQL v2 |
|---|---|
| `ID_STATUS_PROPOSTAS` | `ID_LAB_PROPOSTAS_STATUS` |
| `ID_END_ENT_COBRANCA` (endereço gravado pela tela) | `ID_ENT_END_COBRANCA` (se nulo, usar `ID_ENT_END_NF`) |
| `ID_ENT_END_NF` | descartado (incorporado acima) |
| `ID_PEDIDOS_VENDAS` | → `PEDIDOS_VENDA` (1 por COD+ANO) |
| `ANO_PROPOSTA` (2 díg.) | 4 dígitos |
| 5 tabelas de texto | `LAB_PROPOSTAS_TEXTOS` |
| — | `NR_PROPOSTA`, `ID_LAB_PROPOSTAS_ORIGEM` (revisão anterior), `ULTIMA_REVISAO` |

### LAB_RESULTADOS_PARAM
| Firebird | MySQL v2 |
|---|---|
| `ID_LAB_HIST_AMOSTRA_TESTES` | `ID_LAB_HIST_AMOSTRAS_TESTES` |
| `VR_EXP_RESULTADO` / `VR_EXP_ESPECIF` (double) | `SMALLINT` |
| `DT_INICIAL`, `DT_RESULTADO` (timestamp) | `DATE` |
| — | `RESULTADO_FORMATADO`, `ESPECIF_FORMATADA` (trigger) |

### LAB_BOLETIM_ANALISES
| Firebird | MySQL v2 |
|---|---|
| `ID_LAB_RESULTADO_ANALISES` | `ID_LAB_RESULTADOS_ANALISES` |
| `COD_BOLETIM` = nº da amostra (às vezes com " R01") | `COD_BOLETIM` = número **base** (sem " Rnn"); `REVISAO` renumerada 0..n por código |
| `LAB_BOLETIM_OBS.*` | `TXT_OBSERVACAO`, `TXT_LEGENDA`, `TXT_METODOLOGIA_UTIL`, `TXT_METODOLOGIA_ACRED` |
| `DH_EMISSAO` (sempre nulo) | `DH_EMISSAO = DH_GERACAO` na migração (congela o histórico) |

### Entidades
| Firebird | MySQL v2 |
|---|---|
| `ENTIDADES_PF.CPF` / `ENTIDADES_PJ.CNPJ` | `ENTIDADES.CPF_CNPJ` |
| `PF.SOBRENOME` / `PJ.NOME_FANTASIA` | `NOME_FANTASIA` |
| `PF.RG` / `PJ.INSC_ESTADUAL` | `RG_IE` |
| `PF.ESTADO_CIVIL` 0..4 | `ENUM('S','C','D','P','V')` |
| `TIPO_*`, `INATIVO`, `PRINCIPAL`, `ENVIAR_NFE` ('S'/'N') | `TINYINT` |
| `ENTIDADES_ENDERECOS.ID_LOGRADOUROS` | `LOGRADOURO` + `BAIRRO` + `ID_CIDADES` + `ID_TIPOS_LOGRADOURO` (join na migração) |
| `ENTIDADES_FUNCIONARIOS.ID_CARGO_FUNCIONARIO` | `ID_CARGOS_FUNCIONARIOS` |
| `USUARIO.ID_ENTIDADES_FUNC` | `USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC` |

### Cores (TColor → hex)
`TColor` é BGR: `#` + `RR GG BB` = `(c & 0xFF)`, `((c>>8) & 0xFF)`, `((c>>16) & 0xFF)`. Ex.: 65280 → `#00FF00`; 16711680 → `#0000FF`; 255 → `#FF0000`. Valores negativos (cores de sistema, ex.: -16777214) → `NULL`.

## 5. Regras garantidas pelo banco (independem da aplicação)

1. Número de amostra/proposta/pedido/RE gerado no servidor, com bloqueio; ano = ano do servidor. Fora da migração o número/ano enviado é **ignorado** (a trigger sempre numera).
2. Data de entrega não pode ser futura nem mais antiga que `DIAS_RETROATIVOS_ENTREGA` sem liberação.
3. Validade/fabricação parciais coerentes (dia exige mês e ano; mês exige ano; dia válido para o mês).
4. A amostra só usa proposta da mesma empresa, do mesmo cliente, na última revisão e com status que permite amostra; o produto vem do item.
5. Revisões antigas (proposta, amostra, resultado, boletim) são somente leitura, e revisão nova (`REVISAO`/`REV_PROPOSTA` > 0) só é incluída pelas `SP_GERAR_REVISAO_*` (`@LC_REVISAO`).
6. Boletim emitido não pode ser alterado nem excluído, e o resultado de boletim emitido também não.
7. Saída de estoque não pode deixar saldo negativo; a entrada automática não pode ser excluída.
8. Filhos herdam `ID_EMPRESAS` do pai; um `ID_EMPRESAS` informado diferente do pai é recusado (`PR_CONFERIR_EMPRESA`).
10. Eventos das procedures com transação (revisão, emissão, desfazer, pedido, troca de produto) gravam `AUDITORIA` na própria transação (`PR_AUDITAR`).
9. Totais da proposta recalculados a cada alteração das análises.
