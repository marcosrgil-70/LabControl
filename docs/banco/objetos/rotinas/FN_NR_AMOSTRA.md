# FN_NR_AMOSTRA

Formata o número da amostra `TT-CCCCC-AA/YY[ Rnn]` a partir dos códigos.

**Tipo:** Função · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
expressão `LPAD(...)` repetida nas views/procedures

Comentário do script: _FN_NR_AMOSTRA — TT-CCCCC-AA/YY[ Rnn]  (TT/AA = CODIGO dos tipos)_

## Assinatura
```sql
CREATE FUNCTION FN_NR_AMOSTRA(p_cod_tipo_amostra INT, p_cod_amostra INT, p_cod_tipo_analise INT,
                              p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) DETERMINISTIC NO SQL
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_cod_tipo_amostra` | INT |
| IN | `p_cod_amostra` | INT |
| IN | `p_cod_tipo_analise` | INT |
| IN | `p_ano` | INT |
| IN | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** `FN_NR_AMOSTRA_ID`, migração.
- **Chamada por rotinas:** [`FN_NR_AMOSTRA_ID`](../rotinas/FN_NR_AMOSTRA_ID.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_NR_AMOSTRA({0}, {1}, {2}, {3}, {4}) AS Value", cod_tipo_amostra, cod_amostra, cod_tipo_analise, ano, revisao).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_NR_AMOSTRA(p_cod_tipo_amostra INT, p_cod_amostra INT, p_cod_tipo_analise INT,
                              p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) DETERMINISTIC NO SQL
BEGIN
  RETURN CONCAT(LPAD(COALESCE(p_cod_tipo_amostra, 0), 2, '0'), '-',
                LPAD(COALESCE(p_cod_amostra, 0), 5, '0'), '-',
                LPAD(COALESCE(p_cod_tipo_analise, 0), 2, '0'), '/',
                LPAD(MOD(COALESCE(p_ano, 0), 100), 2, '0'),
                IF(COALESCE(p_revisao, 0) > 0, CONCAT(' R', LPAD(p_revisao, 2, '0')), ''));
END
```
