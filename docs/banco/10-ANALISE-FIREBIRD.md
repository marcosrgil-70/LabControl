# 10 — Análise do Banco Firebird (legado)

> Origem: `C:\Project\Laboratorio\DBMAIS.FDB` (Firebird 2.5, ISO8859_1, página 16 KB; último acesso em 09/03/2026).
> Os metadados foram extraídos com `isql -x` e as contagens/anomalias foram consultadas no próprio banco em 16/10/2026.
> Este documento é a **referência do que existe**. O modelo novo está em [11-MODELO-MYSQL.md](11-MODELO-MYSQL.md).

## 1. Visão geral

| Objeto | Quantidade | Observação |
|---|---|---|
| Tabelas | 120 | 61 vazias (módulos de Compras, Fiscal e Contábil nunca usados) |
| Stored procedures | 31 (+31 `SP_GEN_*`) | as `SP_GEN_*` só fazem `GEN_ID` |
| Triggers | 107 | a maioria é `BEFORE INSERT` com `GEN_ID` |
| Generators | 92 | — |
| Views | 11 | — |
| Exceptions | 4 | `ERR_BOLETIM_EMITIDO`, `ERR_BOLETIM_NAO_ENCONTRADO`, `ERR_DELETE_RESULTADO`, `ERR_MOV_AMOSTRA` |
| Empresas | 1 | `EMPRESAS.ID=1`, `COD_EMPRESAS='001'` |

O sistema Delphi é multiempresa **apenas nos movimentos** (`ID_EMPRESAS` em propostas, amostras, resultados e boletins). Os cadastros (entidades, produtos, parâmetros e auxiliares) são **globais**. No sistema novo **tudo é por empresa** (decisão D-01).

## 2. Tabelas em uso (com registros)

### 2.1 Laboratório (núcleo do sistema)

| Tabela | Registros | Função |
|---|---:|---|
| `LAB_PROPOSTAS` | 2.358 | Cabeçalho da proposta comercial (revisões são linhas novas) |
| `LAB_PROPOSTAS_PRODUTOS` | 5.761 | Produtos da proposta. PK composta (proposta, empresa, item) |
| `LAB_PROPOSTAS_PRODANALISES` | 19.252 | Parâmetros/análises por produto, com preço e desconto |
| `LAB_PROPOSTAS_CONTRATOS` | 2.219 | Texto RTF do contrato (1:1) |
| `LAB_PROPOSTAS_OBSERVACAO` | 284 | Observação RTF (1:1) |
| `LAB_PROPOSTAS_ITENS_SOL` / `_ITENS_ADQ` | 10 / 12 | Itens solicitados/adquiridos RTF (1:1) |
| `LAB_PROPOSTAS_AUTORIZACAO` | 0 | Texto de autorização (1:1) |
| `LAB_PROPOSTAS_STATUS` | 3 | Em elaboração / Aguardando informação / Enviado ao cliente |
| `LAB_HIST_AMOSTRAS` | 5.770 | **Amostra recebida** (o nome "histórico" é legado) |
| `LAB_HIST_AMOSTRAS_TESTES` | 20.551 | Parâmetros a analisar na amostra (copiados da proposta) |
| `LAB_HIST_AMOSTRAS_OBSERVACAO` | 6 | Observação RTF da amostra (1:1) |
| `LAB_HIST_AMOSTRAS_LOG` | 10.509 | Log de I/A/E da amostra (cópia das 45 colunas) |
| `LAB_HIST_AMOSTRAS_SALDO` | 5.770 | Saldo físico (mantido por trigger) |
| `LAB_MOV_AMOSTRAS` | 5.770 | Movimentação E/S (só as entradas automáticas foram usadas) |
| `LAB_MOV_AMOSTRAS_PARAM` | 0 | Parâmetros atendidos por uma saída |
| `LAB_LOCAL_AMOSTRAS` | 2.686 | Arquivamento (armário/prateleira/caixa) e descarte |
| `LAB_RESULTADOS_ANALISES` | 5.345 | Cabeçalho do resultado (com revisão) |
| `LAB_RESULTADOS_PARAM` | 19.528 | Resultado por teste (numérico ou descritivo + especificação) |
| `LAB_BOLETIM_ANALISES` | 5.312 | Cabeçalho do boletim (laudo), com revisão |
| `LAB_BOLETIM_PARAMETROS` | 19.615 | Parâmetros escolhidos para o boletim e o texto do resultado |
| `LAB_BOLETIM_OBS` | 5.311 | Observação, legenda e metodologias (1:1) |
| `LAB_PARAMETROS_ANALISES` | 462 | Catálogo de parâmetros (52 acreditados/auditados) |
| `LAB_TIPOS_RESULTADOS` | 61 | Resultados descritivos ("Ausência em 25 g", "Satisfatório"…) |
| `LAB_RELATORIO_ENSAIO` | 0 | Relatório de ensaio livre (tela existe, nunca usada) |

