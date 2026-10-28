# LAB_BOLETIM_ANALISES

Cabeçalho do boletim (laudo): resultado vinculado, responsáveis, textos, emissão e PDF.

**Grupo:** Resultados, boletins e relatório de ensaio · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-17

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_BOLETIM_ANALISES` + `LAB_BOLETIM_OBS` | 5.312 |

Comentário do script: _10. BOLETINS (LAB_BOLETIM_OBS incorporada ao cabeçalho)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_BOLETIM_ANALISES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_BOLETIM_BI |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_RESULTADOS_ANALISES` | INT | não |  | aplicação |  |
| 5 | `COD_BOLETIM` | VARCHAR(30) | não |  | trigger TG_BOLETIM_BI | = NR_AMOSTRA da amostra |
| 6 | `REVISAO` | INT | não | 0 | aplicação |  |
| 7 | `ID_BOLETIM_ORIGEM` | INT | sim |  | aplicação |  |
| 8 | `ULTIMA_REVISAO` | TINYINT(1) | não | 1 | aplicação |  |
| 9 | `MOTIVO_REVISAO` | VARCHAR(500) | sim |  | aplicação |  |
| 10 | `ID_BOLETINS_STATUS` | INT | sim |  | aplicação |  |
| 11 | `DT_BOLETIM` | DATE | não |  | trigger TG_BOLETIM_BI |  |
| 12 | `DH_GERACAO` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 13 | `DH_EMISSAO` | DATETIME | sim |  | aplicação | Preenchido na EMISSÃO: bloqueia alterações |
| 14 | `ID_ENTIDADES_FUNC_GER` | INT | não |  | aplicação |  |
| 15 | `ID_ENTIDADES_FUNC_EMI` | INT | sim |  | aplicação |  |
| 16 | `ID_ENTIDADES_FUNC_RESP` | INT | sim |  | aplicação | Responsável técnico 1 |
| 17 | `ID_ENTIDADES_FUNC_RESP2` | INT | sim |  | aplicação | Responsável técnico 2 |
| 18 | `TXT_OBSERVACAO` | MEDIUMTEXT | sim |  | aplicação |  |
| 19 | `TXT_LEGENDA` | MEDIUMTEXT | sim |  | aplicação |  |
| 20 | `TXT_METODOLOGIA_UTIL` | MEDIUMTEXT | sim |  | aplicação | Metodologias NÃO acreditadas |
| 21 | `TXT_METODOLOGIA_ACRED` | MEDIUMTEXT | sim |  | aplicação | Metodologias acreditadas |
| 22 | `ARQUIVO_PDF` | VARCHAR(255) | sim |  | aplicação | Caminho do PDF emitido (imutável) |
| 23 | `HASH_PDF` | CHAR(64) | sim |  | aplicação |  |
| 24 | `ID_LEGADO` | INT | sim |  | aplicação |  |
| 25 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 26 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 27 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_BOLETIM_ANALISES`
- **Única** `UQ_BOL_REV` (ID_EMPRESAS, COD_BOLETIM, REVISAO)

- **Índice** `IX_BOL_AMO` (ID_LAB_HIST_AMOSTRAS)
- **Índice** `IX_BOL_RES` (ID_LAB_RESULTADOS_ANALISES)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| `ID_LAB_RESULTADOS_ANALISES` | [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_LAB_RESULTADOS_ANALISES | bloqueia |
| `ID_BOLETIM_ORIGEM` | [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_LAB_BOLETIM_ANALISES | bloqueia |
| `ID_BOLETINS_STATUS` | [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md) | ID_BOLETINS_STATUS | bloqueia |
| `ID_ENTIDADES_FUNC_GER` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_EMI` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_RESP` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_RESP2` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_BOLETIM_ORIGEM | bloqueia |
| [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md) | ID_LAB_BOLETIM_ANALISES | CASCADE |

## Triggers
- [`TG_BOLETIM_BI`](../triggers/TG_BOLETIM_BI.md) — BEFORE INSERT: Resultado da mesma amostra; `COD_BOLETIM` = número base da amostra; data padrão.
- [`TG_BOLETIM_BU`](../triggers/TG_BOLETIM_BU.md) — BEFORE UPDATE: Boletim emitido congelado (só o status muda); revisão antiga somente leitura; código/revisão/amostra imutáveis.
- [`TG_BOLETIM_BD`](../triggers/TG_BOLETIM_BD.md) — BEFORE DELETE: Emitido não se exclui; revisão > 0 só por "Desfazer revisão".

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_BOLETINS`](../views/VW_LAB_BOLETINS.md)
- **Rotinas que alteram:** [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md)
- **Rotinas que leem:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_RESULTADO`](../rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md), [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md), [`TG_RESULT_BD`](../triggers/TG_RESULT_BD.md), [`TG_RESPAR_BU`](../triggers/TG_RESPAR_BU.md), [`TG_BOLPAR_BI`](../triggers/TG_BOLPAR_BI.md), [`TG_BOLPAR_BU`](../triggers/TG_BOLPAR_BU.md), [`TG_BOLPAR_BD`](../triggers/TG_BOLPAR_BD.md)

