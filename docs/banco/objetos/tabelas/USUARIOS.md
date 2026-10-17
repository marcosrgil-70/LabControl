# USUARIOS

Usuários do sistema (login global). A senha é PBKDF2.

**Grupo:** Núcleo global e segurança · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-09

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `USUARIO` (`USUCOD`, `USUNOM`, `USUSEN` com hash proprietário, `USUADM`) | 15 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_USUARIOS` | INT | não |  | auto-incremento |  |
| 2 | `LOGIN` | VARCHAR(30) | não |  | aplicação |  |
| 3 | `NOME` | VARCHAR(100) | não |  | aplicação |  |
| 4 | `EMAIL` | VARCHAR(150) | sim |  | aplicação |  |
| 5 | `SENHA_HASH` | VARCHAR(255) | não |  | aplicação | PBKDF2 (ASP.NET Identity PasswordHasher) |
| 6 | `ADMIN_GLOBAL` | TINYINT(1) | não | 0 | aplicação | Acesso total a todas as empresas |
| 7 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |
| 8 | `TROCAR_SENHA` | TINYINT(1) | não | 1 | aplicação |  |
| 9 | `TENTATIVAS_FALHAS` | INT | não | 0 | aplicação |  |
| 10 | `BLOQUEADO_ATE` | DATETIME | sim |  | aplicação |  |
| 11 | `ULTIMO_ACESSO` | DATETIME | sim |  | aplicação |  |
| 12 | `ID_EMPRESAS_ULTIMA` | INT | sim |  | aplicação | Última empresa usada (pré-seleção no login) |
| 13 | `USUCOD_LEGADO` | INT | sim |  | aplicação | USUARIO.USUCOD do Firebird |
| 14 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 15 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_USUARIOS`
- **Única** `UQ_USUARIOS_LOGIN` (LOGIN)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS_ULTIMA` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia (FK adiada, fim do script) |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`USUARIOS_EMPRESAS`](../tabelas/USUARIOS_EMPRESAS.md) | ID_USUARIOS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_PROFISSIONAIS`](../views/VW_PROFISSIONAIS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T02 Usuários](../../../telas/T02-usuarios.md) · [T00 Login e seleção de empresa](../../../telas/T00-login-empresa.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Regras de negócio
- `LOGIN` único global, sem espaços, em maiúsculas.
- Bloqueio após 5 tentativas (`TENTATIVAS_FALHAS`, `BLOQUEADO_ATE` = agora + 15 min).
- Usuário migrado nasce com `TROCAR_SENHA = 1` e senha provisória (o hash do legado não é recuperável).
- `ADMIN_GLOBAL` ignora permissões e vê todas as empresas; só outro ADMIN_GLOBAL altera essa flag.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_USUARIOS`.
- `TINYINT(1)` → `bool`: `ADMIN_GLOBAL`, `ATIVO`, `TROCAR_SENHA`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- Usar `PasswordHasher<Usuario>`; nunca SHA-256 sem salt (erro da v1).
- Não excluir usuário com registros (`CRIADO_POR`): inativar.

## Definição (DDL)
```sql
CREATE TABLE USUARIOS (
  ID_USUARIOS        INT AUTO_INCREMENT PRIMARY KEY,
  LOGIN              VARCHAR(30)  NOT NULL,
  NOME               VARCHAR(100) NOT NULL,
  EMAIL              VARCHAR(150) NULL,
  SENHA_HASH         VARCHAR(255) NOT NULL COMMENT 'PBKDF2 (ASP.NET Identity PasswordHasher)',
  ADMIN_GLOBAL       TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'Acesso total a todas as empresas',
  ATIVO              TINYINT(1)   NOT NULL DEFAULT 1,
  TROCAR_SENHA       TINYINT(1)   NOT NULL DEFAULT 1,
  TENTATIVAS_FALHAS  INT          NOT NULL DEFAULT 0,
  BLOQUEADO_ATE      DATETIME     NULL,
  ULTIMO_ACESSO      DATETIME     NULL,
  ID_EMPRESAS_ULTIMA INT          NULL COMMENT 'Última empresa usada (pré-seleção no login)',
  USUCOD_LEGADO      INT          NULL COMMENT 'USUARIO.USUCOD do Firebird',
  CRIADO_EM          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ALTERADO_EM        DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT UQ_USUARIOS_LOGIN UNIQUE (LOGIN)
) ENGINE=InnoDB;
```
