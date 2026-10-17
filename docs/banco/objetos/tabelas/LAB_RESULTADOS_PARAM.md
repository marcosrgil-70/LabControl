# LAB_RESULTADOS_PARAM

Resultado de cada teste: numérico (símbolo, valor × 10^exp, unidade) ou descritivo, mais a especificação.

**Grupo:** Resultados, boletins e relatório de ensaio · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-16

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_RESULTADOS_PARAM` | 19.528 |

Comentário do script: _Um resultado por TESTE da amostra. TIPO_RESULTADO: 1 = numérico (símbolo + valor × 10^exp + unidade) | 0 = descritivo (LAB_TIPOS_RESULTADOS) Mesma regra para a ESPECIFICAÇÃO (*_ESPECIF)._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_RESULTADOS_PARAM` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_RESPAR_BI |  |
| 3 | `ID_LAB_RESULTADOS_ANALISES` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_HIST_AMOSTRAS_TESTES` | INT | não |  | aplicação |  |
| 5 | `DT_INICIAL` | DATE | não |  | aplicação |  |
| 6 | `DT_RESULTADO` | DATE | não |  | aplicação |  |
| 7 | `ID_ENTIDADES_FUNC_ANALISE` | INT | sim |  | aplicação | Analista responsável |
| 8 | `ID_ENTIDADES_FUNC_DIG` | INT | sim |  | aplicação |  |
| 9 | `QT_AMOSTRA` | DECIMAL(15,4) | sim |  | aplicação | Quantidade de amostra analisada |
| 10 | `ID_UNIDADES_AMOSTRA` | INT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 11 | `TIPO_RESULTADO` | TINYINT(1) | não | 1 | aplicação |  |
| 12 | `SIMBOLO_GRANDEZA` | VARCHAR(2) | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) | <, >, <=, >=, ~ |
| 13 | `VR_RESULTADO` | VARCHAR(20) | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) | Texto com vírgula decimal, ex.: 3,5 |
| 14 | `VR_EXP_RESULTADO` | SMALLINT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) | Expoente base 10 (3,5 x 10^4) |
| 15 | `ID_UNIDADES` | INT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 16 | `ID_LAB_TIPOS_RESULTADOS` | INT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 17 | `TIPO_RESULTADO_ESPECIF` | TINYINT(1) | não | 1 | aplicação |  |
| 18 | `SIMBOLO_GR_ESPECIF` | VARCHAR(2) | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 19 | `VR_RESULTADO_ESPECIF` | VARCHAR(20) | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 20 | `VR_EXP_ESPECIF` | SMALLINT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 21 | `ID_UNIDADES_ESPECIF` | INT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 22 | `ID_LAB_TIPO_RESULTADOS_ESP` | INT | sim |  | aplicação (TG_RESPAR_BI, TG_RESPAR_BU limpa → NULL) |  |
| 23 | `VR_SATISFEITO` | TINYINT(1) | sim |  | aplicação | Conforme/Não conforme (informado pelo analista) |
| 24 | `RESULTADO_FORMATADO` | VARCHAR(80) | sim |  | trigger TG_RESPAR_BI, trigger TG_RESPAR_BU | Trigger: "< 1,0e2 UFC/g" ou "Ausência em 25 g" |
| 25 | `ESPECIF_FORMATADA` | VARCHAR(80) | sim |  | trigger TG_RESPAR_BI, trigger TG_RESPAR_BU | Trigger |
| 26 | `ID_PARAM_ORIGEM` | INT | sim |  | aplicação | Linha da revisão de origem |
| 27 | `ID_LEGADO` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_RESULTADOS_PARAM`
- **Única** `UQ_RESPAR_TESTE` (ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRAS_TESTES)
- **Check** `CK_RESPAR_DATAS`: `DT_RESULTADO >= DT_INICIAL`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_RESULTADOS_ANALISES` | [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_LAB_RESULTADOS_ANALISES | CASCADE |
| `ID_LAB_HIST_AMOSTRAS_TESTES` | [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_HIST_AMOSTRAS_TESTES | bloqueia |
| `ID_ENTIDADES_FUNC_ANALISE` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_DIG` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_UNIDADES_AMOSTRA` | [`UNIDADES`](../tabelas/UNIDADES.md) | ID_UNIDADES | bloqueia |
| `ID_UNIDADES` | [`UNIDADES`](../tabelas/UNIDADES.md) | ID_UNIDADES | bloqueia |
| `ID_LAB_TIPOS_RESULTADOS` | [`LAB_TIPOS_RESULTADOS`](../tabelas/LAB_TIPOS_RESULTADOS.md) | ID_LAB_TIPOS_RESULTADOS | bloqueia |
| `ID_UNIDADES_ESPECIF` | [`UNIDADES`](../tabelas/UNIDADES.md) | ID_UNIDADES | bloqueia |
| `ID_LAB_TIPO_RESULTADOS_ESP` | [`LAB_TIPOS_RESULTADOS`](../tabelas/LAB_TIPOS_RESULTADOS.md) | ID_LAB_TIPOS_RESULTADOS | bloqueia |
| `ID_PARAM_ORIGEM` | [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_RESULTADOS_PARAM | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_PARAM_ORIGEM | bloqueia |
| [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md) | ID_LAB_RESULTADOS_PARAM | bloqueia |

## Triggers
- [`TG_RESPAR_BI`](../triggers/TG_RESPAR_BI.md) — BEFORE INSERT: Teste da mesma amostra; revisão antiga bloqueada; numérico × descritivo exclusivos; zeros → NULL; textos formatados.
- [`TG_RESPAR_BU`](../triggers/TG_RESPAR_BU.md) — BEFORE UPDATE: Bloqueia revisão antiga e resultado com boletim emitido; mantém exclusividade e textos formatados.
- [`TG_RESPAR_BD`](../triggers/TG_RESPAR_BD.md) — BEFORE DELETE: Bloqueia exclusão em revisão antiga ou de parâmetro usado em boletim.

## Usada por
- **Views:** [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md)
- **Rotinas que alteram:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md)
- **Rotinas que leem:** [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_BOLPAR_BI`](../triggers/TG_BOLPAR_BI.md)

