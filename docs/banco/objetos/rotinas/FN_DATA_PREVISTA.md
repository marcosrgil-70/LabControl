# FN_DATA_PREVISTA

Data prevista do teste: entrega + `QTDE_DIAS` do prazo. `PRAZOS.DIAS_UTEIS = 1` conta só dias úteis (pula sábado e domingo; feriados não entram — não há cadastro de feriados); 0 = dias corridos, como o legado. Sem prazo: a própria data.

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
soma de dias corridos no Delphi (`Grava_Hist_Amostra_Testes`)

Comentário do script: _FN_DATA_PREVISTA — data prevista do teste: entrega + QTDE_DIAS do prazo. PRAZOS.DIAS_UTEIS = 1 conta só dias úteis (pula sábado e domingo; feriados não entram — não há cadastro de feriados); 0 = dias corridos, como o legado (decisão do usuário, 30/10/2026). Sem prazo: a própria data._

## Assinatura
```sql
CREATE FUNCTION FN_DATA_PREVISTA(p_data DATE, p_id_prazo INT)
RETURNS DATE READS SQL DATA
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_data` | DATE |
| IN | `p_id_prazo` | INT |

## Quem chama
- **Uso previsto:** `SP_AMOSTRA_GERAR_TESTES`, `TG_TESTES_BI` (quando `DT_PREVISTA` vem vazia).
- **Chamada por rotinas:** [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md)
- **Chamada por triggers:** [`TG_TESTES_BI`](../triggers/TG_TESTES_BI.md)
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** [`PRAZOS`](../tabelas/PRAZOS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_DATA_PREVISTA({0}, {1}) AS Value", data, id_prazo).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_DATA_PREVISTA(p_data DATE, p_id_prazo INT)
RETURNS DATE READS SQL DATA
BEGIN
  DECLARE v_dias INT DEFAULT 0;
  DECLARE v_uteis TINYINT DEFAULT 0;
  DECLARE v_dt DATE;
  IF p_data IS NULL THEN
    RETURN NULL;
  END IF;
  SELECT COALESCE(MAX(QTDE_DIAS), 0), COALESCE(MAX(DIAS_UTEIS), 0) INTO v_dias, v_uteis FROM PRAZOS WHERE ID_PRAZOS = p_id_prazo;
  IF v_uteis = 0 THEN
    RETURN DATE_ADD(p_data, INTERVAL v_dias DAY);
  END IF;
  SET v_dt = p_data;
  WHILE v_dias > 0 DO
    SET v_dt = DATE_ADD(v_dt, INTERVAL 1 DAY);
    IF DAYOFWEEK(v_dt) NOT IN (1, 7) THEN   -- 1 = domingo, 7 = sábado
      SET v_dias = v_dias - 1;
    END IF;
  END WHILE;
  RETURN v_dt;
END
```
