# LAB_BOLETIM_PARAMETROS

Parâmetros **escolhidos** para o boletim, com o texto do resultado congelado e a ordem de impressão.

**Grupo:** Resultados, boletins e relatório de ensaio · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-17

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_BOLETIM_PARAMETROS` | 19.615 |

Comentário do script: _Parâmetros ESCOLHIDOS para o boletim (subconjunto dos resultados) com o texto congelado._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_BOLETIM_PARAMETROS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_BOLPAR_BI |  |
| 3 | `ID_LAB_BOLETIM_ANALISES` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_RESULTADOS_PARAM` | INT | não |  | aplicação |  |
| 5 | `ORDEM` | INT | não | 0 | aplicação |  |
| 6 | `DESC_PARAMETRO` | VARCHAR(150) | não |  | aplicação | LAB_PARAMETROS_ANALISES.DESC_REDUZIDA |
| 7 | `RESULTADO_CONCAT` | VARCHAR(80) | sim |  | aplicação | Resultado 1 (numérico formatado) |
| 8 | `TIPO_RESULTADO_DESC` | VARCHAR(80) | sim |  | aplicação | Resultado 2 (descritivo) |
| 9 | `UNIDADE_SG` | VARCHAR(15) | sim |  | aplicação |  |
| 10 | `ESPECIFICACAO` | VARCHAR(80) | sim |  | aplicação |  |
| 11 | `ACREDITADO` | TINYINT(1) | não | 0 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_BOLETIM_PARAMETROS`
- **Única** `UQ_BOLPAR` (ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADOS_PARAM)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_BOLETIM_ANALISES` | [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_LAB_BOLETIM_ANALISES | CASCADE |
| `ID_LAB_RESULTADOS_PARAM` | [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_RESULTADOS_PARAM | bloqueia |

### É referenciada por
—

## Triggers
- [`TG_BOLPAR_BI`](../triggers/TG_BOLPAR_BI.md) — BEFORE INSERT: Parâmetro do mesmo resultado; bloqueia boletim emitido.
- [`TG_BOLPAR_BU`](../triggers/TG_BOLPAR_BU.md) — BEFORE UPDATE: Bloqueia alteração de parâmetro de boletim emitido.
- [`TG_BOLPAR_BD`](../triggers/TG_BOLPAR_BD.md) — BEFORE DELETE: Bloqueia exclusão de parâmetro de boletim emitido.

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Rotinas que leem:** [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_RESPAR_BD`](../triggers/TG_RESPAR_BD.md)

## Telas e processos
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Regras de negócio
- Só parâmetros do resultado vinculado ao boletim; cada um uma vez (`UQ_BOLPAR`).
- Texto (`RESULTADO_CONCAT`, `TIPO_RESULTADO_DESC`, unidade, especificação) gravado no momento da seleção.
- Boletim emitido: nada pode ser incluído/alterado/excluído.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_BOLETIM_PARAMETROS`.
- `TINYINT(1)` → `bool`: `ACREDITADO`.
- "Atualizar textos" (não emitido) regrava a partir de `VW_LAB_RESULTADOS`.

## Definição (DDL)
```sql
CREATE TABLE LAB_BOLETIM_PARAMETROS (
  ID_LAB_BOLETIM_PARAMETROS  INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_BOLETIM_ANALISES    INT          NOT NULL,
  ID_LAB_RESULTADOS_PARAM    INT          NOT NULL,
  ORDEM                      INT          NOT NULL DEFAULT 0,
  DESC_PARAMETRO             VARCHAR(150) NOT NULL COMMENT 'LAB_PARAMETROS_ANALISES.DESC_REDUZIDA',
  RESULTADO_CONCAT           VARCHAR(80)  NULL COMMENT 'Resultado 1 (numérico formatado)',
  TIPO_RESULTADO_DESC        VARCHAR(80)  NULL COMMENT 'Resultado 2 (descritivo)',
  UNIDADE_SG                 VARCHAR(15)  NULL,
  ESPECIFICACAO              VARCHAR(80)  NULL,
  ACREDITADO                 TINYINT(1)   NOT NULL DEFAULT 0,
  CONSTRAINT UQ_BOLPAR UNIQUE (ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADOS_PARAM),
  CONSTRAINT FK_BOLPAR_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_BOLPAR_BOL FOREIGN KEY (ID_LAB_BOLETIM_ANALISES) REFERENCES LAB_BOLETIM_ANALISES (ID_LAB_BOLETIM_ANALISES) ON DELETE CASCADE,
  CONSTRAINT FK_BOLPAR_RESPAR FOREIGN KEY (ID_LAB_RESULTADOS_PARAM) REFERENCES LAB_RESULTADOS_PARAM (ID_LAB_RESULTADOS_PARAM)
) ENGINE=InnoDB;
```
