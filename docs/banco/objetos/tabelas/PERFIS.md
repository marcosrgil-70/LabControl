# PERFIS

Perfis de acesso por empresa (ex.: ADMINISTRADOR, Recepção, Analista).

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-09

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| — (o legado dava direito usuário × empresa × componente) | — |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PERFIS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `NOME` | VARCHAR(60) | não |  | aplicação |  |
| 4 | `DESCRICAO` | VARCHAR(200) | sim |  | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PERFIS`
- **Única** `UQ_PERFIS_NOME` (ID_EMPRESAS, NOME)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`PERFIS_PERMISSOES`](../tabelas/PERFIS_PERMISSOES.md) | ID_PERFIS | CASCADE |
| [`USUARIOS_PERFIS`](../tabelas/USUARIOS_PERFIS.md) | ID_PERFIS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T04 Perfis e permissões](../../../telas/T04-perfis-permissoes.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md) · [P23 Nova empresa e cópia de cadastros](../../../processos/P23-nova-empresa.md)

## Regras de negócio
- Nome único por empresa.
- O perfil ADMINISTRADOR criado por `SP_INICIALIZAR_EMPRESA` não pode ser excluído.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_PERFIS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE PERFIS (
  ID_PERFIS        INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  NOME             VARCHAR(60)  NOT NULL,
  DESCRICAO        VARCHAR(200) NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_PERFIS_NOME UNIQUE (ID_EMPRESAS, NOME),
  CONSTRAINT FK_PERFIS_EMPRESAS FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
