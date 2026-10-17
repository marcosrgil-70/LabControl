# LAB_PROPOSTAS_PRODANALISES

Parâmetros/análises de cada produto da proposta, com método, idioma, prazo, tipo de documento e valores.

**Grupo:** Propostas e pedidos · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-12

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PROPOSTAS_PRODANALISES` | 19.252 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PROPOSTAS_PRODANALISES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_PRODAN_BI |  |
| 3 | `ID_LAB_PROPOSTAS` | INT | não |  | trigger TG_PRODAN_BI |  |
| 4 | `ID_LAB_PROPOSTAS_PRODUTOS` | INT | não |  | aplicação |  |
| 5 | `ID_LAB_PARAMETROS_ANALISES` | INT | não |  | aplicação |  |
| 6 | `ID_ANALISES_METODOS` | INT | não |  | aplicação |  |
| 7 | `ID_IDIOMAS` | INT | não |  | aplicação |  |
| 8 | `ID_PRAZOS` | INT | não |  | aplicação |  |
| 9 | `TIPO_DOCUMENTO` | ENUM('BOLETIM','RELATORIO') | não | 'BOLETIM' | aplicação |  |
| 10 | `QTDE_AMOSTRAS` | INT | não | 1 | aplicação |  |
| 11 | `VR_UNITARIO` | DECIMAL(15,2) | não | 0 | aplicação |  |
| 12 | `VR_SUBTOTAL` | DECIMAL(15,2) | não | 0 | trigger TG_PRODAN_BI, trigger TG_PRODAN_BU | QTDE × UNITÁRIO (trigger) |
| 13 | `PORC_DESCONTO` | DECIMAL(7,4) | não | 0 | aplicação |  |
| 14 | `VR_DESCONTO` | DECIMAL(15,2) | não | 0 | trigger TG_PRODAN_BI, trigger TG_PRODAN_BU |  |
| 15 | `VR_TOTAL` | DECIMAL(15,2) | não | 0 | trigger TG_PRODAN_BI, trigger TG_PRODAN_BU | SUBTOTAL − DESCONTO (trigger) |
| 16 | `ST_REV_ANTERIOR` | TINYINT(1) | não | 0 | aplicação | 1 = já existia na revisão de origem |
| 17 | `ID_ORIGEM` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PROPOSTAS_PRODANALISES`

