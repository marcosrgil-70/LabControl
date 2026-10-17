# ENTIDADES_FONES

Telefones da entidade.

**Grupo:** Cadastros · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-06

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENTIDADES_FONES` | 695 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENTIDADES_FONES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_ENTFON_BI |  |
| 3 | `ID_ENTIDADES` | INT | não |  | aplicação |  |
| 4 | `ID_FONES_TIPOS` | INT | não |  | aplicação |  |
| 5 | `DDD` | VARCHAR(3) | não |  | aplicação |  |
| 6 | `FONE` | VARCHAR(20) | não |  | aplicação |  |
| 7 | `PRINCIPAL` | TINYINT(1) | não | 0 | aplicação |  |
| 8 | `CONTATO` | VARCHAR(100) | sim |  | aplicação |  |
| 9 | `DEPARTAMENTO` | VARCHAR(100) | sim |  | aplicação |  |
| 10 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 11 | `ID_LEGADO` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ENTIDADES_FONES`

- **Índice** `IX_ENTFON_ENT` (ID_ENTIDADES)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | CASCADE |
| `ID_FONES_TIPOS` | [`FONES_TIPOS`](../tabelas/FONES_TIPOS.md) | ID_FONES_TIPOS | bloqueia |

### É referenciada por
—

## Triggers
- [`TG_ENTFON_BI`](../triggers/TG_ENTFON_BI.md) — BEFORE INSERT: Telefone herda `ID_EMPRESAS` da entidade.

## Usada por
- **Views:** [`VW_CLIENTES`](../views/VW_CLIENTES.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md)
- **Processos:** —

## Regras de negócio
- Um só `PRINCIPAL` por entidade (ao marcar, desmarcar os outros no serviço).
- DDD 2–3 dígitos.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_ENTIDADES_FONES`.
- `TINYINT(1)` → `bool`: `PRINCIPAL`, `INATIVO`.
- `ID_EMPRESAS` herdado por `TG_ENTFON_BI`.

## Definição (DDL)
```sql
CREATE TABLE ENTIDADES_FONES (
  ID_ENTIDADES_FONES INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  ID_ENTIDADES       INT          NOT NULL,
  ID_FONES_TIPOS     INT          NOT NULL,
  DDD                VARCHAR(3)   NOT NULL,
  FONE               VARCHAR(20)  NOT NULL,
  PRINCIPAL          TINYINT(1)   NOT NULL DEFAULT 0,
  CONTATO            VARCHAR(100) NULL,
  DEPARTAMENTO       VARCHAR(100) NULL,
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL,
  INDEX IX_ENTFON_ENT (ID_ENTIDADES),
  CONSTRAINT FK_ENTFON_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_ENTFON_ENT FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES) ON DELETE CASCADE,
  CONSTRAINT FK_ENTFON_TIPO FOREIGN KEY (ID_FONES_TIPOS) REFERENCES FONES_TIPOS (ID_FONES_TIPOS)
) ENGINE=InnoDB;
```
