# LAB_MOV_AMOSTRAS

Movimentação física (entradas/saídas) da amostra — sempre na amostra **raiz**.

**Grupo:** Amostras e estoque · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_MOV_AMOSTRAS` (`AMOSTRA_COMPLEMENTAR`: A = automática, C = entrada complementar, M = saída manual) | 5.770 (só entradas automáticas) |

Comentário do script: _Movimentação física (estoque) — sempre na amostra RAIZ (revisão 0)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_MOV_AMOSTRAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_MOV_BI |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | trigger TG_MOV_BI |  |
| 4 | `DATA_MOV` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 5 | `QTDE` | DECIMAL(15,4) | não |  | aplicação |  |
| 6 | `E_S` | ENUM('E','S') | não |  | aplicação |  |
| 7 | `JUSTIFICATIVA` | VARCHAR(250) | sim |  | aplicação |  |
| 8 | `AUTOMATICA` | TINYINT(1) | não | 0 | aplicação | 1 = entrada gerada no recebimento (antigo AMOSTRA_COMPLEMENTAR=A) |
| 9 | `ID_ENTIDADES_FUNC` | INT | não |  | aplicação |  |
| 10 | `CRIADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_MOV_AMOSTRAS`

- **Check** `CK_MOV_QTDE`: `QTDE > 0`
- **Índice** `IX_MOV_AMO` (ID_LAB_HIST_AMOSTRAS, DATA_MOV)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| `ID_ENTIDADES_FUNC` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_MOV_AMOSTRAS_PARAM`](../tabelas/LAB_MOV_AMOSTRAS_PARAM.md) | ID_LAB_MOV_AMOSTRAS | CASCADE |

## Triggers
- [`TG_MOV_BI`](../triggers/TG_MOV_BI.md) — BEFORE INSERT: Movimento vai sempre para a amostra raiz; saída sem saldo é recusada, com a linha do saldo travada até o fim da transação (duas saídas ao mesmo tempo não passam juntas — D-25).
- [`TG_MOV_BD`](../triggers/TG_MOV_BD.md) — BEFORE DELETE: A entrada automática do recebimento não pode ser excluída; a entrada complementar também não, se o saldo ficaria negativo sem ela (RN-06.15, linha do saldo travada — D-25).
- [`TG_MOV_AI`](../triggers/TG_MOV_AI.md) — AFTER INSERT: Recalcula o saldo após incluir movimento.
- [`TG_MOV_AU`](../triggers/TG_MOV_AU.md) — AFTER UPDATE: Recalcula o saldo após alterar movimento.
- [`TG_MOV_AD`](../triggers/TG_MOV_AD.md) — AFTER DELETE: Recalcula o saldo após excluir movimento.

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`PR_ATUALIZA_SALDO`](../rotinas/PR_ATUALIZA_SALDO.md)
- **Triggers de outras tabelas que alteram:** [`TG_AMOSTRAS_AI`](../triggers/TG_AMOSTRAS_AI.md), [`TG_AMOSTRAS_AU`](../triggers/TG_AMOSTRAS_AU.md)
- **Triggers de outras tabelas que leem:** [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md), [`TG_AMOSTRAS_BD`](../triggers/TG_AMOSTRAS_BD.md)

## Telas e processos
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Regras de negócio
- Entrada automática criada pelo recebimento (revisão 0) com a quantidade entregue; não pode ser excluída.
- Movimento enviado para uma revisão é redirecionado para a raiz (`TG_MOV_BI`).
- Saída maior que o saldo é recusada.
- Cada movimento recalcula `LAB_HIST_AMOSTRAS_SALDO`.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_LAB_HIST_AMOSTRAS`, `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_MOV_AMOSTRAS`.
- `TINYINT(1)` → `bool`: `AUTOMATICA`.
- `ENUM` → enum C# com conversão para string: `E_S` ('E','S').
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DATA_MOV` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- `AUTOMATICA` nunca é enviada como 1 pela aplicação.

## Definição (DDL)
```sql
CREATE TABLE LAB_MOV_AMOSTRAS (
  ID_LAB_MOV_AMOSTRAS        INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL,
  DATA_MOV                   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  QTDE                       DECIMAL(15,4) NOT NULL,
  E_S                        ENUM('E','S') NOT NULL,
  JUSTIFICATIVA              VARCHAR(250) NULL,
  AUTOMATICA                 TINYINT(1)   NOT NULL DEFAULT 0 COMMENT '1 = entrada gerada no recebimento (antigo AMOSTRA_COMPLEMENTAR=A)',
  ID_ENTIDADES_FUNC          INT          NOT NULL,
  CRIADO_POR                 INT          NULL,
  INDEX IX_MOV_AMO (ID_LAB_HIST_AMOSTRAS, DATA_MOV),
  CONSTRAINT CK_MOV_QTDE CHECK (QTDE > 0),
  CONSTRAINT FK_MOV_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_MOV_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS) ON DELETE CASCADE,
  CONSTRAINT FK_MOV_FUNC FOREIGN KEY (ID_ENTIDADES_FUNC) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
