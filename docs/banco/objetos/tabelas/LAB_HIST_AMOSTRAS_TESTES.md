# LAB_HIST_AMOSTRAS_TESTES

Testes (parâmetros) a analisar na amostra, copiados das análises do item da proposta.

**Grupo:** Amostras e estoque · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_HIST_AMOSTRAS_TESTES` | 20.551 |

Comentário do script: _Testes (parâmetros) solicitados para a amostra, copiados da proposta._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_HIST_AMOSTRAS_TESTES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_TESTES_BI |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_PROPOSTAS_PRODANALISES` | INT | sim |  | aplicação | Linha da proposta que originou o teste |
| 5 | `ID_LAB_PARAMETROS_ANALISES` | INT | não |  | aplicação |  |
| 6 | `ID_ANALISES_TIPO` | INT | não |  | aplicação |  |
| 7 | `ID_ANALISES_METODOS` | INT | não |  | aplicação |  |
| 8 | `ID_IDIOMAS` | INT | não |  | aplicação |  |
| 9 | `ID_PRAZOS` | INT | não |  | aplicação |  |
| 10 | `DT_PREVISTA` | DATE | sim |  | trigger TG_TESTES_BI | DT_ENTREGA + PRAZOS.QTDE_DIAS |
| 11 | `ID_ANALISES_STATUS` | INT | sim |  | aplicação |  |
| 12 | `TX_ANALISES_TIPO` | TEXT | sim |  | aplicação |  |
| 13 | `ID_TESTE_ORIGEM` | INT | sim |  | aplicação | Teste da revisão de origem (mapeia resultados) |

## Chaves, restrições e índices
- **PK:** `ID_LAB_HIST_AMOSTRAS_TESTES`

- **Índice** `IX_TESTE_AMO` (ID_LAB_HIST_AMOSTRAS)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| `ID_LAB_PROPOSTAS_PRODANALISES` | [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_LAB_PROPOSTAS_PRODANALISES | SET NULL |
| `ID_LAB_PARAMETROS_ANALISES` | [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md) | ID_LAB_PARAMETROS_ANALISES | bloqueia |
| `ID_ANALISES_TIPO` | [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md) | ID_ANALISES_TIPO | bloqueia |
| `ID_ANALISES_METODOS` | [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md) | ID_ANALISES_METODOS | bloqueia |
| `ID_IDIOMAS` | [`IDIOMAS`](../tabelas/IDIOMAS.md) | ID_IDIOMAS | bloqueia |
| `ID_PRAZOS` | [`PRAZOS`](../tabelas/PRAZOS.md) | ID_PRAZOS | bloqueia |
| `ID_ANALISES_STATUS` | [`ANALISES_STATUS`](../tabelas/ANALISES_STATUS.md) | ID_ANALISES_STATUS | bloqueia |
| `ID_TESTE_ORIGEM` | [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_HIST_AMOSTRAS_TESTES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_TESTE_ORIGEM | bloqueia |
| [`LAB_MOV_AMOSTRAS_PARAM`](../tabelas/LAB_MOV_AMOSTRAS_PARAM.md) | ID_LAB_HIST_AMOSTRAS_TESTES | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_HIST_AMOSTRAS_TESTES | bloqueia |

## Triggers
- [`TG_TESTES_BI`](../triggers/TG_TESTES_BI.md) — BEFORE INSERT: Teste herda a empresa da amostra e calcula a data prevista (entrega + prazo).

## Usada por
- **Views:** [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md)
- **Rotinas que alteram:** [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`PR_REVISAO_AMOSTRA`](../rotinas/PR_REVISAO_AMOSTRA.md)
- **Rotinas que leem:** [`FN_ACREDITADO`](../rotinas/FN_ACREDITADO.md), [`FN_OBS_METODOS`](../rotinas/FN_OBS_METODOS.md), [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md), [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_RESPAR_BI`](../triggers/TG_RESPAR_BI.md)

## Telas e processos
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md)
- **Processos:** [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md)

## Regras de negócio
- Gerados por `SP_AMOSTRA_GERAR_TESTES` (apaga e recria) — recusado se já houver resultado.
- `DT_PREVISTA` = entrega + `PRAZOS.QTDE_DIAS`.
- `ID_TESTE_ORIGEM` liga o teste à revisão anterior (mapeia resultados mesmo com parâmetro repetido).
- Com resultados, os testes não são mais regerados (mensagem do legado).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `DT_PREVISTA` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_HIST_AMOSTRAS_TESTES`.
- `DATE` → `DateOnly`: `DT_PREVISTA`.
- Somente leitura na tela (gerados pelo banco).

## Definição (DDL)
```sql
CREATE TABLE LAB_HIST_AMOSTRAS_TESTES (
  ID_LAB_HIST_AMOSTRAS_TESTES   INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                   INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS          INT          NOT NULL,
  ID_LAB_PROPOSTAS_PRODANALISES INT          NULL COMMENT 'Linha da proposta que originou o teste',
  ID_LAB_PARAMETROS_ANALISES    INT          NOT NULL,
  ID_ANALISES_TIPO              INT          NOT NULL,
  ID_ANALISES_METODOS           INT          NOT NULL,
  ID_IDIOMAS                    INT          NOT NULL,
  ID_PRAZOS                     INT          NOT NULL,
  DT_PREVISTA                   DATE         NULL COMMENT 'DT_ENTREGA + PRAZOS.QTDE_DIAS',
  ID_ANALISES_STATUS            INT          NULL,
  TX_ANALISES_TIPO              TEXT         NULL,
  ID_TESTE_ORIGEM               INT          NULL COMMENT 'Teste da revisão de origem (mapeia resultados)',
  INDEX IX_TESTE_AMO (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_TESTE_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_TESTE_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS) ON DELETE CASCADE,
  CONSTRAINT FK_TESTE_PRODAN FOREIGN KEY (ID_LAB_PROPOSTAS_PRODANALISES) REFERENCES LAB_PROPOSTAS_PRODANALISES (ID_LAB_PROPOSTAS_PRODANALISES) ON DELETE SET NULL,
  CONSTRAINT FK_TESTE_PARAM FOREIGN KEY (ID_LAB_PARAMETROS_ANALISES) REFERENCES LAB_PARAMETROS_ANALISES (ID_LAB_PARAMETROS_ANALISES),
  CONSTRAINT FK_TESTE_ANATIPO FOREIGN KEY (ID_ANALISES_TIPO) REFERENCES ANALISES_TIPO (ID_ANALISES_TIPO),
  CONSTRAINT FK_TESTE_MET FOREIGN KEY (ID_ANALISES_METODOS) REFERENCES ANALISES_METODOS (ID_ANALISES_METODOS),
  CONSTRAINT FK_TESTE_IDI FOREIGN KEY (ID_IDIOMAS) REFERENCES IDIOMAS (ID_IDIOMAS),
  CONSTRAINT FK_TESTE_PRZ FOREIGN KEY (ID_PRAZOS) REFERENCES PRAZOS (ID_PRAZOS),
  CONSTRAINT FK_TESTE_STATUS FOREIGN KEY (ID_ANALISES_STATUS) REFERENCES ANALISES_STATUS (ID_ANALISES_STATUS),
  CONSTRAINT FK_TESTE_ORIGEM FOREIGN KEY (ID_TESTE_ORIGEM) REFERENCES LAB_HIST_AMOSTRAS_TESTES (ID_LAB_HIST_AMOSTRAS_TESTES)
) ENGINE=InnoDB;
```
