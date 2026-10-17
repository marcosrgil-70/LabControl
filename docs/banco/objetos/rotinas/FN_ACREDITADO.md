# FN_ACREDITADO

0 = nenhum parâmetro acreditado, 1 = todos, 2 = misto, NULL = sem testes.

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`FN_ACREDITADO`

Comentário do script: _FN_ACREDITADO — 0 = nenhum parâmetro acreditado, 1 = todos, 2 = misto, NULL = amostra sem testes. (Firebird: FN_ACREDITADO; retornava NULL também quando não havia testes, mas a view fazia INNER JOIN e duplicava linhas.)_

## Assinatura
```sql
CREATE FUNCTION FN_ACREDITADO(p_id_amostra INT)
RETURNS TINYINT READS SQL DATA
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_amostra` | INT |

## Quem chama
- **Uso previsto:** `VW_LAB_AMOSTRAS` (`TIPO_ACREDITACAO`); cabeçalho do boletim (nº de acreditação).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md)

## O que acessa
- **Altera:** —
- **Lê:** [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_ACREDITADO({0}) AS Value", id_amostra).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_ACREDITADO(p_id_amostra INT)
RETURNS TINYINT READS SQL DATA
BEGIN
  DECLARE v_ok INT DEFAULT 0;
  DECLARE v_nok INT DEFAULT 0;
  SELECT COALESCE(SUM(P.AUDITADO = 1), 0), COALESCE(SUM(P.AUDITADO = 0), 0)
    INTO v_ok, v_nok
    FROM LAB_HIST_AMOSTRAS_TESTES T
    JOIN LAB_PARAMETROS_ANALISES P ON P.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
   WHERE T.ID_LAB_HIST_AMOSTRAS = p_id_amostra;
  IF v_ok = 0 AND v_nok = 0 THEN RETURN NULL; END IF;
  IF v_ok = 0 THEN RETURN 0; END IF;
  IF v_nok = 0 THEN RETURN 1; END IF;
  RETURN 2;
END
```
