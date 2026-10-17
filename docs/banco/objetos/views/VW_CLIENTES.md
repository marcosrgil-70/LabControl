# VW_CLIENTES

Lista de clientes (entidades com `TIPO_CLIENTE = 1`) com telefone e e-mail principais.

**Tipo:** view · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
`VW_CLIENTES`, `VW_FORNECEDORES`, `VW_TRANSPORTADOR` (UNION PF/PJ repetido)

Comentário do script: _Clientes (VW_CLIENTES do legado, sem UNION PF/PJ)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `E.ID_EMPRESAS` |
| `ID_ENTIDADES` | `E.ID_ENTIDADES` |
| `CATEGORIA` | `E.CATEGORIA` |
| `CPF_CNPJ` | `E.CPF_CNPJ` |
| `NOME` | `E.NOME` |
| `NOME_FANTASIA` | `E.NOME_FANTASIA` |
| `RG_IE` | `E.RG_IE` |
| `INSC_MUNICIPAL` | `E.INSC_MUNICIPAL` |
| `INSC_SUFRAMA` | `E.INSC_SUFRAMA` |
| `DATA_CADASTRO` | `E.DATA_CADASTRO` |
| `INATIVO` | `E.INATIVO` |
| `FONE_PRINCIPAL` | `(SELECT CONCAT('(', F.DDD, ') ', F.FONE) FROM ENTIDADES_FONES F WHERE F.ID_ENTIDADES = E.ID_ENTIDADES AND F.INATIVO = 0 ORDER BY F.PRINCIPAL DESC, F.ID_ENTIDADE` |
| `EMAIL_PRINCIPAL` | `(SELECT M.EMAIL FROM ENTIDADES_EMAILS M WHERE M.ID_ENTIDADES = E.ID_ENTIDADES AND M.INATIVO = 0 ORDER BY M.PRINCIPAL DESC, M.ID_ENTIDADES_EMAILS LIMIT 1)` |

## Origem dos dados
- **Tabelas/views:** [`ENTIDADES`](../tabelas/ENTIDADES.md), [`ENTIDADES_FONES`](../tabelas/ENTIDADES_FONES.md), [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md)
- **Funções:** —

## Telas que usam
- [T12 Clientes/fornecedores](../../../telas/T12-clientes.md)
- [T20 Propostas](../../../telas/T20-propostas.md)
- [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md)
- [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- Filtrar `ID_EMPRESAS` e `INATIVO = 0` nos combos.

## Definição
```sql
CREATE OR REPLACE VIEW VW_CLIENTES AS
SELECT E.ID_EMPRESAS, E.ID_ENTIDADES, E.CATEGORIA, E.CPF_CNPJ, E.NOME, E.NOME_FANTASIA,
       E.RG_IE, E.INSC_MUNICIPAL, E.INSC_SUFRAMA, E.DATA_CADASTRO, E.INATIVO,
       (SELECT CONCAT('(', F.DDD, ') ', F.FONE) FROM ENTIDADES_FONES F
         WHERE F.ID_ENTIDADES = E.ID_ENTIDADES AND F.INATIVO = 0
         ORDER BY F.PRINCIPAL DESC, F.ID_ENTIDADES_FONES LIMIT 1) AS FONE_PRINCIPAL,
       (SELECT M.EMAIL FROM ENTIDADES_EMAILS M
         WHERE M.ID_ENTIDADES = E.ID_ENTIDADES AND M.INATIVO = 0
         ORDER BY M.PRINCIPAL DESC, M.ID_ENTIDADES_EMAILS LIMIT 1) AS EMAIL_PRINCIPAL
  FROM ENTIDADES E
 WHERE E.TIPO_CLIENTE = 1;
```
