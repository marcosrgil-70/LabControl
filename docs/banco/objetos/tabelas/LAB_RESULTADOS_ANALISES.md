# LAB_RESULTADOS_ANALISES

Cabeçalho do resultado de uma amostra (uma linha por revisão do resultado).

**Grupo:** Resultados, boletins e relatório de ensaio · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-16

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_RESULTADOS_ANALISES` | 5.345 |

Comentário do script: _9. RESULTADOS_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_RESULTADOS_ANALISES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_RESULT_BI |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 4 | `REVISAO` | INT | não | 0 | aplicação | Numeração contínua por amostra física (raiz) |
| 5 | `ID_RESULTADO_ORIGEM` | INT | sim |  | aplicação |  |
| 6 | `ULTIMA_REVISAO` | TINYINT(1) | não | 1 | aplicação |  |
| 7 | `MOTIVO_REVISAO` | VARCHAR(500) | sim |  | aplicação |  |
| 8 | `ID_ENTIDADES_FUNC_DIG` | INT | não |  | aplicação |  |
| 9 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 10 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 11 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 12 | `ALTERADO_POR` | INT | sim |  | aplicação |  |
| 13 | `ID_LEGADO` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_RESULTADOS_ANALISES`
- **Única** `UQ_RES_REV` (ID_LAB_HIST_AMOSTRAS, REVISAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| `ID_RESULTADO_ORIGEM` | [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_LAB_RESULTADOS_ANALISES | bloqueia |
| `ID_ENTIDADES_FUNC_DIG` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_RESULTADO_ORIGEM | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_RESULTADOS_ANALISES | CASCADE |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_LAB_RESULTADOS_ANALISES | bloqueia |

## Triggers
- [`TG_RESULT_BI`](../triggers/TG_RESULT_BI.md) — BEFORE INSERT: Resultado herda a empresa da amostra.
- [`TG_RESULT_BU`](../triggers/TG_RESULT_BU.md) — BEFORE UPDATE: Revisão antiga somente leitura; amostra/revisão imutáveis.
- [`TG_RESULT_BD`](../triggers/TG_RESULT_BD.md) — BEFORE DELETE: Exclusão só da última revisão, sem boletim; revisão > 0 só por "Desfazer revisão".

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md), [`VW_LAB_BOLETINS`](../views/VW_LAB_BOLETINS.md)
- **Rotinas que alteram:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md), [`SP_EXCLUIR_REVISAO_RESULTADO`](../rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md)
- **Rotinas que leem:** [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md), [`PR_REVISAO_AMOSTRA`](../rotinas/PR_REVISAO_AMOSTRA.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md), [`TG_AMOSTRAS_BD`](../triggers/TG_AMOSTRAS_BD.md), [`TG_RESPAR_BI`](../triggers/TG_RESPAR_BI.md), [`TG_RESPAR_BU`](../triggers/TG_RESPAR_BU.md), [`TG_RESPAR_BD`](../triggers/TG_RESPAR_BD.md), [`TG_BOLETIM_BI`](../triggers/TG_BOLETIM_BI.md)

## Telas e processos
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Regras de negócio
- Um resultado por revisão da amostra (`UQ_RES_REV`); "Incluir" de amostra que já tem resultado abre o existente.
- `REVISAO` contínua por amostra física (todas as revisões da raiz).
- Revisão antiga somente leitura; excluir só a última e sem boletim; revisão > 0 só via "Desfazer revisão".

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_RESULTADOS_ANALISES`.
- `TINYINT(1)` → `bool`: `ULTIMA_REVISAO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Revisão/desfazer pelas procedures, fora de transação EF.

## Definição (DDL)
```sql
CREATE TABLE LAB_RESULTADOS_ANALISES (
  ID_LAB_RESULTADOS_ANALISES INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL,
  REVISAO                    INT          NOT NULL DEFAULT 0 COMMENT 'Numeração contínua por amostra física (raiz)',
  ID_RESULTADO_ORIGEM        INT          NULL,
  ULTIMA_REVISAO             TINYINT(1)   NOT NULL DEFAULT 1,
  MOTIVO_REVISAO             VARCHAR(500) NULL,
  ID_ENTIDADES_FUNC_DIG      INT          NOT NULL,
  CRIADO_EM                  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR                 INT          NULL,
  ALTERADO_EM                DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR               INT          NULL,
  ID_LEGADO                  INT          NULL,
  CONSTRAINT UQ_RES_REV UNIQUE (ID_LAB_HIST_AMOSTRAS, REVISAO),
  CONSTRAINT FK_RES_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_RES_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_RES_ORIGEM FOREIGN KEY (ID_RESULTADO_ORIGEM) REFERENCES LAB_RESULTADOS_ANALISES (ID_LAB_RESULTADOS_ANALISES),
  CONSTRAINT FK_RES_DIG FOREIGN KEY (ID_ENTIDADES_FUNC_DIG) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
