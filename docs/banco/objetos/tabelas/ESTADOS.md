# ESTADOS

Unidades federativas (globais, códigos IBGE).

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-02

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ESTADOS` | 28 (seed v2: 27 UFs) |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ESTADOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_PAISES` | INT | não |  | aplicação |  |
| 3 | `NOME` | VARCHAR(100) | não |  | aplicação |  |
| 4 | `SIGLA` | CHAR(2) | não |  | aplicação |  |
| 5 | `CODIGO_IBGE` | VARCHAR(2) | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ESTADOS`
- **Única** `UQ_ESTADOS_SIGLA` (ID_PAISES, SIGLA)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_PAISES` | [`PAISES`](../tabelas/PAISES.md) | ID_PAISES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`CIDADES`](../tabelas/CIDADES.md) | ID_ESTADOS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_ENTIDADES_ENDERECOS`](../views/VW_ENTIDADES_ENDERECOS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T11-02 Estados](../../../telas/T11-02-estados.md)
- **Processos:** —

## Regras de negócio
- Sigla única por país (`UQ_ESTADOS_SIGLA`).
- Seed já traz as 27 UFs; a migração casa as UFs pela sigla.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_ESTADOS`.
- Entidade sem filtro de empresa.

## Definição (DDL)
```sql
CREATE TABLE ESTADOS (
  ID_ESTADOS       INT AUTO_INCREMENT PRIMARY KEY,
  ID_PAISES        INT          NOT NULL,
  NOME             VARCHAR(100) NOT NULL,
  SIGLA            CHAR(2)      NOT NULL,
  CODIGO_IBGE      VARCHAR(2)   NULL,
  CONSTRAINT UQ_ESTADOS_SIGLA UNIQUE (ID_PAISES, SIGLA),
  CONSTRAINT FK_ESTADOS_PAISES FOREIGN KEY (ID_PAISES) REFERENCES PAISES (ID_PAISES)
) ENGINE=InnoDB;
```
