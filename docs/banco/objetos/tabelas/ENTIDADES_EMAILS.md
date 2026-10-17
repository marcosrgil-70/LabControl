# ENTIDADES_EMAILS

E-mails da entidade — são também os **contatos** usados na proposta (contato comercial, do resultado e da NF).

**Grupo:** Cadastros · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-06

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENTIDADES_EMAILS` | 664 |

Comentário do script: _E-mails também são os CONTATOS usados na proposta (contato comercial, contato do resultado/laudo, contato da NF)._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENTIDADES_EMAILS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_ENTEML_BI |  |
| 3 | `ID_ENTIDADES` | INT | não |  | aplicação |  |
| 4 | `ID_EMAILS_TIPOS` | INT | sim |  | aplicação |  |
| 5 | `EMAIL` | VARCHAR(150) | não |  | aplicação |  |
| 6 | `NOME_CONTATO` | VARCHAR(100) | sim |  | aplicação |  |
| 7 | `DEPARTAMENTO` | VARCHAR(100) | sim |  | aplicação |  |
| 8 | `PRINCIPAL` | TINYINT(1) | não | 0 | aplicação |  |
| 9 | `ENVIAR_NFE` | TINYINT(1) | não | 0 | aplicação |  |
| 10 | `ENVIAR_LAUDO` | TINYINT(1) | não | 0 | aplicação |  |
| 11 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 12 | `ID_LEGADO` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ENTIDADES_EMAILS`
- **Única** `UQ_ENTEML` (ID_ENTIDADES, EMAIL)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | CASCADE |
| `ID_EMAILS_TIPOS` | [`EMAILS_TIPOS`](../tabelas/EMAILS_TIPOS.md) | ID_EMAILS_TIPOS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_CONTATO | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_CONTATO_RESULTADO | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_CONTATO_NF | bloqueia |

## Triggers
- [`TG_ENTEML_BI`](../triggers/TG_ENTEML_BI.md) — BEFORE INSERT: E-mail/contato herda `ID_EMPRESAS` da entidade.

## Usada por
- **Views:** [`VW_CLIENTES`](../views/VW_CLIENTES.md), [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md)

## Regras de negócio
- E-mail único por entidade (`UQ_ENTEML`).
- `ENVIAR_LAUDO` sugere o contato do resultado; `ENVIAR_NFE` sugere o contato da NF.
- Cliente sem e-mail não consegue ter proposta gravada (contatos obrigatórios).
- Os IDs são FKs da proposta: manter o mapa de IDs na migração (M-06).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_ENTIDADES_EMAILS`.
- `TINYINT(1)` → `bool`: `PRINCIPAL`, `ENVIAR_NFE`, `ENVIAR_LAUDO`, `INATIVO`.
- `ID_EMPRESAS` herdado por `TG_ENTEML_BI`.

## Definição (DDL)
```sql
CREATE TABLE ENTIDADES_EMAILS (
  ID_ENTIDADES_EMAILS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  ID_ENTIDADES       INT          NOT NULL,
  ID_EMAILS_TIPOS    INT          NULL,
  EMAIL              VARCHAR(150) NOT NULL,
  NOME_CONTATO       VARCHAR(100) NULL,
  DEPARTAMENTO       VARCHAR(100) NULL,
  PRINCIPAL          TINYINT(1)   NOT NULL DEFAULT 0,
  ENVIAR_NFE         TINYINT(1)   NOT NULL DEFAULT 0,
  ENVIAR_LAUDO       TINYINT(1)   NOT NULL DEFAULT 0,
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL,
  CONSTRAINT UQ_ENTEML UNIQUE (ID_ENTIDADES, EMAIL),
  CONSTRAINT FK_ENTEML_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_ENTEML_ENT FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES) ON DELETE CASCADE,
  CONSTRAINT FK_ENTEML_TIPO FOREIGN KEY (ID_EMAILS_TIPOS) REFERENCES EMAILS_TIPOS (ID_EMAILS_TIPOS)
) ENGINE=InnoDB;
```
