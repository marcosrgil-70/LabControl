# LAB_PROPOSTAS

Cabeçalho da proposta comercial. Cada revisão é uma linha nova (`REV_PROPOSTA`).

**Grupo:** Propostas e pedidos · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-11

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PROPOSTAS` | 2.358 |

Comentário do script: _6. PROPOSTAS_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PROPOSTAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `COD_PROPOSTA` | INT | não |  | trigger TG_PROPOSTAS_BI | Sequencial anual por empresa |
| 4 | `ANO_PROPOSTA` | SMALLINT | não |  | trigger TG_PROPOSTAS_BI | Ano com 4 dígitos (exibição YY) |
| 5 | `REV_PROPOSTA` | INT | não | 0 | aplicação |  |
| 6 | `NR_PROPOSTA` | VARCHAR(20) | não |  | trigger TG_PROPOSTAS_BI, trigger TG_PROPOSTAS_BU | 000123/26-R0 (preenchido por trigger) |
| 7 | `ID_LAB_PROPOSTAS_ORIGEM` | INT | sim |  | aplicação | Revisão de onde esta foi copiada |
| 8 | `ULTIMA_REVISAO` | TINYINT(1) | não | 1 | aplicação |  |
| 9 | `MOTIVO_REVISAO` | VARCHAR(500) | sim |  | aplicação |  |
| 10 | `ID_ENTIDADES` | INT | não |  | aplicação | Cliente |
| 11 | `ID_ENT_CONTATO` | INT | sim |  | aplicação | Contato comercial (ENTIDADES_EMAILS do cliente) |
| 12 | `ID_ENTIDADES_COMERC` | INT | sim |  | aplicação | Comercial responsável (ENTIDADES) |
| 13 | `ID_ENTIDADES_FUNC` | INT | não |  | aplicação | Profissional que digitou |
| 14 | `ID_ENT_END_LAUDO` | INT | não |  | aplicação | Endereço para emissão do resultado |
| 15 | `ID_ENT_CONTATO_RESULTADO` | INT | não |  | aplicação | Contato que recebe o resultado |
| 16 | `ID_ENT_COBRANCA` | INT | não |  | aplicação | Entidade de cobrança/NF (pode ser outra) |
| 17 | `ID_ENT_END_COBRANCA` | INT | sim |  | aplicação | Endereço da NF/cobrança |
| 18 | `ID_ENT_CONTATO_NF` | INT | sim |  | aplicação | Contato da NF (ENTIDADES_EMAILS da cobrança) |
| 19 | `DT_SOLICITACAO` | DATE | não |  | trigger TG_PROPOSTAS_BI |  |
| 20 | `DT_VALIDADE` | DATE | sim |  | aplicação |  |
| 21 | `DT_ENVIO_CLIENTE` | DATE | sim |  | trigger TG_PROPOSTAS_BU |  |
| 22 | `DT_AUTORIZACAO` | DATE | sim |  | aplicação |  |
| 23 | `ID_LAB_PROPOSTAS_STATUS` | INT | não |  | aplicação |  |
| 24 | `ID_CONDICOES_PAGTOS` | INT | não |  | aplicação |  |
| 25 | `ID_MOEDAS` | INT | sim |  | aplicação |  |
| 26 | `VR_SOMA_VR_ANALIS` | DECIMAL(15,2) | não | 0 | aplicação | Σ subtotal das análises |
| 27 | `VR_SOMA_DESC_ANALIS` | DECIMAL(15,2) | não | 0 | aplicação | Σ desconto das análises |
| 28 | `VR_SOMA_VR_TOT_ANALIS` | DECIMAL(15,2) | não | 0 | aplicação | Σ total das análises |
| 29 | `PORC_DESCONTO` | DECIMAL(7,4) | não | 0 | aplicação | Desconto geral % |
| 30 | `VR_DESCONTO` | DECIMAL(15,2) | não | 0 | aplicação | Desconto geral R$ |
| 31 | `VR_TOTAL_PROPOSTA` | DECIMAL(15,2) | não | 0 | aplicação |  |
| 32 | `ID_LEGADO` | INT | sim |  | aplicação |  |
| 33 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 34 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 35 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 36 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PROPOSTAS`
- **Única** `UQ_PROP_NUM` (ID_EMPRESAS, ANO_PROPOSTA, COD_PROPOSTA, REV_PROPOSTA)

