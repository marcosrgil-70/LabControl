# PERFIS_PERMISSOES

Permissões de cada perfil (matriz da tela de perfis).

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-09

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `SYS_*_ACESSOS` (3.690 linhas por componente) | — |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PERFIS` | INT | não |  | aplicação |  |
| 2 | `ID_PERMISSOES` | INT | não |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PERFIS`, `ID_PERMISSOES`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_PERFIS` | [`PERFIS`](../tabelas/PERFIS.md) | ID_PERFIS | CASCADE |
| `ID_PERMISSOES` | [`PERMISSOES`](../tabelas/PERMISSOES.md) | ID_PERMISSOES | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`PR_CRIAR_PERFIL`](../rotinas/PR_CRIAR_PERFIL.md), [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T04 Perfis e permissões](../../../telas/T04-perfis-permissoes.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Regras de negócio
- Excluir o perfil apaga as permissões (cascade).

## Implementação no C# (EF Core)
- —

## Definição (DDL)
```sql
CREATE TABLE PERFIS_PERMISSOES (
  ID_PERFIS        INT NOT NULL,
  ID_PERMISSOES    INT NOT NULL,
  PRIMARY KEY (ID_PERFIS, ID_PERMISSOES),
  CONSTRAINT FK_PERFPERM_PERFIS FOREIGN KEY (ID_PERFIS) REFERENCES PERFIS (ID_PERFIS) ON DELETE CASCADE,
  CONSTRAINT FK_PERFPERM_PERMISSOES FOREIGN KEY (ID_PERMISSOES) REFERENCES PERMISSOES (ID_PERMISSOES)
) ENGINE=InnoDB;
```
