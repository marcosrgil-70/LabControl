# ENDERECOS_TIPOS

Tipos de endereço com **finalidade** (Geral, NF, Correspondência, Entrega, Cobrança).

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENDERECOS_TIPOS` (IDs fixos no código: 2 = Correspondência) | 4 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENDERECOS_TIPOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(30) | não |  | aplicação |  |
| 4 | `FINALIDADE` | ENUM('GERAL','NOTA_FISCAL','CORRESPONDENCIA','ENTREGA','COBRANCA') | não | 'GERAL' | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ENDERECOS_TIPOS`
- **Única** `UQ_ENDTIP` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_ENDERECOS_TIPOS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_ENTIDADES_ENDERECOS`](../views/VW_ENTIDADES_ENDERECOS.md)
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-02 Tipos de endereço](../../../telas/T10-02-tipos-endereco.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- A finalidade substitui o ID fixo do legado: a proposta sugere o endereço do laudo (CORRESPONDENCIA) e o da cobrança (COBRANCA/NOTA_FISCAL).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_ENDERECOS_TIPOS`.
- `TINYINT(1)` → `bool`: `ATIVO`.
- `ENUM` → enum C# com conversão para string: `FINALIDADE` ('GERAL','NOTA_FISCAL','CORRESPONDENCIA','ENTREGA','COBRANCA').

## Definição (DDL)
```sql
CREATE TABLE ENDERECOS_TIPOS (
  ID_ENDERECOS_TIPOS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(30)  NOT NULL,
  FINALIDADE       ENUM('GERAL','NOTA_FISCAL','CORRESPONDENCIA','ENTREGA','COBRANCA') NOT NULL DEFAULT 'GERAL',
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_ENDTIP UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_ENDTIP_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