- **Índice** `IX_PROP_CLIENTE` (ID_EMPRESAS, ID_ENTIDADES)
- **Índice** `IX_PROP_ULTIMA` (ID_EMPRESAS, ULTIMA_REVISAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_PROPOSTAS_ORIGEM` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_ENT_CONTATO` | [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) | ID_ENTIDADES_EMAILS | bloqueia |
| `ID_ENTIDADES_COMERC` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_ENT_END_LAUDO` | [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_ENTIDADES_ENDERECOS | bloqueia |
| `ID_ENT_CONTATO_RESULTADO` | [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) | ID_ENTIDADES_EMAILS | bloqueia |
| `ID_ENT_COBRANCA` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_ENT_END_COBRANCA` | [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_ENTIDADES_ENDERECOS | bloqueia |
| `ID_ENT_CONTATO_NF` | [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) | ID_ENTIDADES_EMAILS | bloqueia |
| `ID_LAB_PROPOSTAS_STATUS` | [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md) | ID_LAB_PROPOSTAS_STATUS | bloqueia |
| `ID_CONDICOES_PAGTOS` | [`CONDICOES_PAGTOS`](../tabelas/CONDICOES_PAGTOS.md) | ID_CONDICOES_PAGTOS | bloqueia |
| `ID_MOEDAS` | [`MOEDAS`](../tabelas/MOEDAS.md) | ID_MOEDAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS_ORIGEM | bloqueia |
| [`LAB_PROPOSTAS_TEXTOS`](../tabelas/LAB_PROPOSTAS_TEXTOS.md) | ID_LAB_PROPOSTAS | CASCADE |
| [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_LAB_PROPOSTAS | CASCADE |
| [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_LAB_PROPOSTAS | CASCADE |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_LAB_PROPOSTAS | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_PROPOSTAS | bloqueia |

## Triggers
- [`TG_PROPOSTAS_BI`](../triggers/TG_PROPOSTAS_BI.md) — BEFORE INSERT: Numeração da proposta (ano do servidor + sequência anual), data de solicitação padrão, validade ≥ solicitação e `NR_PROPOSTA`.
- [`TG_PROPOSTAS_BU`](../triggers/TG_PROPOSTAS_BU.md) — BEFORE UPDATE: Número/revisão/empresa imutáveis; revisão antiga somente leitura; status "Enviado ao cliente" grava `DT_ENVIO_CLIENTE`; recalcula `NR_PROPOSTA`.

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md), [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** [`SP_RECALCULAR_PROPOSTA`](../rotinas/SP_RECALCULAR_PROPOSTA.md), [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md)
- **Rotinas que leem:** [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_PROPPROD_BI`](../triggers/TG_PROPPROD_BI.md), [`TG_PROPPROD_BU`](../triggers/TG_PROPPROD_BU.md), [`TG_PRODAN_BI`](../triggers/TG_PRODAN_BI.md), [`TG_PRODAN_BU`](../triggers/TG_PRODAN_BU.md), [`TG_PRODAN_BD`](../triggers/TG_PRODAN_BD.md), [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md)

## Telas e processos
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md) · [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Regras de negócio
- Número `000123/26-R0` gerado pelo banco (ano do servidor, sequência por empresa/ano) — a aplicação envia `COD_PROPOSTA = 0`.
- Revisão antiga (`ULTIMA_REVISAO = 0`) é somente leitura (trigger).
- Totais calculados pelo banco (`SP_RECALCULAR_PROPOSTA`).
- Contatos/endereços obrigatórios: endereço do laudo, contato do resultado, entidade de cobrança (+ endereço e contato da NF na tela).
- Status "Enviado ao cliente" grava `DT_ENVIO_CLIENTE`.
- Com pedido, amostra ou envio ao cliente, alteração só por revisão.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ANO_PROPOSTA`, `COD_PROPOSTA`, `DT_SOLICITACAO`, `NR_PROPOSTA`, `DT_ENVIO_CLIENTE` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_PROPOSTAS`.
- `TINYINT(1)` → `bool`: `ULTIMA_REVISAO`.
- `DATE` → `DateOnly`: `DT_SOLICITACAO`, `DT_VALIDADE`, `DT_ENVIO_CLIENTE`, `DT_AUTORIZACAO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Não enviar `ANO/COD/NR_PROPOSTA` nem totais: reler após gravar.
- Revisão via `SP_GERAR_REVISAO_PROPOSTA` fora de transação EF.

## Definição (DDL)
```sql
CREATE TABLE LAB_PROPOSTAS (
  ID_LAB_PROPOSTAS         INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS              INT          NOT NULL,
  COD_PROPOSTA             INT          NOT NULL COMMENT 'Sequencial anual por empresa',
  ANO_PROPOSTA             SMALLINT     NOT NULL COMMENT 'Ano com 4 dígitos (exibição YY)',
  REV_PROPOSTA             INT          NOT NULL DEFAULT 0,
  NR_PROPOSTA              VARCHAR(20)  NOT NULL COMMENT '000123/26-R0 (preenchido por trigger)',
  ID_LAB_PROPOSTAS_ORIGEM  INT          NULL COMMENT 'Revisão de onde esta foi copiada',
  ULTIMA_REVISAO           TINYINT(1)   NOT NULL DEFAULT 1,
  MOTIVO_REVISAO           VARCHAR(500) NULL,
  ID_ENTIDADES             INT          NOT NULL COMMENT 'Cliente',
  ID_ENT_CONTATO           INT          NULL COMMENT 'Contato comercial (ENTIDADES_EMAILS do cliente)',
  ID_ENTIDADES_COMERC      INT          NULL COMMENT 'Comercial responsável (ENTIDADES)',
  ID_ENTIDADES_FUNC        INT          NOT NULL COMMENT 'Profissional que digitou',
  ID_ENT_END_LAUDO         INT          NOT NULL COMMENT 'Endereço para emissão do resultado',
  ID_ENT_CONTATO_RESULTADO INT          NOT NULL COMMENT 'Contato que recebe o resultado',
  ID_ENT_COBRANCA          INT          NOT NULL COMMENT 'Entidade de cobrança/NF (pode ser outra)',
  ID_ENT_END_COBRANCA      INT          NULL COMMENT 'Endereço da NF/cobrança',
  ID_ENT_CONTATO_NF        INT          NULL COMMENT 'Contato da NF (ENTIDADES_EMAILS da cobrança)',
  DT_SOLICITACAO           DATE         NOT NULL,
  DT_VALIDADE              DATE         NULL,
  DT_ENVIO_CLIENTE         DATE         NULL,
  DT_AUTORIZACAO           DATE         NULL,
  ID_LAB_PROPOSTAS_STATUS  INT          NOT NULL,
  ID_CONDICOES_PAGTOS      INT          NOT NULL,
  ID_MOEDAS                INT          NULL,
  VR_SOMA_VR_ANALIS        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Σ subtotal das análises',
  VR_SOMA_DESC_ANALIS      DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Σ desconto das análises',
  VR_SOMA_VR_TOT_ANALIS    DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Σ total das análises',
  PORC_DESCONTO            DECIMAL(7,4)  NOT NULL DEFAULT 0 COMMENT 'Desconto geral %',
  VR_DESCONTO              DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Desconto geral R$',
  VR_TOTAL_PROPOSTA        DECIMAL(15,2) NOT NULL DEFAULT 0,
  ID_LEGADO                INT          NULL,
  CRIADO_EM                DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR               INT          NULL,
  ALTERADO_EM              DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR             INT          NULL,
  CONSTRAINT UQ_PROP_NUM UNIQUE (ID_EMPRESAS, ANO_PROPOSTA, COD_PROPOSTA, REV_PROPOSTA),
  INDEX IX_PROP_CLIENTE (ID_EMPRESAS, ID_ENTIDADES),
  INDEX IX_PROP_ULTIMA (ID_EMPRESAS, ULTIMA_REVISAO),
  CONSTRAINT FK_PROP_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PROP_ORIGEM FOREIGN KEY (ID_LAB_PROPOSTAS_ORIGEM) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS),
  CONSTRAINT FK_PROP_CLI FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PROP_CONTATO FOREIGN KEY (ID_ENT_CONTATO) REFERENCES ENTIDADES_EMAILS (ID_ENTIDADES_EMAILS),
  CONSTRAINT FK_PROP_COMERC FOREIGN KEY (ID_ENTIDADES_COMERC) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PROP_FUNC FOREIGN KEY (ID_ENTIDADES_FUNC) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PROP_ENDLAU FOREIGN KEY (ID_ENT_END_LAUDO) REFERENCES ENTIDADES_ENDERECOS (ID_ENTIDADES_ENDERECOS),
  CONSTRAINT FK_PROP_CONTRES FOREIGN KEY (ID_ENT_CONTATO_RESULTADO) REFERENCES ENTIDADES_EMAILS (ID_ENTIDADES_EMAILS),
  CONSTRAINT FK_PROP_COBR FOREIGN KEY (ID_ENT_COBRANCA) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PROP_ENDCOBR FOREIGN KEY (ID_ENT_END_COBRANCA) REFERENCES ENTIDADES_ENDERECOS (ID_ENTIDADES_ENDERECOS),
  CONSTRAINT FK_PROP_CONTNF FOREIGN KEY (ID_ENT_CONTATO_NF) REFERENCES ENTIDADES_EMAILS (ID_ENTIDADES_EMAILS),
  CONSTRAINT FK_PROP_STATUS FOREIGN KEY (ID_LAB_PROPOSTAS_STATUS) REFERENCES LAB_PROPOSTAS_STATUS (ID_LAB_PROPOSTAS_STATUS),
  CONSTRAINT FK_PROP_CONDPAG FOREIGN KEY (ID_CONDICOES_PAGTOS) REFERENCES CONDICOES_PAGTOS (ID_CONDICOES_PAGTOS),
  CONSTRAINT FK_PROP_MOEDA FOREIGN KEY (ID_MOEDAS) REFERENCES MOEDAS (ID_MOEDAS)
) ENGINE=InnoDB;
```
