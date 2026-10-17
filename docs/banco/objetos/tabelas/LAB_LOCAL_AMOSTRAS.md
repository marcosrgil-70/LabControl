# LAB_LOCAL_AMOSTRAS

Arquivamento (armário/prateleira/caixa) e descarte da amostra física (1 por amostra raiz).

**Grupo:** Amostras e estoque · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_LOCAL_AMOSTRAS` (`STATUS` 0/1, `DT_HR_DESCATE`) | 2.686 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_LOCAL_AMOSTRAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação | Amostra raiz |
| 4 | `STATUS` | ENUM('ARQUIVADA','DESCARTADA') | não | 'ARQUIVADA' | aplicação |  |
| 5 | `DT_HR_ARQUIVO` | DATETIME | não |  | aplicação |  |
| 6 | `NR_ARMARIO` | VARCHAR(15) | sim |  | aplicação |  |
| 7 | `NR_PRATELEIRA` | VARCHAR(15) | sim |  | aplicação |  |
| 8 | `NR_CAIXA` | VARCHAR(15) | sim |  | aplicação |  |
| 9 | `OBSERVACAO` | VARCHAR(250) | sim |  | aplicação |  |
| 10 | `DT_HR_DESCARTE` | DATETIME | sim |  | aplicação |  |
| 11 | `ID_ENTIDADES_FUNC_DESCARTE` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_LOCAL_AMOSTRAS`
- **Única** `UQ_LOCAL_AMO` (ID_LAB_HIST_AMOSTRAS)
- **Check** `CK_LOCAL_DESC`: `STATUS = 'ARQUIVADA' OR DT_HR_DESCARTE IS NOT NULL`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| `ID_ENTIDADES_FUNC_DESCARTE` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md)
- **Processos:** [P13 Arquivo e descarte](../../../processos/P13-arquivo-descarte.md)

## Regras de negócio
- Uma localização por amostra (`UQ_LOCAL_AMO`), sempre a raiz.
- DESCARTADA exige data/hora do descarte (`CK_LOCAL_DESC`); o profissional do descarte é o do usuário logado.
- "Apagar indicação de descarte" volta para ARQUIVADA e limpa data/profissional (o legado não voltava o status).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_LOCAL_AMOSTRAS`.
- `ENUM` → enum C# com conversão para string: `STATUS` ('ARQUIVADA','DESCARTADA').
- Gravar sempre o ID da raiz (`COALESCE(ID_AMOSTRA_RAIZ, ID)`).

## Definição (DDL)
```sql
CREATE TABLE LAB_LOCAL_AMOSTRAS (
  ID_LAB_LOCAL_AMOSTRAS      INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL COMMENT 'Amostra raiz',
  STATUS                     ENUM('ARQUIVADA','DESCARTADA') NOT NULL DEFAULT 'ARQUIVADA',
  DT_HR_ARQUIVO              DATETIME     NOT NULL,
  NR_ARMARIO                 VARCHAR(15)  NULL,
  NR_PRATELEIRA              VARCHAR(15)  NULL,
  NR_CAIXA                   VARCHAR(15)  NULL,
  OBSERVACAO                 VARCHAR(250) NULL,
  DT_HR_DESCARTE             DATETIME     NULL,
  ID_ENTIDADES_FUNC_DESCARTE INT          NULL,
  CONSTRAINT UQ_LOCAL_AMO UNIQUE (ID_LAB_HIST_AMOSTRAS),
  CONSTRAINT CK_LOCAL_DESC CHECK (STATUS = 'ARQUIVADA' OR DT_HR_DESCARTE IS NOT NULL),
  CONSTRAINT FK_LOCAL_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_LOCAL_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS) ON DELETE CASCADE,
  CONSTRAINT FK_LOCAL_FUNC FOREIGN KEY (ID_ENTIDADES_FUNC_DESCARTE) REFERENCES ENTIDADES_FUNCIONARIOS (ID_ENTIDADES)
) ENGINE=InnoDB;
```
