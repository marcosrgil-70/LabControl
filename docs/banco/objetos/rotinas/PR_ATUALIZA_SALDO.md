# PR_ATUALIZA_SALDO

Recalcula o saldo físico da amostra (Σ entradas − Σ saídas).

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
trigger `TG_LAB_MOV_AMOSTRAS_AIUD`

Comentário do script: _PR_ATUALIZA_SALDO — recalcula o saldo físico da amostra (TG_LAB_MOV_AMOSTRAS_AIUD)_

## Assinatura
```sql
CREATE PROCEDURE PR_ATUALIZA_SALDO(IN p_id_amostra INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_amostra` | INT |

## Quem chama
- **Uso previsto:** Triggers `TG_MOV_AI/AU/AD`; migração (após M-15).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_MOV_AI`](../triggers/TG_MOV_AI.md), [`TG_MOV_AU`](../triggers/TG_MOV_AU.md), [`TG_MOV_AD`](../triggers/TG_MOV_AD.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`LAB_HIST_AMOSTRAS_SALDO`](../tabelas/LAB_HIST_AMOSTRAS_SALDO.md)
- **Lê:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_ATUALIZA_SALDO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_amostra", id_amostra));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_ATUALIZA_SALDO(IN p_id_amostra INT)
BEGIN
  DECLARE v_emp INT;
  DECLARE v_saldo DECIMAL(15,4);
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;
  IF v_emp IS NOT NULL THEN
    SELECT COALESCE(SUM(IF(E_S = 'E', QTDE, -QTDE)), 0) INTO v_saldo
      FROM LAB_MOV_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;
    INSERT INTO LAB_HIST_AMOSTRAS_SALDO (ID_LAB_HIST_AMOSTRAS, ID_EMPRESAS, SALDO_ATUAL, DATA_ATUALIZACAO)
    VALUES (p_id_amostra, v_emp, v_saldo, NOW())
    ON DUPLICATE KEY UPDATE SALDO_ATUAL = v_saldo, DATA_ATUALIZACAO = NOW();
  END IF;
END
```
