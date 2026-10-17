# FN_DATA_PARCIAL

Data parcial para exibição: `dd/mm/aaaa`, `mm/aaaa` ou `aaaa` (fabricação/validade).

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
concatenação na view (gerava "  /  2026")

Comentário do script: _FN_DATA_PARCIAL — "dd/mm/aaaa", "mm/aaaa" ou "aaaa" (validade/fabricação parciais) Corrige o legado, que gerava "  /  2026" com espaços sobrando._

## Assinatura
```sql
CREATE FUNCTION FN_DATA_PARCIAL(p_dia INT, p_mes INT, p_ano INT)
RETURNS VARCHAR(10) DETERMINISTIC NO SQL
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_dia` | INT |
| IN | `p_mes` | INT |
| IN | `p_ano` | INT |

## Quem chama
- **Uso previsto:** `VW_LAB_AMOSTRAS`, log da amostra, etiquetas, boletim.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_AMOSTRAS_AU`](../triggers/TG_AMOSTRAS_AU.md)
- **Usada em views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md)

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_DATA_PARCIAL({0}, {1}, {2}) AS Value", dia, mes, ano).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_DATA_PARCIAL(p_dia INT, p_mes INT, p_ano INT)
RETURNS VARCHAR(10) DETERMINISTIC NO SQL
BEGIN
  IF p_ano IS NULL THEN
    RETURN NULL;
  END IF;
  RETURN CONCAT(IF(p_dia IS NOT NULL AND p_mes IS NOT NULL, CONCAT(LPAD(p_dia, 2, '0'), '/'), ''),
                IF(p_mes IS NOT NULL, CONCAT(LPAD(p_mes, 2, '0'), '/'), ''),
                p_ano);
END
```
