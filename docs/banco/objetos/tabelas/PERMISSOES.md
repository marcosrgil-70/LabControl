# PERMISSOES

Catálogo global de permissões `TELA.ACAO` (carregado pelo `05_seed.sql`).

**Grupo:** Núcleo global e segurança · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** — (seed)

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `SYS_MODULOS/SYS_MENUS/SYS_FORMS/SYS_COMPONENTES` (catálogo de telas e componentes) | 109 (seed) |

Comentário do script: _Catálogo global de permissões (tela + ação). Ex.: AMOSTRAS.INCLUIR_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PERMISSOES` | INT | não |  | auto-incremento |  |
| 2 | `CODIGO` | VARCHAR(60) | não |  | aplicação |  |
| 3 | `MODULO` | VARCHAR(40) | não |  | aplicação |  |
| 4 | `TELA` | VARCHAR(60) | não |  | aplicação |  |
| 5 | `ACAO` | VARCHAR(30) | não |  | aplicação |  |
| 6 | `DESCRICAO` | VARCHAR(150) | não |  | aplicação |  |
| 7 | `ORDEM` | INT | não | 0 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PERMISSOES`
- **Única** `UQ_PERMISSOES_COD` (CODIGO)

## Relacionamentos
### Referencia
—

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`PERFIS_PERMISSOES`](../tabelas/PERFIS_PERMISSOES.md) | ID_PERMISSOES | bloqueia |

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`PR_CRIAR_PERFIL`](../rotinas/PR_CRIAR_PERFIL.md), [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T04 Perfis e permissões](../../../telas/T04-perfis-permissoes.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Regras de negócio
- Só o seed cria permissões; nova tela = nova linha no `05_seed.sql`.
- Ações padrão: CONSULTAR, INCLUIR, ALTERAR, EXCLUIR, IMPRIMIR; especiais: REVISAR, EMITIR, GERAR, CANCELAR, ENTREGA_RETROATIVA, DESFAZER_REVISAO.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_PERMISSOES`.
- Constantes C# (`Permissoes.Amostras.Incluir = "AMOSTRAS.INCLUIR"`) espelhando o seed.

## Definição (DDL)
```sql
CREATE TABLE PERMISSOES (
  ID_PERMISSOES    INT AUTO_INCREMENT PRIMARY KEY,
  CODIGO           VARCHAR(60)  NOT NULL,
  MODULO           VARCHAR(40)  NOT NULL,
  TELA             VARCHAR(60)  NOT NULL,
  ACAO             VARCHAR(30)  NOT NULL,
  DESCRICAO        VARCHAR(150) NOT NULL,
  ORDEM            INT          NOT NULL DEFAULT 0,
  CONSTRAINT UQ_PERMISSOES_COD UNIQUE (CODIGO)
) ENGINE=InnoDB;
```