### 2.2 Cadastros

| Tabela | Registros | Observação |
|---|---:|---|
| `ENTIDADES` | 527 | Pessoa genérica com flags `TIPO_CLIENTE`, `TIPO_FUNCIONARIO` etc. ('S'/'N') |
| `ENTIDADES_PF` / `ENTIDADES_PJ` | 44 / 483 | Extensão 1:1 por categoria (`ENTIDADES.CATEGORIA` = F/J) |
| `ENTIDADES_FUNCIONARIOS` | 16 | Registro profissional + cargo (18 entidades marcadas como funcionário) |
| `ENTIDADES_FUNC_ASSINATURAS` | 0 | Assinatura digital (blob) |
| `ENTIDADES_EMAILS` | 664 | Também são os **contatos** usados na proposta |
| `ENTIDADES_FONES` | 695 | — |
| `ENTIDADES_ENDERECOS` | 538 | Normalizado via `LOGRADOUROS` → `CIDADES_BAIRROS` → `CIDADES` |
| `ENTIDADES_OBSERVACOES` | 1 | — |
| `PRODUTOS` | 1.915 | Código via `SYS_TABELAS_ID` (último = 3801) |
| `ANALISES_METODOS` | 176 | — |
| `CONDICOES_PAGTOS` | 112 | Só código + descrição (parcelas nunca usadas) |
| `UNIDADES` | 27 | — |
| `EMBALAGENS_TIPOS` | 19 | — |
| `PRAZOS` | 17 | Descrição + quantidade de dias |
| `AMOSTRAS_TIPO` | 13 | **O ID compõe o número da amostra** (TT) |
| `ANALISES_TIPO` | 13 | **O ID compõe o número da amostra** (AA) |
| `CARGO_FUNCIONARIOS` | 9 | — |
| `AMOSTRAS_STATUS` | 4 | Liberada / Bloqueada / Reprovada-devolvida / Reprovada-descartada |
| `ANALISES_STATUS`, `BOLETINS_STATUS` | 3 / 3 | Cor em `TColor` (inteiro BGR) |
| `IDIOMAS` | 4 | Português, Inglês, Espanhol, "---" |
| `MOEDAS` | 2 | Real, Dólar |
| `TIPOS_REG_PROFISSIONAL` | 2 | CRF, CRQ |
| `ENDERECOS_TIPOS` | 4 | 1 NF, **2 Correspondência**, 3 Entrega, 4 Cobrança |
| `FONES_TIPOS` / `EMAILS_TIPOS` | 5 / 1 | — |
| `PAISES`/`ESTADOS`/`CIDADES` | 1 / 28 / 190 | `CIDADES_BAIRROS` 485, `LOGRADOUROS` 552, `LOGRADOUROS_TIPOS` 10 |

### 2.3 Sistema / segurança