## Telas e processos
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Regras de negócio
- `COD_BOLETIM` = número **base** da amostra (sem " Rnn"), constante entre revisões; `REVISAO` contínua por código.
- O resultado precisa ser da amostra do boletim.
- **Emitido (`DH_EMISSAO`) = congelado**: só o status pode mudar; não pode ser excluído.
- Revisão antiga somente leitura; revisão > 0 só se exclui por "Desfazer revisão" (não emitida).
- Na migração todo o histórico é congelado (`DH_EMISSAO = DH_GERACAO`).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `COD_BOLETIM`, `DT_BOLETIM` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_BOLETIM_ANALISES`.
- `TINYINT(1)` → `bool`: `ULTIMA_REVISAO`.
- `DATE` → `DateOnly`: `DT_BOLETIM`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DH_GERACAO`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Emissão: gerar PDF → salvar → SHA-256 → `SP_EMITIR_BOLETIM` (fora de transação EF).
- Reimpressão de emitido usa o `ARQUIVO_PDF` gravado.

## Definição (DDL)
```sql
CREATE TABLE LAB_BOLETIM_ANALISES (
  ID_LAB_BOLETIM_ANALISES    INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL,
  ID_LAB_RESULTADOS_ANALISES INT          NOT NULL,
  COD_BOLETIM                VARCHAR(30)  NOT NULL COMMENT '= NR_AMOSTRA da amostra',
  REVISAO                    INT          NOT NULL DEFAULT 0,
  ID_BOLETIM_ORIGEM          INT          NULL,
  ULTIMA_REVISAO             TINYINT(1)   NOT NULL DEFAULT 1,
  MOTIVO_REVISAO             VARCHAR(500) NULL,
  ID_BOLETINS_STATUS         INT          NULL,
  DT_BOLETIM                 DATE         NOT NULL,
  DH_GERACAO                 DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  DH_EMISSAO                 DATETIME     NULL COMMENT 'Preenchido na EMISSÃO: bloqueia alterações',
  ID_ENTIDADES_FUNC_GER      INT          NOT NULL,
  ID_ENTIDADES_FUNC_EMI      INT          NULL,
  ID_ENTIDADES_FUNC_RESP     INT          NULL COMMENT 'Responsável técnico 1',
  ID_ENTIDADES_FUNC_RESP2    INT          NULL COMMENT 'Responsável técnico 2',
  TXT_OBSERVACAO             MEDIUMTEXT   NULL,
  TXT_LEGENDA                MEDIUMTEXT   NULL,
  TXT_METODOLOGIA_UTIL       MEDIUMTEXT   NULL COMMENT 'Metodologias NÃO acreditadas',
  TXT_METODOLOGIA_ACRED      MEDIUMTEXT   NULL COMMENT 'Metodologias acreditadas',
  ARQUIVO_PDF                VARCHAR(255) NULL COMMENT 'Caminho do PDF emitido (imutável)',
  HASH_PDF                   CHAR(64)     NULL,
  ID_LEGADO                  INT          NULL,
  CRIADO_POR                 INT          NULL,
  ALTERADO_EM                DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR               INT          NULL,
  CONSTRAINT UQ_BOL_REV UNIQUE (ID_EMPRESAS, COD_BOLETIM, REVISAO),
  INDEX IX_BOL_AMO (ID_LAB_HIST_AMOSTRAS),
  INDEX IX_BOL_RES (ID_LAB_RESULTADOS_ANALISES),
  CONSTRAINT FK_BOL_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_BOL_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_BOL_RES FOREIGN KEY (ID_LAB_RESULTADOS_ANALISES) REFERENCES LAB_RESULTADOS_ANALISES (ID_LAB_RESULTADOS_ANALISES),
  CONSTRAINT FK_BOL_ORIGEM FOREIGN KEY (ID_BOLETIM_ORIGEM) REFERENCES LAB_BOLETIM_ANALISES (ID_LAB_BOLETIM_ANALISES),
  CONSTRAINT FK_BOL_STATUS FOREIGN KEY (ID_BOLETINS_STATUS) REFERENCES BOLETINS_STATUS (ID_BOLETINS_STATUS),
  CONSTRAINT FK_BOL_GER FOREIGN KEY (ID_ENTIDADES_FUNC_GER) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_BOL_EMI FOREIGN KEY (ID_ENTIDADES_FUNC_EMI) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_BOL_RESP FOREIGN KEY (ID_ENTIDADES_FUNC_RESP) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_BOL_RESP2 FOREIGN KEY (ID_ENTIDADES_FUNC_RESP2) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
