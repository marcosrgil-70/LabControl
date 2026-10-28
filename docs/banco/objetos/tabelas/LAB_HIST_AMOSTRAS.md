# LAB_HIST_AMOSTRAS

**Amostra recebida** (o nome "histórico" é do legado). Guarda o número `TT-CCCCC-AA/YY [Rnn]`, os dados do recebimento e as revisões.

**Grupo:** Amostras e estoque · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-14

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_HIST_AMOSTRAS` + `LAB_HIST_AMOSTRAS_OBSERVACAO` | 5.770 |

Comentário do script: _8. AMOSTRAS (Histórico de Amostras / Recebimento) Número: TT-CCCCC-AA/YY [Rnn]   TT = AMOSTRAS_TIPO.CODIGO | CCCCC = COD_AMOSTRA (sequencial anual)   AA = ANALISES_TIPO.CODIGO | YY = ANO_AMOSTRA mod 100 | Rnn = REVISAO_

Notas do script: _Fabricação/Validade podem ser PARCIAIS (só mês/ano, só ano) — particularidade do legado_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ANO_AMOSTRA` | SMALLINT | não |  | trigger TG_AMOSTRAS_BI | Ano do SERVIDOR na inclusão (trigger) |
| 4 | `COD_AMOSTRA` | INT | não |  | trigger TG_AMOSTRAS_BI | Sequencial por empresa/ano (trigger) |
| 5 | `REVISAO` | INT | não | 0 | aplicação |  |
| 6 | `ID_AMOSTRAS_TIPO` | INT | não |  | aplicação |  |
| 7 | `ID_ANALISES_TIPO` | INT | não |  | aplicação |  |
| 8 | `NR_AMOSTRA` | VARCHAR(25) | não |  | trigger TG_AMOSTRAS_BI, trigger TG_AMOSTRAS_BU | Número formatado (trigger) |
| 9 | `ID_AMOSTRA_ORIGEM` | INT | sim |  | aplicação (TG_AMOSTRAS_BI limpa → NULL) | Revisão de onde foi copiada |
| 10 | `ID_AMOSTRA_RAIZ` | INT | sim |  | aplicação (TG_AMOSTRAS_BI limpa → NULL) | Revisão 0 (amostra física). NULL na própria raiz |
| 11 | `ULTIMA_REVISAO` | TINYINT(1) | não | 1 | aplicação |  |
| 12 | `MOTIVO_REVISAO` | VARCHAR(500) | sim |  | aplicação |  |
| 13 | `TIPO_DOCUMENTO` | ENUM('BOLETIM','RELATORIO') | não | 'BOLETIM' | aplicação |  |
| 14 | `ID_ENTIDADES` | INT | não |  | aplicação | Cliente |
| 15 | `NOME_CONTATO` | VARCHAR(100) | sim |  | aplicação |  |
| 16 | `ID_LAB_PROPOSTAS` | INT | não |  | aplicação |  |
| 17 | `ID_LAB_PROPOSTAS_PRODUTOS` | INT | não |  | aplicação |  |
| 18 | `ID_PEDIDOS_VENDA` | INT | sim |  | trigger TG_AMOSTRAS_BI |  |
| 19 | `ID_PRODUTOS` | INT | não |  | trigger TG_AMOSTRAS_BI, trigger TG_AMOSTRAS_BU |  |
| 20 | `DT_ENTREGA` | DATE | não |  | aplicação |  |
| 21 | `HR_ENTREGA` | TIME | não |  | aplicação |  |
| 22 | `LOCAL_RECEBIMENTO` | VARCHAR(50) | não |  | aplicação |  |
| 23 | `ID_EMBALAGENS_TIPOS` | INT | não |  | aplicação |  |
| 24 | `QTDE_EMBALAGENS_ENTREGUE` | DECIMAL(15,4) | não | 0 | aplicação |  |
| 25 | `NR_LOTE` | VARCHAR(30) | sim |  | aplicação |  |
| 26 | `FABRICACAO_DIA` | TINYINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 27 | `FABRICACAO_MES` | TINYINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 28 | `FABRICACAO_ANO` | SMALLINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 29 | `VALIDADE_DIA` | TINYINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 30 | `VALIDADE_MES` | TINYINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 31 | `VALIDADE_ANO` | SMALLINT | sim |  | aplicação (TG_AMOSTRAS_BI, TG_AMOSTRAS_BU limpa → NULL) |  |
| 32 | `DT_FABRICACAO` | DATE | sim |  | trigger TG_AMOSTRAS_BI, trigger TG_AMOSTRAS_BU | Legado; preenchido só quando a data é completa |
| 33 | `DT_VALIDADE` | DATE | sim |  | trigger TG_AMOSTRAS_BI, trigger TG_AMOSTRAS_BU | Legado; preenchido só quando a data é completa |
| 34 | `NOTA_ROTULO` | VARCHAR(255) | sim |  | aplicação |  |
| 35 | `DT_ENTREGA_VERIFICACAO` | DATE | sim |  | aplicação |  |
| 36 | `HR_ENTREGA_VERIFICACAO` | TIME | sim |  | aplicação |  |
| 37 | `ESPECIE_AMOSTRA` | VARCHAR(50) | sim |  | aplicação |  |
| 38 | `ASPECTO_AMOSTRA` | VARCHAR(50) | sim |  | aplicação |  |
| 39 | `COR` | VARCHAR(20) | sim |  | aplicação |  |
| 40 | `OUTRAS_CARACTERISTICAS` | VARCHAR(100) | sim |  | aplicação |  |
| 41 | `QTDE_AMOSTRA_VERIFICACAO` | DECIMAL(15,4) | sim |  | aplicação |  |
| 42 | `TEMPERATURA_VERIFICACAO` | DECIMAL(7,2) | sim |  | aplicação |  |
| 43 | `OBSERVACAO_VERIFICACAO` | VARCHAR(255) | sim |  | aplicação |  |
| 44 | `ACOMPANHA_FICHA_TECNICA` | TINYINT(1) | não | 0 | aplicação |  |
| 45 | `ACOMPANHA_PADRAO_ANALITICO` | TINYINT(1) | não | 0 | aplicação |  |
| 46 | `ACOMPANHA_CA_CLIENTE` | TINYINT(1) | não | 0 | aplicação |  |
| 47 | `ENVIAR_OUTRO_LABORATORIO` | TINYINT(1) | não | 0 | aplicação |  |
| 48 | `QTDE_ENVIO_OUTRO_LABORATORIO` | DECIMAL(15,4) | não | 0 | aplicação |  |
| 49 | `ID_AMOSTRAS_STATUS` | INT | não |  | aplicação |  |
| 50 | `ID_ENTIDADES_FUNC_RESP` | INT | não |  | aplicação | Responsável pelo recebimento |
| 51 | `ID_ENTIDADES_FUNC_DIG` | INT | não |  | aplicação | Profissional do usuário logado |
| 52 | `NOME_COLETOR` | VARCHAR(60) | sim |  | aplicação |  |
| 53 | `DT_HR_COLETA` | DATETIME | sim |  | aplicação |  |
| 54 | `TX_OBSERVACAO` | MEDIUMTEXT | sim |  | aplicação | Antiga LAB_HIST_AMOSTRAS_OBSERVACAO (RTF/HTML) |
| 55 | `ID_LEGADO` | INT | sim |  | aplicação |  |
| 56 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 57 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 58 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 59 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_HIST_AMOSTRAS`
- **Única** `UQ_AMO_NUM` (ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, REVISAO)
- **Check** `CK_AMO_FABDIA`: `FABRICACAO_DIA IS NULL OR FABRICACAO_DIA BETWEEN 1 AND 31`
- **Check** `CK_AMO_FABMES`: `FABRICACAO_MES IS NULL OR FABRICACAO_MES BETWEEN 1 AND 12`
- **Check** `CK_AMO_VALDIA`: `VALIDADE_DIA IS NULL OR VALIDADE_DIA BETWEEN 1 AND 31`
- **Check** `CK_AMO_VALMES`: `VALIDADE_MES IS NULL OR VALIDADE_MES BETWEEN 1 AND 12`
- **Índice** `IX_AMO_NR` (ID_EMPRESAS, NR_AMOSTRA)
- **Índice** `IX_AMO_CLI` (ID_EMPRESAS, ID_ENTIDADES, DT_ENTREGA)
- **Índice** `IX_AMO_PROP` (ID_LAB_PROPOSTAS)
- **Índice** `IX_AMO_ENTREGA` (ID_EMPRESAS, DT_ENTREGA)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_AMOSTRAS_TIPO` | [`AMOSTRAS_TIPO`](../tabelas/AMOSTRAS_TIPO.md) | ID_AMOSTRAS_TIPO | bloqueia |
| `ID_ANALISES_TIPO` | [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md) | ID_ANALISES_TIPO | bloqueia |
| `ID_AMOSTRA_ORIGEM` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| `ID_AMOSTRA_RAIZ` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_LAB_PROPOSTAS` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | bloqueia |
| `ID_LAB_PROPOSTAS_PRODUTOS` | [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_LAB_PROPOSTAS_PRODUTOS | bloqueia |
| `ID_PEDIDOS_VENDA` | [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_PEDIDOS_VENDA | bloqueia |
| `ID_PRODUTOS` | [`PRODUTOS`](../tabelas/PRODUTOS.md) | ID_PRODUTOS | bloqueia |
| `ID_EMBALAGENS_TIPOS` | [`EMBALAGENS_TIPOS`](../tabelas/EMBALAGENS_TIPOS.md) | ID_EMBALAGENS_TIPOS | bloqueia |
| `ID_AMOSTRAS_STATUS` | [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md) | ID_AMOSTRAS_STATUS | bloqueia |
| `ID_ENTIDADES_FUNC_RESP` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_DIG` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_AMOSTRA_ORIGEM | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_AMOSTRA_RAIZ | bloqueia |
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| [`LAB_HIST_AMOSTRAS_SALDO`](../tabelas/LAB_HIST_AMOSTRAS_SALDO.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| [`LAB_LOCAL_AMOSTRAS`](../tabelas/LAB_LOCAL_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| [`LAB_RELATORIO_ENSAIO`](../tabelas/LAB_RELATORIO_ENSAIO.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |

## Triggers
- [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md) — BEFORE INSERT: **Regra central do recebimento**: consistência com a proposta (empresa, cliente, item, última revisão, status), proteção da data de entrega, número com ano do servidor, pedido de venda, datas parciais e `NR_AMOSTRA`.
- [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md) — BEFORE UPDATE: Travas de alteração da amostra (revisão antiga, boletim emitido, resultados, movimentação), datas parciais e `NR_AMOSTRA`.
- [`TG_AMOSTRAS_BD`](../triggers/TG_AMOSTRAS_BD.md) — BEFORE DELETE: Exclusão só da revisão 0, sem resultados e sem movimentação manual.
- [`TG_AMOSTRAS_AI`](../triggers/TG_AMOSTRAS_AI.md) — AFTER INSERT: Entrada automática no estoque (só revisão 0) + log de inclusão.
- [`TG_AMOSTRAS_AU`](../triggers/TG_AMOSTRAS_AU.md) — AFTER UPDATE: Atualiza a quantidade da entrada automática + log de alteração.
- [`TG_AMOSTRAS_AD`](../triggers/TG_AMOSTRAS_AD.md) — AFTER DELETE: Log de exclusão da amostra.

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_BOLETINS`](../views/VW_LAB_BOLETINS.md), [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** [`PR_REVISAO_AMOSTRA`](../rotinas/PR_REVISAO_AMOSTRA.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
- **Rotinas que leem:** [`PR_ATUALIZA_SALDO`](../rotinas/PR_ATUALIZA_SALDO.md), [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_AMOSTRA`](../rotinas/SP_GERAR_REVISAO_AMOSTRA.md), [`SP_GERAR_REVISAO_RESULTADO`](../rotinas/SP_GERAR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md), [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_TESTES_BI`](../triggers/TG_TESTES_BI.md), [`TG_MOV_BI`](../triggers/TG_MOV_BI.md), [`TG_RESULT_BI`](../triggers/TG_RESULT_BI.md), [`TG_BOLETIM_BI`](../triggers/TG_BOLETIM_BI.md), [`TG_RE_BI`](../triggers/TG_RE_BI.md)

## Telas e processos
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Regras de negócio
- **Ano = ano do servidor** e código = `SP_PROXIMO_NUMERO('AMOSTRA', ano)` na revisão 0; o que a aplicação enviar em `ANO_AMOSTRA`/`COD_AMOSTRA` é ignorado.
- Data de entrega futura é recusada; mais antiga que `DIAS_RETROATIVOS_ENTREGA` só com `@LC_PERMITE_RETROATIVO = 1` (confirmação + permissão).
- Proposta da mesma empresa, do mesmo cliente, na última revisão e com status que permite amostra; produto vem do item.
- Fabricação/validade parciais (só ano, mês/ano ou data completa), com validação do dia do mês.
- Revisão antiga somente leitura; com boletim emitido, alteração só por revisão.
- Com resultado: tipo, proposta e produto não mudam; quantidade não muda se houver movimentação manual.
- Exclusão só da revisão 0, sem resultados e sem movimentação manual.
- Revisões apontam para a raiz (`ID_AMOSTRA_RAIZ`), onde ficam estoque, saldo e localização.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_PRODUTOS`, `ANO_AMOSTRA`, `COD_AMOSTRA`, `ID_PEDIDOS_VENDA`, `DT_VALIDADE`, `DT_FABRICACAO`, `NR_AMOSTRA` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_HIST_AMOSTRAS`.
- `TINYINT(1)` → `bool`: `ULTIMA_REVISAO`, `ACOMPANHA_FICHA_TECNICA`, `ACOMPANHA_PADRAO_ANALITICO`, `ACOMPANHA_CA_CLIENTE`, `ENVIAR_OUTRO_LABORATORIO`.
- `ENUM` → enum C# com conversão para string: `TIPO_DOCUMENTO` ('BOLETIM','RELATORIO').
- `DATE` → `DateOnly`: `DT_ENTREGA`, `DT_FABRICACAO`, `DT_VALIDADE`, `DT_ENTREGA_VERIFICACAO`.
- `TIME` → `TimeOnly`: `HR_ENTREGA`, `HR_ENTREGA_VERIFICACAO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Enviar `ANO_AMOSTRA = 0`, `COD_AMOSTRA = 0`, `NR_AMOSTRA = ""` e **reler** a linha após o INSERT para mostrar o número.
- Gravar a amostra e chamar `SP_AMOSTRA_GERAR_TESTES` na mesma transação da aplicação.
- `SET @LC_PERMITE_RETROATIVO = 1` na mesma conexão do INSERT, só quando autorizado.

## Definição (DDL)
```sql
CREATE TABLE LAB_HIST_AMOSTRAS (
  ID_LAB_HIST_AMOSTRAS       INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ANO_AMOSTRA                SMALLINT     NOT NULL COMMENT 'Ano do SERVIDOR na inclusão (trigger)',
  COD_AMOSTRA                INT          NOT NULL COMMENT 'Sequencial por empresa/ano (trigger)',
  REVISAO                    INT          NOT NULL DEFAULT 0,
  ID_AMOSTRAS_TIPO           INT          NOT NULL,
  ID_ANALISES_TIPO           INT          NOT NULL,
  NR_AMOSTRA                 VARCHAR(25)  NOT NULL COMMENT 'Número formatado (trigger)',
  ID_AMOSTRA_ORIGEM          INT          NULL COMMENT 'Revisão de onde foi copiada',
  ID_AMOSTRA_RAIZ            INT          NULL COMMENT 'Revisão 0 (amostra física). NULL na própria raiz',
  ULTIMA_REVISAO             TINYINT(1)   NOT NULL DEFAULT 1,
  MOTIVO_REVISAO             VARCHAR(500) NULL,
  TIPO_DOCUMENTO             ENUM('BOLETIM','RELATORIO') NOT NULL DEFAULT 'BOLETIM',
  ID_ENTIDADES               INT          NOT NULL COMMENT 'Cliente',
  NOME_CONTATO               VARCHAR(100) NULL,
  ID_LAB_PROPOSTAS           INT          NOT NULL,
  ID_LAB_PROPOSTAS_PRODUTOS  INT          NOT NULL,
  ID_PEDIDOS_VENDA           INT          NULL,
  ID_PRODUTOS                INT          NOT NULL,
  DT_ENTREGA                 DATE         NOT NULL,
  HR_ENTREGA                 TIME         NOT NULL,
  LOCAL_RECEBIMENTO          VARCHAR(50)  NOT NULL,
  ID_EMBALAGENS_TIPOS        INT          NOT NULL,
  QTDE_EMBALAGENS_ENTREGUE   DECIMAL(15,4) NOT NULL DEFAULT 0,
  NR_LOTE                    VARCHAR(30)  NULL,
  -- Fabricação/Validade podem ser PARCIAIS (só mês/ano, só ano) — particularidade do legado
  FABRICACAO_DIA             TINYINT      NULL,
  FABRICACAO_MES             TINYINT      NULL,
  FABRICACAO_ANO             SMALLINT     NULL,
  VALIDADE_DIA               TINYINT      NULL,
  VALIDADE_MES               TINYINT      NULL,
  VALIDADE_ANO               SMALLINT     NULL,
  DT_FABRICACAO              DATE         NULL COMMENT 'Legado; preenchido só quando a data é completa',
  DT_VALIDADE                DATE         NULL COMMENT 'Legado; preenchido só quando a data é completa',
  NOTA_ROTULO                VARCHAR(255) NULL,
  DT_ENTREGA_VERIFICACAO     DATE         NULL,
  HR_ENTREGA_VERIFICACAO     TIME         NULL,
  ESPECIE_AMOSTRA            VARCHAR(50)  NULL,
  ASPECTO_AMOSTRA            VARCHAR(50)  NULL,
  COR                        VARCHAR(20)  NULL,
  OUTRAS_CARACTERISTICAS     VARCHAR(100) NULL,
  QTDE_AMOSTRA_VERIFICACAO   DECIMAL(15,4) NULL,
  TEMPERATURA_VERIFICACAO    DECIMAL(7,2)  NULL,
  OBSERVACAO_VERIFICACAO     VARCHAR(255) NULL,
  ACOMPANHA_FICHA_TECNICA    TINYINT(1)   NOT NULL DEFAULT 0,
  ACOMPANHA_PADRAO_ANALITICO TINYINT(1)   NOT NULL DEFAULT 0,
  ACOMPANHA_CA_CLIENTE       TINYINT(1)   NOT NULL DEFAULT 0,
  ENVIAR_OUTRO_LABORATORIO   TINYINT(1)   NOT NULL DEFAULT 0,
  QTDE_ENVIO_OUTRO_LABORATORIO DECIMAL(15,4) NOT NULL DEFAULT 0,
  ID_AMOSTRAS_STATUS         INT          NOT NULL,
  ID_ENTIDADES_FUNC_RESP     INT          NOT NULL COMMENT 'Responsável pelo recebimento',
  ID_ENTIDADES_FUNC_DIG      INT          NOT NULL COMMENT 'Profissional do usuário logado',
  NOME_COLETOR               VARCHAR(60)  NULL,
  DT_HR_COLETA               DATETIME     NULL,
  TX_OBSERVACAO              MEDIUMTEXT   NULL COMMENT 'Antiga LAB_HIST_AMOSTRAS_OBSERVACAO (RTF/HTML)',
  ID_LEGADO                  INT          NULL,
  CRIADO_EM                  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR                 INT          NULL,
  ALTERADO_EM                DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR               INT          NULL,
  CONSTRAINT UQ_AMO_NUM UNIQUE (ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, REVISAO),
  INDEX IX_AMO_NR (ID_EMPRESAS, NR_AMOSTRA),
  INDEX IX_AMO_CLI (ID_EMPRESAS, ID_ENTIDADES, DT_ENTREGA),
  INDEX IX_AMO_PROP (ID_LAB_PROPOSTAS),
  INDEX IX_AMO_ENTREGA (ID_EMPRESAS, DT_ENTREGA),
  CONSTRAINT CK_AMO_FABDIA CHECK (FABRICACAO_DIA IS NULL OR FABRICACAO_DIA BETWEEN 1 AND 31),
  CONSTRAINT CK_AMO_FABMES CHECK (FABRICACAO_MES IS NULL OR FABRICACAO_MES BETWEEN 1 AND 12),
  CONSTRAINT CK_AMO_VALDIA CHECK (VALIDADE_DIA IS NULL OR VALIDADE_DIA BETWEEN 1 AND 31),
  CONSTRAINT CK_AMO_VALMES CHECK (VALIDADE_MES IS NULL OR VALIDADE_MES BETWEEN 1 AND 12),
  CONSTRAINT FK_AMO_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_AMO_TIPO FOREIGN KEY (ID_AMOSTRAS_TIPO) REFERENCES AMOSTRAS_TIPO (ID_AMOSTRAS_TIPO),
  CONSTRAINT FK_AMO_ANATIPO FOREIGN KEY (ID_ANALISES_TIPO) REFERENCES ANALISES_TIPO (ID_ANALISES_TIPO),
  CONSTRAINT FK_AMO_ORIGEM FOREIGN KEY (ID_AMOSTRA_ORIGEM) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_AMO_RAIZ FOREIGN KEY (ID_AMOSTRA_RAIZ) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_AMO_CLI FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_AMO_PROP FOREIGN KEY (ID_LAB_PROPOSTAS) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS),
  CONSTRAINT FK_AMO_PROPPROD FOREIGN KEY (ID_LAB_PROPOSTAS_PRODUTOS) REFERENCES LAB_PROPOSTAS_PRODUTOS (ID_LAB_PROPOSTAS_PRODUTOS),
  CONSTRAINT FK_AMO_PEDVEN FOREIGN KEY (ID_PEDIDOS_VENDA) REFERENCES PEDIDOS_VENDA (ID_PEDIDOS_VENDA),
  CONSTRAINT FK_AMO_PROD FOREIGN KEY (ID_PRODUTOS) REFERENCES PRODUTOS (ID_PRODUTOS),
  CONSTRAINT FK_AMO_EMB FOREIGN KEY (ID_EMBALAGENS_TIPOS) REFERENCES EMBALAGENS_TIPOS (ID_EMBALAGENS_TIPOS),
  CONSTRAINT FK_AMO_STATUS FOREIGN KEY (ID_AMOSTRAS_STATUS) REFERENCES AMOSTRAS_STATUS (ID_AMOSTRAS_STATUS),
  CONSTRAINT FK_AMO_RESP FOREIGN KEY (ID_ENTIDADES_FUNC_RESP) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_AMO_DIG FOREIGN KEY (ID_ENTIDADES_FUNC_DIG) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