| Tabela | Registros | Observação |
|---|---:|---|
| `USUARIO` | 15 | `USUCOD`, `USUNOM` (login), `USUSEN` (hash proprietário, **não recuperável**), `USUADM`, `ID_ENTIDADES_FUNC` (vínculo com o profissional) |
| `SYS_MODULOS` / `SYS_MENUS` / `SYS_FORMS` / `SYS_COMPONENTES` | 2 / 55 / 43 / 246 | Catálogo de telas e componentes |
| `SYS_*_ACESSOS` | 30 / 825 / 645 / 3.690 | Direitos por usuário × empresa (habilitado/visível/incluir/alterar/excluir) |
| `EMPRESAS_PARAMETROS` | 1 | Últimos números (`ULT_COD_*`) + status padrão |
| `SYS_TABELAS_ID` | 1 | Sequência genérica (só `PRODUTOS`) |
| `ACESSO`, `ACOES`, `FORMS` | 0 | Modelo antigo de permissões (abandonado) |

### 2.4 Tabelas vazias que **não** serão migradas

`CMP_*` (compras: 9 tabelas), `FSC_*` (fiscal: 13), `CONT_CONTAS`, `PLANO_CONTAS`, `CONTA_CORRENTE*`, `BANCOS`, `PRODUTOS_APRESENTACAO/ESPECIFICACAO/FABRICANTE/FORNECEDORES/GRUPO/SECAO/SUBGRUPO`, `TIPO_APRESENTACAO/AVALIACAO/BAIXA/ESPECIFICACAO`, `UNIDADES_CONVERSAO`, `HISTORICO_BAIXA`, `DEPARTAMENTO`, `CRF_NATUREZAS_OPERACAO`, `PEDIDO_COMPRAS_STATUS`, `FABRICANTE`, `CONDICOES_PAGTOS_PARC`. Se um dia forem necessárias, entram como módulo novo.

## 3. Relacionamentos principais

```
EMPRESAS 1─N LAB_PROPOSTAS 1─N LAB_PROPOSTAS_PRODUTOS 1─N LAB_PROPOSTAS_PRODANALISES N─1 LAB_PARAMETROS_ANALISES
                 │ 1─1 textos (contrato, observação, itens sol/adq, autorização)
                 │
                 └─1─N LAB_HIST_AMOSTRAS 1─N LAB_HIST_AMOSTRAS_TESTES (cópia de PRODANALISES do item)
                          │ 1─N LAB_MOV_AMOSTRAS ── LAB_HIST_AMOSTRAS_SALDO
                          │ 1─1 LAB_LOCAL_AMOSTRAS
                          └─1─N LAB_RESULTADOS_ANALISES 1─N LAB_RESULTADOS_PARAM N─1 LAB_HIST_AMOSTRAS_TESTES
                                      └─1─N LAB_BOLETIM_ANALISES 1─N LAB_BOLETIM_PARAMETROS N─1 LAB_RESULTADOS_PARAM
                                                              1─1 LAB_BOLETIM_OBS
ENTIDADES ─ PF/PJ/FUNCIONARIOS/EMAILS/FONES/ENDERECOS ;  USUARIO.ID_ENTIDADES_FUNC → ENTIDADES_FUNCIONARIOS
```

Pontos importantes:

- **Pedido de Venda não é tabela.** É um número em `LAB_PROPOSTAS.ID_PEDIDOS_VENDAS` (1.454 propostas com pedido), gerado por `SP_GET_ULT_PED_VENDA` quando há data de autorização, e copiado para a amostra em `LAB_HIST_AMOSTRAS.ID_LAB_PEDIDOS_VENDAS`.
- A proposta guarda o **cliente**, o **contato** (e-mail), o **comercial**, o **endereço do laudo** + **contato do resultado** e a **entidade de cobrança**, que pode ser outra, com seu endereço de NF/cobrança e o contato da NF.
- `LAB_HIST_AMOSTRAS.ID_LAB_PROPOSTAS_PRODUTOS` é o **número do item**, não uma FK simples (a PK de produtos é composta).

## 4. Número da amostra (particularidade central)

Formato: **`TT-CCCCC-AA/YY [Rnn]`**

| Parte | Origem | Exemplo |
|---|---|---|
| `TT` | `ID_AMOSTRAS_TIPO` (2 dígitos) | `04` = Alimento |
| `CCCCC` | `COD_AMOSTRA`, sequencial **por empresa, reiniciado a cada ano** | `01382` |
| `AA` | `ID_ANALISES_TIPO` (2 dígitos) | `01` = Contagem/Pesquisa/FQ |
| `YY` | `ANO_AMOSTRA` (2 dígitos) | `18` |
| `Rnn` | `REVISAO` > 0 | ` R01` |

