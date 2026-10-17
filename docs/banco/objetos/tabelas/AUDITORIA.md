# AUDITORIA

Trilha de auditoria gravada pela aplicação (antes/depois em JSON).

**Grupo:** Núcleo global e segurança · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** —

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| — (só havia o log de amostras) | — |

Comentário do script: _Auditoria genérica gravada pela aplicação (antes/depois em JSON)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_AUDITORIA` | BIGINT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | sim |  | aplicação |  |
| 3 | `ID_USUARIOS` | INT | sim |  | aplicação |  |
| 4 | `TABELA` | VARCHAR(60) | não |  | aplicação |  |
| 5 | `ID_REGISTRO` | VARCHAR(60) | não |  | aplicação |  |
| 6 | `ACAO` | CHAR(1) | não |  | aplicação | I, A, E, R (revisão), M (emissão) |
| 7 | `DADOS_ANTES` | JSON | sim |  | aplicação |  |
| 8 | `DADOS_DEPOIS` | JSON | sim |  | aplicação |  |
| 9 | `IP` | VARCHAR(45) | sim |  | aplicação |  |
| 10 | `DT_EVENTO` | DATETIME(3) | não | CURRENT_TIMESTAMP(3) | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_AUDITORIA`

- **Índice** `IX_AUDITORIA_REG` (TABELA, ID_REGISTRO)
- **Índice** `IX_AUDITORIA_DATA` (ID_EMPRESAS, DT_EVENTO)

## Relacionamentos
### Referencia
—

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T06 Auditoria](../../../telas/T06-auditoria.md)
- **Processos:** [P21 Auditoria e log](../../../processos/P21-auditoria-log.md)

## Regras de negócio
- Ações: I, A, E, R (revisão), M (emissão) e L (login).
- Obrigatória em tabelas críticas (propostas, amostras, resultados, boletins, sequências, usuários/perfis).

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_AUDITORIA`.
- `JSON` → `string` (ou `JsonDocument`): `DADOS_ANTES`, `DADOS_DEPOIS`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DT_EVENTO` — mapear como gerado pelo banco.
- Gravar via `SaveChangesInterceptor` (ChangeTracker) + chamadas explícitas nas procedures de revisão/emissão.

## Definição (DDL)
```sql
CREATE TABLE AUDITORIA (
  ID_AUDITORIA     BIGINT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NULL,
  ID_USUARIOS      INT          NULL,
  TABELA           VARCHAR(60)  NOT NULL,
  ID_REGISTRO      VARCHAR(60)  NOT NULL,
  ACAO             CHAR(1)      NOT NULL COMMENT 'I, A, E, R (revisão), M (emissão)',
  DADOS_ANTES      JSON         NULL,
  DADOS_DEPOIS     JSON         NULL,
  IP               VARCHAR(45)  NULL,
  DT_EVENTO        DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  INDEX IX_AUDITORIA_REG (TABELA, ID_REGISTRO),
  INDEX IX_AUDITORIA_DATA (ID_EMPRESAS, DT_EVENTO)
) ENGINE=InnoDB;
```
