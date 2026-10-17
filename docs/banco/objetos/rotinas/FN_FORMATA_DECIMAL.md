# FN_FORMATA_DECIMAL

Decimal → texto com vírgula, sem zeros à direita; 0/NULL → vazio.

**Tipo:** Função · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_FORMATDOUBLE`

Comentário do script: _FN_FORMATA_DECIMAL — equivalente corrigido de SP_FORMATDOUBLE. Remove zeros à direita e usa vírgula decimal. 0/NULL -> ''._

## Assinatura
```sql
CREATE FUNCTION FN_FORMATA_DECIMAL(p_valor DECIMAL(30,10))
RETURNS VARCHAR(40) DETERMINISTIC NO SQL
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_valor` | DECIMAL(30,10) |

## Quem chama
- **Uso previsto:** Views (`VW_LAB_RESULTADOS`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md)

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_FORMATA_DECIMAL({0}) AS Value", valor).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_FORMATA_DECIMAL(p_valor DECIMAL(30,10))
RETURNS VARCHAR(40) DETERMINISTIC NO SQL
BEGIN
  DECLARE v VARCHAR(40);
  IF p_valor IS NULL OR p_valor = 0 THEN
    RETURN '';
  END IF;
  SET v = CAST(p_valor AS CHAR);
  IF LOCATE('.', v) > 0 THEN
    SET v = TRIM(TRAILING '0' FROM v);
    SET v = TRIM(TRAILING '.' FROM v);
  END IF;
  RETURN REPLACE(v, '.', ',');
END
```