O sequencial `CCCCC` é **único por ano**, independentemente de TT e AA (TT/AA só "decoram" o número). `COD_BOLETIM` recebe o mesmo texto do número da amostra.

### Como o legado gera o número (SP_GET_COD_AMOSTRA + tela)

1. Ao sair do campo **Data de Entrega**, a tela calcula `ANO_AMOSTRA = FormatDateTime('YY', DT_ENTREGA)`, ou seja, o ano vem da data **digitada**.
2. Ao gravar (e também ao abrir a aba Observação!), chama `SP_GET_COD_AMOSTRA(empresa)`, que:
   - lê o ano do `DT_ENTREGA` da **última amostra pelo maior ID**;
   - se esse ano for **menor que o ano corrente**, zera `ULT_COD_AMOSTRA`;
   - incrementa e devolve.

### Erros dessa regra (confirmados no banco)

| # | Erro | Efeito / evidência |
|---|---|---|
| A1 | O ano da amostra vem da data digitada, não do servidor | 3 amostras com ano ≠ ano da entrega (ANO 0 com data de 2014; ANO 14 com datas de 2013; ANO 17 com data de 2016) |
| A2 | O reinício da sequência depende da data digitada na **última** amostra | Se alguém digita o ano anterior por engano em janeiro, a próxima amostra **reinicia em 1** e duplica números já usados no ano |
| A3 | Sequência sem chave de ano: um contador único que é "zerado" | Não há como saber a qual ano pertence o contador; uma correção manual quebra tudo |
| A4 | `GetNrAmostra` é chamado ao trocar para a aba Observação **e** ao gravar | Números "queimados" (lacunas) |
| A5 | Sem bloqueio de concorrência (UPDATE depois do SELECT) | Dois usuários simultâneos podem receber o mesmo número |
| A6 | Não há `UNIQUE` em (empresa, ano, código, revisão) | Nada impede duplicidade no banco |

