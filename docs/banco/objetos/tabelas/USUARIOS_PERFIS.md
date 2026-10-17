# USUARIOS_PERFIS

Perfis do usuário em cada empresa (as permissões efetivas são a união dos perfis).

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-09

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| — | — |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_USUARIOS` | INT | não |  | aplicação |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_PERFIS` | INT | não |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_USUARIOS`, `ID_EMPRESAS`, `ID_PERFIS`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_USUARIOS`, `ID_EMPRESAS` | [`USUARIOS_EMPRESAS`](../tabelas/USUARIOS_EMPRESAS.md) | ID_USUARIOS, ID_EMPRESAS | CASCADE |
| `ID_PERFIS` | [`PERFIS`](../tabelas/PERFIS.md) | ID_PERFIS | bloqueia |

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
- **Telas:** [T02 Usuários](../../../telas/T02-usuarios.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Regras de negócio
- FK composta para `USUARIOS_EMPRESAS`: só se dá perfil numa empresa à qual o usuário tem acesso.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Permissões carregadas como claims ao escolher a empresa; trocar perfil exige novo login/troca de empresa.

## Definição (DDL)
```sql
CREATE TABLE USUARIOS_PERFIS (
  ID_USUARIOS      INT NOT NULL,
  ID_EMPRESAS      INT NOT NULL,
  ID_PERFIS        INT NOT NULL,
  PRIMARY KEY (ID_USUARIOS, ID_EMPRESAS, ID_PERFIS),
  CONSTRAINT FK_USUPERF_USUEMP FOREIGN KEY (ID_USUARIOS, ID_EMPRESAS) REFERENCES USUARIOS_EMPRESAS (ID_USUARIOS, ID_EMPRESAS) ON DELETE CASCADE,
  CONSTRAINT FK_USUPERF_PERFIS FOREIGN KEY (ID_PERFIS) REFERENCES PERFIS (ID_PERFIS)
) ENGINE=InnoDB;
```