- **Check** `CK_PRODAN_DESC`: `PORC_DESCONTO BETWEEN 0 AND 100`
- **Índice** `IX_PRODAN_PROD` (ID_LAB_PROPOSTAS_PRODUTOS)
- **Índice** `IX_PRODAN_PROP` (ID_LAB_PROPOSTAS)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_PROPOSTAS` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | CASCADE |
| `ID_LAB_PROPOSTAS_PRODUTOS` | [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_LAB_PROPOSTAS_PRODUTOS | CASCADE |
| `ID_LAB_PARAMETROS_ANALISES` | [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md) | ID_LAB_PARAMETROS_ANALISES | bloqueia |
| `ID_ANALISES_METODOS` | [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md) | ID_ANALISES_METODOS | bloqueia |
| `ID_IDIOMAS` | [`IDIOMAS`](../tabelas/IDIOMAS.md) | ID_IDIOMAS | bloqueia |
| `ID_PRAZOS` | [`PRAZOS`](../tabelas/PRAZOS.md) | ID_PRAZOS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_PROPOSTAS_PRODANALISES | SET NULL |

## Triggers
- [`TG_PRODAN_BI`](../triggers/TG_PRODAN_BI.md) — BEFORE INSERT: Análise herda empresa/proposta do item; bloqueia revisão antiga; calcula subtotal, desconto e total da linha.
- [`TG_PRODAN_BU`](../triggers/TG_PRODAN_BU.md) — BEFORE UPDATE: Recalcula os valores da linha; bloqueia revisão antiga.
- [`TG_PRODAN_BD`](../triggers/TG_PRODAN_BD.md) — BEFORE DELETE: Bloqueia exclusão de análise em revisão antiga.
- [`TG_PRODAN_AI`](../triggers/TG_PRODAN_AI.md) — AFTER INSERT: Recalcula os totais da proposta após incluir análise.
- [`TG_PRODAN_AU`](../triggers/TG_PRODAN_AU.md) — AFTER UPDATE: Recalcula os totais da proposta após alterar análise.
- [`TG_PRODAN_AD`](../triggers/TG_PRODAN_AD.md) — AFTER DELETE: Recalcula os totais da proposta após excluir análise.

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS_ANALISES`](../views/VW_LAB_PROPOSTAS_ANALISES.md)
- **Rotinas que alteram:** [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md)
- **Rotinas que leem:** [`SP_RECALCULAR_PROPOSTA`](../rotinas/SP_RECALCULAR_PROPOSTA.md), [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md)
- **Triggers de outras tabelas que alteram:** [`TG_PROPPROD_AU`](../triggers/TG_PROPPROD_AU.md)
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Regras de negócio
- Subtotal = qtde × unitário; % > 0 calcula o desconto; total = subtotal − desconto (trigger).
- Desconto não pode passar do subtotal.
- Cada inclusão/alteração/exclusão recalcula a proposta.
- `ST_REV_ANTERIOR = 1` = linha veio da revisão anterior (destaque na tela).
- É a origem dos testes da amostra (`SP_AMOSTRA_GERAR_TESTES`).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `ID_LAB_PROPOSTAS`, `VR_SUBTOTAL`, `VR_DESCONTO`, `VR_TOTAL` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_PROPOSTAS_PRODANALISES`.
- `TINYINT(1)` → `bool`: `ST_REV_ANTERIOR`.
- `ENUM` → enum C# com conversão para string: `TIPO_DOCUMENTO` ('BOLETIM','RELATORIO').
- Valores calculados pelo banco: a tela recalcula só para exibir.
- `ID_EMPRESAS` e `ID_LAB_PROPOSTAS` preenchidos pela trigger a partir do produto.

## Definição (DDL)
```sql
CREATE TABLE LAB_PROPOSTAS_PRODANALISES (
  ID_LAB_PROPOSTAS_PRODANALISES INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_PROPOSTAS           INT          NOT NULL,
  ID_LAB_PROPOSTAS_PRODUTOS  INT          NOT NULL,
  ID_LAB_PARAMETROS_ANALISES INT          NOT NULL,
  ID_ANALISES_METODOS        INT          NOT NULL,
  ID_IDIOMAS                 INT          NOT NULL,
  ID_PRAZOS                  INT          NOT NULL,
  TIPO_DOCUMENTO             ENUM('BOLETIM','RELATORIO') NOT NULL DEFAULT 'BOLETIM',
  QTDE_AMOSTRAS              INT          NOT NULL DEFAULT 1,
  VR_UNITARIO                DECIMAL(15,2) NOT NULL DEFAULT 0,
  VR_SUBTOTAL                DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'QTDE × UNITÁRIO (trigger)',
  PORC_DESCONTO              DECIMAL(7,4)  NOT NULL DEFAULT 0,
  VR_DESCONTO                DECIMAL(15,2) NOT NULL DEFAULT 0,
  VR_TOTAL                   DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'SUBTOTAL − DESCONTO (trigger)',
  ST_REV_ANTERIOR            TINYINT(1)   NOT NULL DEFAULT 0 COMMENT '1 = já existia na revisão de origem',
  ID_ORIGEM                  INT          NULL,
  INDEX IX_PRODAN_PROD (ID_LAB_PROPOSTAS_PRODUTOS),
  INDEX IX_PRODAN_PROP (ID_LAB_PROPOSTAS),
  CONSTRAINT CK_PRODAN_DESC CHECK (PORC_DESCONTO BETWEEN 0 AND 100),
  CONSTRAINT FK_PRODAN_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PRODAN_PROP FOREIGN KEY (ID_LAB_PROPOSTAS) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS) ON DELETE CASCADE,
  CONSTRAINT FK_PRODAN_PRODUTO FOREIGN KEY (ID_LAB_PROPOSTAS_PRODUTOS) REFERENCES LAB_PROPOSTAS_PRODUTOS (ID_LAB_PROPOSTAS_PRODUTOS) ON DELETE CASCADE,
  CONSTRAINT FK_PRODAN_PARAM FOREIGN KEY (ID_LAB_PARAMETROS_ANALISES) REFERENCES LAB_PARAMETROS_ANALISES (ID_LAB_PARAMETROS_ANALISES),
  CONSTRAINT FK_PRODAN_MET FOREIGN KEY (ID_ANALISES_METODOS) REFERENCES ANALISES_METODOS (ID_ANALISES_METODOS),
  CONSTRAINT FK_PRODAN_IDI FOREIGN KEY (ID_IDIOMAS) REFERENCES IDIOMAS (ID_IDIOMAS),
  CONSTRAINT FK_PRODAN_PRZ FOREIGN KEY (ID_PRAZOS) REFERENCES PRAZOS (ID_PRAZOS)
) ENGINE=InnoDB;
```