➡️ **Correção (v2):** tabela `SEQUENCIAS_DOCUMENTOS (empresa, tipo, ano)` com `SELECT … FOR UPDATE`; o ano é **sempre** `YEAR(CURDATE())` do servidor, aplicado pela trigger; a data de entrega só pode ser futura com erro, e só pode ficar mais de N dias no passado (parâmetro, padrão 7) com confirmação **e** permissão; `UNIQUE (ID_EMPRESAS, ANO, COD, REVISAO)`. Detalhes em [12](12-PROCEDURES-REVISOES.md#1-numeração-de-documentos).

## 5. Stored procedures do legado

| Procedure | Função | Situação |
|---|---|---|
| `SP_GET_COD_AMOSTRA` | Próximo número de amostra | Com erros A1–A5 → substituída por `SP_PROXIMO_NUMERO` |
| `SP_GET_COD_PROPOSTAS` | Próximo número de proposta (reinicia no ano) | Mesmos erros; `ULT_COD_PROPOSTA + 1` sem `COALESCE`; a tela grava `COD_PROPOSTA = ID` provisório (12 propostas ficaram com o ID como número) |
| `SP_GET_COD_BOLETIM` | Próximo número de boletim | **Nunca usada** (`ULT_COD_BOLETIM` nulo; o código do boletim é o número da amostra) |
| `SP_GET_ULT_PED_VENDA` | Próximo número de pedido de venda (não anual) | Sem bloqueio |
| `SP_SYS_GERAR_CODIGO` | Sequência genérica (produtos) | OK, sem bloqueio |
| `SP_GERAR_REVISAO_PROPOSTAS` | Copia a proposta para REV+1 | **Com erros** — ver doc 12 |
| `SP_GERAR_REVISAO_AMOSTRA` | Copia a amostra para REV+1 + revisa resultado | **Com erros** — ver doc 12 |
| `SP_GERAR_REVISAO_RESULTADOS` | Copia o resultado para REV+1 + revisa boletim | **Com erros** — ver doc 12 |
| `SP_GERAR_REVISAO_BOLETIM` | Copia o boletim para REV+1 | **Com erros graves** — ver doc 12 |
| `SP_ULT_REVISAO_BOLETIM` / `_RESULTADO` | Maior revisão | Usada pela trigger de exclusão |
| `FN_ACREDITADO` | 0 = nenhum parâmetro acreditado, 1 = todos, 2 = misto | OK → `FN_ACREDITADO` |
| `SP_FORMATDOUBLE` | Double → texto sem zeros à direita | → `FN_FORMATA_DECIMAL` |
| `SP_OBSMETODOS` | Texto "Método: descrição" dos parâmetros (acreditados ou não) | Buscava o método na proposta pelo parâmetro (pode trazer o método de outro produto) → `FN_OBS_METODOS` usa o teste da amostra |
| `SP_TROCAPRODUTO` | Troca o produto de uma proposta e das amostras | **Bug grave:** o `FOR SELECT` percorre **todas** as amostras do banco → `SP_TROCAR_PRODUTO_PROPOSTA` |
| `VAI` | Correção pontual de `DESC_PARAMETRO` | Script de manutenção, descartar |

## 6. Triggers relevantes do legado

| Trigger | Regra | v2 |
|---|---|---|
| `LAB_HIST_AMOSTRAS_BI0/BU0` | Dia/mês/ano de validade/fabricação = 0 → NULL | Mantida (`TG_AMOSTRAS_BI/BU`) |
| `LAB_HIST_AMOSTRAS_AI0` | Cria a entrada automática em `LAB_MOV_AMOSTRAS` (`AMOSTRA_COMPLEMENTAR='A'`) | Mantida **só para a revisão 0** (no legado cada revisão gerava outra entrada → estoque duplicado) |
| `LAB_HIST_AMOSTRAS_AU0` | Bloqueia **qualquer** alteração se houver movimentação manual; atualiza a quantidade da entrada automática | Bloqueia só a mudança de **quantidade** |
| `LAB_HIST_AMOSTRAS_AD0/AD1` | Apaga testes/observação/movimento; bloqueia se houver movimentação manual | FKs `ON DELETE CASCADE` + `TG_AMOSTRAS_BD` |
| `LAB_HIST_AMOSTRAS_AI1/AU1/AD2` | Log com as 45 colunas | `LAB_HIST_AMOSTRAS_LOG` em JSON |
| `TG_LAB_MOV_AMOSTRAS_AIUD` | Recalcula o saldo | `TG_MOV_AI/AU/AD` + `PR_ATUALIZA_SALDO`; saída sem saldo é bloqueada |
| `LAB_BOLETIM_ANALISES_BU0/BD0` | Bloqueia alterar/excluir boletim emitido (`OLD.DH_EMISSAO > 0`, comparação timestamp × inteiro) | **Nunca funcionou:** 0 de 5.312 boletins têm `DH_EMISSAO` → `SP_EMITIR_BOLETIM` + `TG_BOLETIM_BU/BD` |
| `LAB_BOLETIM_ANALISES_AD0` | Apaga filhos (duplicado com BD0) | FK cascade |
| `LAB_RESULTADOS_ANALISES_BD0` | Só exclui a última revisão | `TG_RESULT_BD` + `SP_EXCLUIR_REVISAO_RESULTADO` |
| `LAB_RESULTADOS_PARAM_BIU0` | IDs 0 → NULL | Mantida |
| `LAB_RELATORIO_ENSAIO_BI` | Grava `ULT_COD_RE = NEW.ID_EMPRESAS` (**bug**: deveria ser o número) | `TG_RE_BI` |
| `SYS_*_AI0` | Cria direitos para todos os usuários × empresas ao cadastrar tela/menu | Substituído por perfis |
| `TG_SYS_USUARIO_BD0` | Apaga direitos do usuário | FK cascade |

## 7. Views do legado e problemas

| View | Problema | v2 |
|---|---|---|
| `VW_LAB_HIST_AMOSTRA` | `INNER JOIN` com uma subconsulta de testes → **6.287 linhas para 5.770 amostras** (duplica amostras com parâmetros acreditados e não acreditados); exige endereço tipo 2 (correspondência) **fixo no código** | `VW_LAB_AMOSTRAS` |
| `VW_LAB_PROPOSTAS` | `INNER JOIN` na entidade de cobrança (some proposta sem cobrança); junta `ID_END_ENT_COBRANCA` com **e-mails**, mas a tela grava um **endereço** nesse campo | `VW_LAB_PROPOSTAS` |
| `VW_FUNCIONARIO` | Exige `ENTIDADES_PF` e cargo (`INNER JOIN`); esconde inativos | `VW_PROFISSIONAIS` |
| `VW_CLIENTES`, `VW_FORNECEDORES`, `VW_TRANSPORTADOR` | `UNION` PF/PJ repetido | `VW_CLIENTES` (tabela única) |
| `VW_DUPLICADO` | Diagnóstico: amostras com mais de um resultado (124) | Desnecessária (`UNIQUE` + revisão) |
| `VW_LAB_AMOSTRAS_PROP`, `VW_DADOS_EMPRESAS`, `VW_ENDERECOS`, `VW_CLIENTES_ENDERECOS` | Montagem de texto | `VW_ENTIDADES_ENDERECOS`, `VW_LAB_AMOSTRAS` |

## 8. Anomalias de dados encontradas (tratar na migração)

| Anomalia | Qtde | Tratamento |
|---|---:|---|
| Amostra com `ANO_AMOSTRA` diferente do ano da entrega | 3 | Manter o número original (é o que foi impresso); registrar no relatório da migração |
| Amostra com `ANO_AMOSTRA = 0` | 1 | Migrar como ano 2014 (ano da entrega); avisar |
| Propostas com `COD_PROPOSTA = ID` (número provisório nunca substituído) | 12 | Migrar como estão e listar para conferência |
| Pedido de venda repetido em várias revisões da mesma proposta | 19 números | 1 pedido por proposta (`COD`+`ANO`), apontando para a maior revisão com pedido |
| Resultados com mais de um boletim (revisão de boletim reaproveitando o mesmo resultado) | 120 | Migrar todas as revisões; `COD_BOLETIM` sem " Rnn" e `REVISAO` renumerada por código |
| Revisões de amostra que perderam `COLETOR`/`DATAHORACOLETA` | 61 | Copiar da revisão 0 na migração |
| Amostras com o mesmo parâmetro duas vezes nos testes | 18 | Migrar (o modelo novo suporta); a revisão nova usa `ID_TESTE_ORIGEM` |
| Itens de proposta com o mesmo parâmetro duas vezes | 2 | Migrar e listar |
| Amostras sem resultado | 552 | Normal (canceladas/pendentes) |
| Resultados sem boletim | 156 | Normal |
| Boletins com `DH_EMISSAO` preenchido | **0** | Na migração, todo boletim com data menor que a da migração recebe `DH_EMISSAO = DH_GERACAO` (congelado); decisão D-11 |
| Usuários: senha em hash proprietário | 15 | Senha provisória + troca obrigatória no 1º acesso |

## 9. Login e multiempresa no legado

- O LabControl.exe **não tem tela de login própria**: é chamado por um lançador externo (`MAIS_Acesso`, fora deste repositório) com `ParamStr(1)=funcionário`, `ParamStr(2)=empresa`, `ParamStr(3)=usuário`.
- A empresa ativa vem do parâmetro; o menu "Utilitários → Ativar Empresa" troca a empresa.
- Direitos por usuário × empresa em `SYS_MODULOS/MENUS/FORMS/COMPONENTES_ACESSOS` (`unClassAcessos.pas` habilita/oculta itens de menu e botões).
- O **funcionário ativo** (`USUARIO.ID_ENTIDADES_FUNC`) é gravado como digitador (`ID_ENTIDADES_FUNC_DIG`) em amostras/resultados e como gerador do boletim.

No sistema novo: login próprio → escolha da empresa (entre as liberadas ao usuário) → o profissional vinculado ao usuário **naquela empresa** passa a ser o funcionário ativo. Ver [../telas/T00-login-empresa.md](../telas/T00-login-empresa.md).
