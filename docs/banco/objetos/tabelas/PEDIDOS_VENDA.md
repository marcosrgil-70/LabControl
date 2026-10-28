# PEDIDOS_VENDA

Pedido de venda gerado a partir da proposta aprovada (1 por proposta, considerando todas as revisões).

**Grupo:** Propostas e pedidos · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-13

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| Não era tabela: número em `LAB_PROPOSTAS.ID_PEDIDOS_VENDAS` (gerado por `SP_GET_ULT_PED_VENDA`) e copiado para a amostra | ≈1.435 (1.454 propostas com pedido − repetições) |

Comentário do script: _7. PEDIDOS DE VENDA (novo: no Firebird era só um número na proposta)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PEDIDOS_VENDA` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_PEDVEN_BI |  |
| 3 | `NR_PEDIDO` | INT | não |  | trigger TG_PEDVEN_BI | Sequencial por empresa (não anual, como no legado) |
| 4 | `DT_PEDIDO` | DATE | não |  | aplicação |  |
| 5 | `ID_LAB_PROPOSTAS` | INT | não |  | aplicação | Revisão da proposta vigente no pedido |
| 6 | `COD_PROPOSTA` | INT | não |  | aplicação |  |
| 7 | `ANO_PROPOSTA` | SMALLINT | não |  | aplicação |  |
| 8 | `ID_ENTIDADES` | INT | não |  | aplicação | Cliente |
| 9 | `ID_ENT_COBRANCA` | INT | não |  | aplicação |  |
| 10 | `ID_CONDICOES_PAGTOS` | INT | não |  | aplicação |  |
| 11 | `ID_MOEDAS` | INT | sim |  | aplicação |  |
| 12 | `DT_AUTORIZACAO` | DATE | não |  | aplicação |  |
| 13 | `AUTORIZADO_POR` | VARCHAR(100) | sim |  | aplicação | Pessoa do cliente que autorizou |
| 14 | `NR_PEDIDO_CLIENTE` | VARCHAR(40) | sim |  | aplicação | Nº de PO/ordem de compra do cliente |
| 15 | `VR_TOTAL` | DECIMAL(15,2) | não | 0 | aplicação |  |
| 16 | `SITUACAO` | ENUM('ABERTO','EM_EXECUCAO','CONCLUIDO','CANCELADO') | não | 'ABERTO' | aplicação |  |
| 17 | `CONCLUSAO_MANUAL` | TINYINT(1) | não | 0 | aplicação | Concluído à mão: a conclusão automática dispensa recebido = contratado (6.3, decisão do usuário) |
| 18 | `DT_CANCELAMENTO` | DATETIME | sim |  | aplicação |  |
| 19 | `MOTIVO_CANCELAMENTO` | VARCHAR(500) | sim |  | aplicação |  |
| 20 | `OBSERVACAO` | TEXT | sim |  | aplicação |  |
| 21 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 22 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 23 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 24 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PEDIDOS_VENDA`
- **Única** `UQ_PEDVEN_NR` (ID_EMPRESAS, NR_PEDIDO)
- **Única** `UQ_PEDVEN_PROP` (ID_EMPRESAS, ANO_PROPOSTA, COD_PROPOSTA) — Um pedido por proposta (todas as revisões)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_PROPOSTAS` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_ENT_COBRANCA` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | bloqueia |
| `ID_CONDICOES_PAGTOS` | [`CONDICOES_PAGTOS`](../tabelas/CONDICOES_PAGTOS.md) | ID_CONDICOES_PAGTOS | bloqueia |
| `ID_MOEDAS` | [`MOEDAS`](../tabelas/MOEDAS.md) | ID_MOEDAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md) | ID_PEDIDOS_VENDA | CASCADE |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_PEDIDOS_VENDA | bloqueia |

## Triggers
- [`TG_PEDVEN_BI`](../triggers/TG_PEDVEN_BI.md) — BEFORE INSERT: Número sequencial do pedido de venda (por empresa, não anual) quando não informado.

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md), [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md)

## Telas e processos
- **Telas:** [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)

