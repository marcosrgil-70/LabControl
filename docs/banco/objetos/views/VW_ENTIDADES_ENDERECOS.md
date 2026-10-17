# VW_ENTIDADES_ENDERECOS

Endereço completo e formatado em duas linhas (tipo, finalidade, cidade, UF, país).

**Tipo:** view · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
`VW_ENDERECOS`, `VW_CLIENTES_ENDERECOS`, `VW_DADOS_EMPRESAS`

Comentário do script: _Endereços completos (substitui VW_ENDERECOS e VW_CLIENTES_ENDERECOS)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `EN.ID_EMPRESAS` |
| `ID_ENTIDADES` | `EN.ID_ENTIDADES` |
| `ID_ENTIDADES_ENDERECOS` | `EN.ID_ENTIDADES_ENDERECOS` |
| `ID_ENDERECOS_TIPOS` | `EN.ID_ENDERECOS_TIPOS` |
| `ENDERECO_TIPO` | `T.DESCRICAO` |
| `FINALIDADE` | `T.FINALIDADE` |
| `TIPO_LOGRADOURO` | `TL.ABREVIATURA` |
| `LOGRADOURO` | `EN.LOGRADOURO` |
| `NUMERO` | `EN.NUMERO` |
| `COMPLEMENTO` | `EN.COMPLEMENTO` |
| `BAIRRO` | `EN.BAIRRO` |
| `CEP` | `EN.CEP` |
| `ID_CIDADES` | `C.ID_CIDADES` |
| `CIDADE` | `C.NOME` |
| `IBGE_CIDADE` | `C.CODIGO_IBGE` |
| `UF` | `UF.SIGLA` |
| `ESTADO` | `UF.NOME` |
| `PAIS` | `P.NOME` |
| `INATIVO` | `EN.INATIVO` |
| `ENDERECO_LINHA1` | `CONCAT_WS(', ', CONCAT_WS(' ', TL.ABREVIATURA, EN.LOGRADOURO), NULLIF(EN.NUMERO, ''), NULLIF(EN.COMPLEMENTO, ''))` |
| `ENDERECO_LINHA2` | `CONCAT_WS(' - ', NULLIF(EN.BAIRRO, ''), CONCAT(C.NOME, '/', UF.SIGLA), NULLIF(EN.CEP, ''))` |

## Origem dos dados
- **Tabelas/views:** [`PAISES`](../tabelas/PAISES.md), [`ESTADOS`](../tabelas/ESTADOS.md), [`CIDADES`](../tabelas/CIDADES.md), [`TIPOS_LOGRADOURO`](../tabelas/TIPOS_LOGRADOURO.md), [`ENDERECOS_TIPOS`](../tabelas/ENDERECOS_TIPOS.md), [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md)
- **Funções:** —

## Telas que usam
- [T12 Clientes/fornecedores](../../../telas/T12-clientes.md)
- [T20 Propostas](../../../telas/T20-propostas.md)
- [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- [T27 Boletim](../../../telas/T27-boletim.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- `ENDERECO_LINHA1/2` são usados nos PDFs (comprovante, proposta, boletim).

## Definição
```sql
CREATE OR REPLACE VIEW VW_ENTIDADES_ENDERECOS AS
SELECT EN.ID_EMPRESAS, EN.ID_ENTIDADES, EN.ID_ENTIDADES_ENDERECOS, EN.ID_ENDERECOS_TIPOS,
       T.DESCRICAO AS ENDERECO_TIPO, T.FINALIDADE, TL.ABREVIATURA AS TIPO_LOGRADOURO, EN.LOGRADOURO,
       EN.NUMERO, EN.COMPLEMENTO, EN.BAIRRO, EN.CEP, C.ID_CIDADES, C.NOME AS CIDADE, C.CODIGO_IBGE AS IBGE_CIDADE,
       UF.SIGLA AS UF, UF.NOME AS ESTADO, P.NOME AS PAIS, EN.INATIVO,
       CONCAT_WS(', ', CONCAT_WS(' ', TL.ABREVIATURA, EN.LOGRADOURO), NULLIF(EN.NUMERO, ''), NULLIF(EN.COMPLEMENTO, '')) AS ENDERECO_LINHA1,
       CONCAT_WS(' - ', NULLIF(EN.BAIRRO, ''), CONCAT(C.NOME, '/', UF.SIGLA), NULLIF(EN.CEP, '')) AS ENDERECO_LINHA2
  FROM ENTIDADES_ENDERECOS EN
  JOIN ENDERECOS_TIPOS T ON T.ID_ENDERECOS_TIPOS = EN.ID_ENDERECOS_TIPOS
  LEFT JOIN TIPOS_LOGRADOURO TL ON TL.ID_TIPOS_LOGRADOURO = EN.ID_TIPOS_LOGRADOURO
  JOIN CIDADES C ON C.ID_CIDADES = EN.ID_CIDADES
  JOIN ESTADOS UF ON UF.ID_ESTADOS = C.ID_ESTADOS
  JOIN PAISES P ON P.ID_PAISES = UF.ID_PAISES;
```
