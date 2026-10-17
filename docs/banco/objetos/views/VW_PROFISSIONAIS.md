# VW_PROFISSIONAIS

Profissionais com registro completo, cargo, flags e usuário vinculado.

**Tipo:** view · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/04_views.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Legado
`VW_FUNCIONARIO` (exigia `ENTIDADES_PF` e cargo e escondia inativos)

Comentário do script: _Profissionais (VW_FUNCIONARIO do legado — que exigia ENTIDADES_PF e escondia inativos)_

## Colunas
| Coluna | Expressão |
|---|---|
| `ID_EMPRESAS` | `E.ID_EMPRESAS` |
| `ID_ENTIDADES` | `E.ID_ENTIDADES` |
| `NOME` | `E.NOME` |
| `SOBRENOME` | `E.NOME_FANTASIA` |
| `NOME_COMPLETO` | `TRIM(CONCAT(E.NOME, ' ', COALESCE(E.NOME_FANTASIA, '')))` |
| `NR_REGISTRO_PROFISSIONAL` | `F.NR_REGISTRO_PROFISSIONAL` |
| `UF_REGISTRO` | `F.UF_REGISTRO` |
| `ID_TIPOS_REG_PROFISSIONAL` | `F.ID_TIPOS_REG_PROFISSIONAL` |
| `TIPO_REGISTRO` | `R.DESCRICAO` |
| `REGISTRO_COMPLETO` | `CONCAT_WS(' ', R.DESCRICAO, CONCAT_WS('-', F.NR_REGISTRO_PROFISSIONAL, F.UF_REGISTRO))` |
| `ID_CARGOS_FUNCIONARIOS` | `F.ID_CARGOS_FUNCIONARIOS` |
| `CARGO` | `C.DESCRICAO` |
| `ASSINA_BOLETIM` | `F.ASSINA_BOLETIM` |
| `EXECUTA_ANALISE` | `F.EXECUTA_ANALISE` |
| `TEM_ASSINATURA` | `(F.ASSINATURA IS NOT NULL)` |
| `INATIVO` | `E.INATIVO` |
| `ID_USUARIOS` | `UE.ID_USUARIOS` |
| `LOGIN` | `U.LOGIN` |

## Origem dos dados
- **Tabelas/views:** [`USUARIOS`](../tabelas/USUARIOS.md), [`USUARIOS_EMPRESAS`](../tabelas/USUARIOS_EMPRESAS.md), [`TIPOS_REG_PROFISSIONAL`](../tabelas/TIPOS_REG_PROFISSIONAL.md), [`CARGOS_FUNCIONARIOS`](../tabelas/CARGOS_FUNCIONARIOS.md), [`ENTIDADES`](../tabelas/ENTIDADES.md), [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md)
- **Funções:** —

## Telas que usam
- [T13 Profissionais](../../../telas/T13-profissionais.md)
- [T02 Usuários](../../../telas/T02-usuarios.md)
- [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- [T26 Resultados](../../../telas/T26-resultados.md)
- [T27 Boletim](../../../telas/T27-boletim.md)

## Notas de uso
- Toda view tem `ID_EMPRESAS`: a consulta da aplicação **sempre** filtra a empresa ativa (views não passam pelo Global Query Filter se mapeadas como keyless sem filtro — configurar `HasQueryFilter` também nelas).
- Combos: responsável do recebimento = ativos; analista = `EXECUTA_ANALISE = 1`; responsável do boletim = `ASSINA_BOLETIM = 1`.

## Definição
```sql
CREATE OR REPLACE VIEW VW_PROFISSIONAIS AS
SELECT E.ID_EMPRESAS, E.ID_ENTIDADES, E.NOME, E.NOME_FANTASIA AS SOBRENOME,
       TRIM(CONCAT(E.NOME, ' ', COALESCE(E.NOME_FANTASIA, ''))) AS NOME_COMPLETO,
       F.NR_REGISTRO_PROFISSIONAL, F.UF_REGISTRO, F.ID_TIPOS_REG_PROFISSIONAL, R.DESCRICAO AS TIPO_REGISTRO,
       CONCAT_WS(' ', R.DESCRICAO, CONCAT_WS('-', F.NR_REGISTRO_PROFISSIONAL, F.UF_REGISTRO)) AS REGISTRO_COMPLETO,
       F.ID_CARGOS_FUNCIONARIOS, C.DESCRICAO AS CARGO, F.ASSINA_BOLETIM, F.EXECUTA_ANALISE,
       (F.ASSINATURA IS NOT NULL) AS TEM_ASSINATURA, E.INATIVO,
       UE.ID_USUARIOS, U.LOGIN
  FROM ENTIDADES_FUNCIONARIOS F
  JOIN ENTIDADES E ON E.ID_ENTIDADES = F.ID_ENTIDADES
  LEFT JOIN TIPOS_REG_PROFISSIONAL R ON R.ID_TIPOS_REG_PROFISSIONAL = F.ID_TIPOS_REG_PROFISSIONAL
  LEFT JOIN CARGOS_FUNCIONARIOS C ON C.ID_CARGOS_FUNCIONARIOS = F.ID_CARGOS_FUNCIONARIOS
  LEFT JOIN USUARIOS_EMPRESAS UE ON UE.ID_EMPRESAS = F.ID_EMPRESAS AND UE.ID_ENTIDADES_FUNC = F.ID_ENTIDADES
  LEFT JOIN USUARIOS U ON U.ID_USUARIOS = UE.ID_USUARIOS;
```