## Regras de negócio
- Gerado só por `SP_GERAR_PEDIDO_VENDA` (exige data de autorização ≤ hoje, última revisão, com análises).
- Número sequencial por empresa (não anual), 9 dígitos na tela/impressão.
- Situação: ABERTO → EM_EXECUCAO → CONCLUIDO; CANCELADO com motivo (bloqueado se houver amostra).
- Atualizado para a revisão vigente por `SP_ATUALIZAR_PEDIDO_REVISAO`.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `NR_PEDIDO` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_PEDIDOS_VENDA`.
- `TINYINT(1)` → `bool`: `CONCLUSAO_MANUAL`.
- `ENUM` → enum C# com conversão para string: `SITUACAO` ('ABERTO','EM_EXECUCAO','CONCLUIDO','CANCELADO').
- `DATE` → `DateOnly`: `DT_PEDIDO`, `DT_AUTORIZACAO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Não inserir pelo EF: sempre pela procedure.

## Definição (DDL)
```sql
CREATE TABLE PEDIDOS_VENDA (
  ID_PEDIDOS_VENDA     INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS          INT          NOT NULL,
  NR_PEDIDO            INT          NOT NULL COMMENT 'Sequencial por empresa (não anual, como no legado)',
  DT_PEDIDO            DATE         NOT NULL,
  ID_LAB_PROPOSTAS     INT          NOT NULL COMMENT 'Revisão da proposta vigente no pedido',
  COD_PROPOSTA         INT          NOT NULL,
  ANO_PROPOSTA         SMALLINT     NOT NULL,
  ID_ENTIDADES         INT          NOT NULL COMMENT 'Cliente',
  ID_ENT_COBRANCA      INT          NOT NULL,
  ID_CONDICOES_PAGTOS  INT          NOT NULL,
  ID_MOEDAS            INT          NULL,
  DT_AUTORIZACAO       DATE         NOT NULL,
  AUTORIZADO_POR       VARCHAR(100) NULL COMMENT 'Pessoa do cliente que autorizou',
  NR_PEDIDO_CLIENTE    VARCHAR(40)  NULL COMMENT 'Nº de PO/ordem de compra do cliente',
  VR_TOTAL             DECIMAL(15,2) NOT NULL DEFAULT 0,
  SITUACAO             ENUM('ABERTO','EM_EXECUCAO','CONCLUIDO','CANCELADO') NOT NULL DEFAULT 'ABERTO',
  CONCLUSAO_MANUAL     TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'Concluído à mão: a conclusão automática dispensa recebido = contratado (6.3, decisão do usuário)',
  DT_CANCELAMENTO      DATETIME     NULL,
  MOTIVO_CANCELAMENTO  VARCHAR(500) NULL,
  OBSERVACAO           TEXT         NULL,
  CRIADO_EM            DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR           INT          NULL,
  ALTERADO_EM          DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR         INT          NULL,
  CONSTRAINT UQ_PEDVEN_NR UNIQUE (ID_EMPRESAS, NR_PEDIDO),
  CONSTRAINT UQ_PEDVEN_PROP UNIQUE (ID_EMPRESAS, ANO_PROPOSTA, COD_PROPOSTA) COMMENT 'Um pedido por proposta (todas as revisões)',
  CONSTRAINT FK_PEDVEN_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PEDVEN_PROP FOREIGN KEY (ID_LAB_PROPOSTAS) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS),
  CONSTRAINT FK_PEDVEN_CLI FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PEDVEN_COBR FOREIGN KEY (ID_ENT_COBRANCA) REFERENCES ENTIDADES (ID_ENTIDADES),
  CONSTRAINT FK_PEDVEN_COND FOREIGN KEY (ID_CONDICOES_PAGTOS) REFERENCES CONDICOES_PAGTOS (ID_CONDICOES_PAGTOS),
  CONSTRAINT FK_PEDVEN_MOEDA FOREIGN KEY (ID_MOEDAS) REFERENCES MOEDAS (ID_MOEDAS)
) ENGINE=InnoDB;
```