## Telas e processos
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T30-04 Parâmetros analisados](../../../telas/T30-04-rel-parametros-analisados.md) · [T30-05 Tempo das análises](../../../telas/T30-05-rel-tempo-analises.md) · [T30-06 Produção por período](../../../telas/T30-06-rel-producao.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Regras de negócio
- O teste precisa ser da mesma amostra do resultado; um resultado por teste.
- Numérico × descritivo são exclusivos: a trigger limpa os campos incompatíveis (resultado e especificação).
- `RESULTADO_FORMATADO`/`ESPECIF_FORMATADA` calculados pela trigger (`FN_FORMATA_RESULTADO`).
- `DT_RESULTADO ≥ DT_INICIAL` (CHECK); na tela: início ≥ entrega e resultado ≤ hoje.
- Com boletim emitido não pode ser alterado (gerar revisão); usado em boletim não pode ser excluído.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `RESULTADO_FORMATADO`, `ESPECIF_FORMATADA` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_RESULTADOS_PARAM`.
- `TINYINT(1)` → `bool`: `TIPO_RESULTADO`, `TIPO_RESULTADO_ESPECIF`, `VR_SATISFEITO`.
- `DATE` → `DateOnly`: `DT_INICIAL`, `DT_RESULTADO`.
- `VR_RESULTADO` é **texto** com vírgula (ex.: "3,5"), validado como número.
- Reler `RESULTADO_FORMATADO` após gravar para a prévia.

## Definição (DDL)
```sql
CREATE TABLE LAB_RESULTADOS_PARAM (
  ID_LAB_RESULTADOS_PARAM     INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                 INT          NOT NULL,
  ID_LAB_RESULTADOS_ANALISES  INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS_TESTES INT          NOT NULL,
  DT_INICIAL                  DATE         NOT NULL,
  DT_RESULTADO                DATE         NOT NULL,
  ID_ENTIDADES_FUNC_ANALISE   INT          NULL COMMENT 'Analista responsável',
  ID_ENTIDADES_FUNC_DIG       INT          NULL,
  QT_AMOSTRA                  DECIMAL(15,4) NULL COMMENT 'Quantidade de amostra analisada',
  ID_UNIDADES_AMOSTRA         INT          NULL,
  TIPO_RESULTADO              TINYINT(1)   NOT NULL DEFAULT 1,
  SIMBOLO_GRANDEZA            VARCHAR(2)   NULL COMMENT '<, >, <=, >=, ~',
  VR_RESULTADO                VARCHAR(20)  NULL COMMENT 'Texto com vírgula decimal, ex.: 3,5',
  VR_EXP_RESULTADO            SMALLINT     NULL COMMENT 'Expoente base 10 (3,5 x 10^4)',
  ID_UNIDADES                 INT          NULL,
  ID_LAB_TIPOS_RESULTADOS     INT          NULL,
  TIPO_RESULTADO_ESPECIF      TINYINT(1)   NOT NULL DEFAULT 1,
  SIMBOLO_GR_ESPECIF          VARCHAR(2)   NULL,
  VR_RESULTADO_ESPECIF        VARCHAR(20)  NULL,
  VR_EXP_ESPECIF              SMALLINT     NULL,
  ID_UNIDADES_ESPECIF         INT          NULL,
  ID_LAB_TIPO_RESULTADOS_ESP  INT          NULL,
  VR_SATISFEITO               TINYINT(1)   NULL COMMENT 'Conforme/Não conforme (informado pelo analista)',
  RESULTADO_FORMATADO         VARCHAR(80)  NULL COMMENT 'Trigger: "< 1,0e2 UFC/g" ou "Ausência em 25 g"',
  ESPECIF_FORMATADA           VARCHAR(80)  NULL COMMENT 'Trigger',
  ID_PARAM_ORIGEM             INT          NULL COMMENT 'Linha da revisão de origem',
  ID_LEGADO                   INT          NULL,
  CONSTRAINT UQ_RESPAR_TESTE UNIQUE (ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRAS_TESTES),
  CONSTRAINT CK_RESPAR_DATAS CHECK (DT_RESULTADO >= DT_INICIAL),
  CONSTRAINT FK_RESPAR_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_RESPAR_RES FOREIGN KEY (ID_LAB_RESULTADOS_ANALISES) REFERENCES LAB_RESULTADOS_ANALISES (ID_LAB_RESULTADOS_ANALISES) ON DELETE CASCADE,
  CONSTRAINT FK_RESPAR_TESTE FOREIGN KEY (ID_LAB_HIST_AMOSTRAS_TESTES) REFERENCES LAB_HIST_AMOSTRAS_TESTES (ID_LAB_HIST_AMOSTRAS_TESTES),
  CONSTRAINT FK_RESPAR_ANALISTA FOREIGN KEY (ID_ENTIDADES_FUNC_ANALISE) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_RESPAR_DIG FOREIGN KEY (ID_ENTIDADES_FUNC_DIG) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_RESPAR_UNIAMO FOREIGN KEY (ID_UNIDADES_AMOSTRA) REFERENCES UNIDADES (ID_UNIDADES),
  CONSTRAINT FK_RESPAR_UNI FOREIGN KEY (ID_UNIDADES) REFERENCES UNIDADES (ID_UNIDADES),
  CONSTRAINT FK_RESPAR_TIPRES FOREIGN KEY (ID_LAB_TIPOS_RESULTADOS) REFERENCES LAB_TIPOS_RESULTADOS (ID_LAB_TIPOS_RESULTADOS),
  CONSTRAINT FK_RESPAR_UNIESP FOREIGN KEY (ID_UNIDADES_ESPECIF) REFERENCES UNIDADES (ID_UNIDADES),
  CONSTRAINT FK_RESPAR_TIPRESESP FOREIGN KEY (ID_LAB_TIPO_RESULTADOS_ESP) REFERENCES LAB_TIPOS_RESULTADOS (ID_LAB_TIPOS_RESULTADOS),
  CONSTRAINT FK_RESPAR_ORIGEM FOREIGN KEY (ID_PARAM_ORIGEM) REFERENCES LAB_RESULTADOS_PARAM (ID_LAB_RESULTADOS_PARAM)
) ENGINE=InnoDB;
```
