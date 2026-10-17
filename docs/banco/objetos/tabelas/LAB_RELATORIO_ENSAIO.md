# LAB_RELATORIO_ENSAIO

Relatório de ensaio livre (alternativa ao boletim quando o item da proposta tem `TIPO_DOCUMENTO = RELATORIO`).

**Grupo:** Resultados, boletins e relatório de ensaio · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** —

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_RELATORIO_ENSAIO` (vazia — tela existe, nunca usada) | 0 |

Comentário do script: _Relatório de Ensaio (documento livre, alternativo ao boletim — TIPO_DOCUMENTO = RELATORIO)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_RELATORIO_ENSAIO` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `NR_RELATORIO` | INT | não |  | trigger TG_RE_BI |  |
| 4 | `ANO_RELATORIO` | SMALLINT | não |  | trigger TG_RE_BI |  |
| 5 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 6 | `DH_GERACAO` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 7 | `DH_EMISSAO` | DATETIME | sim |  | aplicação |  |
| 8 | `ID_ENTIDADES_FUNC_GER` | INT | não |  | aplicação |  |
| 9 | `ID_ENTIDADES_FUNC_EMI` | INT | sim |  | aplicação |  |
| 10 | `TX_RELATORIO` | LONGTEXT | não |  | aplicação | HTML |

## Chaves, restrições e índices
- **PK:** `ID_LAB_RELATORIO_ENSAIO`
- **Única** `UQ_RE_NR` (ID_EMPRESAS, ANO_RELATORIO, NR_RELATORIO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | bloqueia |
| `ID_ENTIDADES_FUNC_GER` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |
| `ID_ENTIDADES_FUNC_EMI` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
—

## Triggers
- [`TG_RE_BI`](../triggers/TG_RE_BI.md) — BEFORE INSERT: Número anual do relatório de ensaio.

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md)
- **Processos:** [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Regras de negócio
- Número anual por empresa (`TG_RE_BI`); o legado gravava `ULT_COD_RE = ID_EMPRESAS` por engano.
- **Confirmar com o usuário se a tela ainda é necessária** antes de implementar.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ANO_RELATORIO`, `NR_RELATORIO` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_RELATORIO_ENSAIO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DH_GERACAO` — mapear como gerado pelo banco.

## Definição (DDL)
```sql
CREATE TABLE LAB_RELATORIO_ENSAIO (
  ID_LAB_RELATORIO_ENSAIO    INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  NR_RELATORIO               INT          NOT NULL,
  ANO_RELATORIO              SMALLINT     NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL,
  DH_GERACAO                 DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  DH_EMISSAO                 DATETIME     NULL,
  ID_ENTIDADES_FUNC_GER      INT          NOT NULL,
  ID_ENTIDADES_FUNC_EMI      INT          NULL,
  TX_RELATORIO               LONGTEXT     NOT NULL COMMENT 'HTML',
  CONSTRAINT UQ_RE_NR UNIQUE (ID_EMPRESAS, ANO_RELATORIO, NR_RELATORIO),
  CONSTRAINT FK_RE_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_RE_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT FK_RE_GER FOREIGN KEY (ID_ENTIDADES_FUNC_GER) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES),
  CONSTRAINT FK_RE_EMI FOREIGN KEY (ID_ENTIDADES_FUNC_EMI) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
