# PAISES

Países (dado global, igual para todas as empresas).

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-02

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `PAISES` | 1 |

Comentário do script: _1. NÚCLEO GLOBAL: EMPRESAS, GEOGRAFIA (IBGE), USUÁRIOS E PERMISSÕES_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PAISES` | INT | não |  | auto-incremento |  |
| 2 | `NOME` | VARCHAR(80) | não |  | aplicação |  |
| 3 | `SIGLA` | VARCHAR(5) | sim |  | aplicação |  |
| 4 | `CODIGO_BACEN` | VARCHAR(10) | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PAISES`
- **Única** `UQ_PAISES_NOME` (NOME)

## Relacionamentos
### Referencia
—

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`ESTADOS`](../tabelas/ESTADOS.md) | ID_PAISES | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_ENTIDADES_ENDERECOS`](../views/VW_ENTIDADES_ENDERECOS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T11-01 Países](../../../telas/T11-01-paises.md)
- **Processos:** —

## Regras de negócio
- Global (sem `ID_EMPRESAS`). Só ADMIN_GLOBAL inclui/altera.
- Seed: BRASIL com `ID_PAISES = 1` e código BACEN 1058.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_PAISES`.
- Entidade sem filtro de empresa.

## Definição (DDL)
```sql
CREATE TABLE PAISES (
  ID_PAISES        INT AUTO_INCREMENT PRIMARY KEY,
  NOME             VARCHAR(80)  NOT NULL,
  SIGLA            VARCHAR(5)   NULL,
  CODIGO_BACEN     VARCHAR(10)  NULL,
  CONSTRAINT UQ_PAISES_NOME UNIQUE (NOME)
) ENGINE=InnoDB;
```
