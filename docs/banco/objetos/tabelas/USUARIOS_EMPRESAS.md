# USUARIOS_EMPRESAS

Empresas que o usuário acessa e o **profissional** que ele representa em cada empresa (digitador/gerador dos registros).

**Grupo:** Núcleo global e segurança · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-09

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `USUARIO.ID_ENTIDADES_FUNC` + direitos de empresa em `SYS_MODULOS_ACESSOS` | — |

Comentário do script: _Empresas que o usuário acessa e o Profissional (funcionário) que ele representa em cada empresa. Este vínculo substitui USUARIO.ID_ENTIDADES_FUNC do Firebird._

Notas do script: _FK para ENTIDADES_FUNCIONARIOS criada no fim do script_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_USUARIOS` | INT | não |  | aplicação |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_ENTIDADES_FUNC` | INT | sim |  | aplicação | Profissional vinculado ao usuário nesta empresa |
| 4 | `PADRAO` | TINYINT(1) | não | 0 | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_USUARIOS`, `ID_EMPRESAS`
- **Única** `UQ_USUEMP_FUNC` (ID_EMPRESAS, ID_ENTIDADES_FUNC)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_USUARIOS` | [`USUARIOS`](../tabelas/USUARIOS.md) | ID_USUARIOS | bloqueia |
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_ENTIDADES_FUNC` | [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | bloqueia (FK adiada, fim do script) |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`USUARIOS_PERFIS`](../tabelas/USUARIOS_PERFIS.md) | ID_USUARIOS, ID_EMPRESAS | CASCADE |

## Triggers
- —

## Usada por
- **Views:** [`VW_PROFISSIONAIS`](../views/VW_PROFISSIONAIS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T02 Usuários](../../../telas/T02-usuarios.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T00 Login e seleção de empresa](../../../telas/T00-login-empresa.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Regras de negócio
- Um profissional só pode estar ligado a **um** usuário por empresa (`UQ_USUEMP_FUNC`).
- Sem profissional vinculado o usuário não recebe amostras, não lança resultados e não gera boletins (aviso no topo).
- `PADRAO` define a empresa pré-selecionada no login.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- `TINYINT(1)` → `bool`: `PADRAO`, `ATIVO`.
- O vínculo pode ser editado pela T02 (Usuários) e pela T13 (Profissionais) — mesma regra nos dois lugares (serviço único).

## Definição (DDL)
```sql
CREATE TABLE USUARIOS_EMPRESAS (
  ID_USUARIOS        INT          NOT NULL,
  ID_EMPRESAS        INT          NOT NULL,
  ID_ENTIDADES_FUNC  INT          NULL COMMENT 'Profissional vinculado ao usuário nesta empresa',
  PADRAO             TINYINT(1)   NOT NULL DEFAULT 0,
  ATIVO              TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (ID_USUARIOS, ID_EMPRESAS),
  CONSTRAINT UQ_USUEMP_FUNC UNIQUE (ID_EMPRESAS, ID_ENTIDADES_FUNC),
  CONSTRAINT FK_USUEMP_USUARIOS FOREIGN KEY (ID_USUARIOS) REFERENCES USUARIOS (ID_USUARIOS),
  CONSTRAINT FK_USUEMP_EMPRESAS FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
  -- FK para ENTIDADES_FUNCIONARIOS criada no fim do script
) ENGINE=InnoDB;
```
