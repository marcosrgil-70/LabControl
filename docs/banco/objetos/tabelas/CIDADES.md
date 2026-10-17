# CIDADES

Municípios (globais, código IBGE de 7 dígitos). Base do componente de endereço com busca por CEP.

**Grupo:** Núcleo global e segurança · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-02

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `CIDADES` (+ `CIDADES_BAIRROS`, `LOGRADOUROS` descontinuadas) | 190 (legado); recomendado importar os 5.570 do IBGE |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_CIDADES` | INT | não |  | auto-incremento |  |
| 2 | `ID_ESTADOS` | INT | não |  | aplicação |  |
| 3 | `NOME` | VARCHAR(150) | não |  | aplicação |  |
| 4 | `CODIGO_IBGE` | VARCHAR(7) | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_CIDADES`
- **Única** `UQ_CIDADES_IBGE` (CODIGO_IBGE)

- **Índice** `IX_CIDADES_NOME` (ID_ESTADOS, NOME)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_ESTADOS` | [`ESTADOS`](../tabelas/ESTADOS.md) | ID_ESTADOS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_CIDADES | bloqueia |
| [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_CIDADES | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_ENTIDADES_ENDERECOS`](../views/VW_ENTIDADES_ENDERECOS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T11-03 Cidades](../../../telas/T11-03-cidades.md) · [T11-04 Bairros e logradouros (descontinuados)](../../../telas/T11-04-bairros-logradouros.md)
- **Processos:** —

## Regras de negócio
- `CODIGO_IBGE` único — é por ele que o ViaCEP casa a cidade.
- Botão "Importar IBGE" (CSV) na tela de cidades.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_CIDADES`.
- Entidade sem filtro de empresa.

## Definição (DDL)
```sql
CREATE TABLE CIDADES (
  ID_CIDADES       INT AUTO_INCREMENT PRIMARY KEY,
  ID_ESTADOS       INT          NOT NULL,
  NOME             VARCHAR(150) NOT NULL,
  CODIGO_IBGE      VARCHAR(7)   NULL,
  CONSTRAINT UQ_CIDADES_IBGE UNIQUE (CODIGO_IBGE),
  INDEX IX_CIDADES_NOME (ID_ESTADOS, NOME),
  CONSTRAINT FK_CIDADES_ESTADOS FOREIGN KEY (ID_ESTADOS) REFERENCES ESTADOS (ID_ESTADOS)
) ENGINE=InnoDB;
```
