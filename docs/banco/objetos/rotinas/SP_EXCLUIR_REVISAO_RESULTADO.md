# SP_EXCLUIR_REVISAO_RESULTADO

Desfaz a última revisão do resultado (sem boletim) e reativa a anterior.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
trigger `LAB_RESULTADOS_ANALISES_BD0` + `SP_ULT_REVISAO_RESULTADO`

Comentário do script: _Desfaz (exclui) a última revisão de um resultado ainda sem boletim, reativando a anterior._

## Assinatura
```sql
CREATE PROCEDURE SP_EXCLUIR_REVISAO_RESULTADO(IN p_id_resultado INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_resultado` | INT |

## Quem chama
- **Uso previsto:** "Desfazer última revisão" da T26 (`RESULTADOS.DESFAZER_REVISAO`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria**; usa `@LC_DESFAZENDO`.

## O que acessa
- **Altera:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Lê:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** —
- **Variáveis de sessão:** `@LC_DESFAZENDO` (uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão)

## Mensagens de negócio (SQLSTATE 45000)
- "Só é possível excluir a última revisão do resultado."
- "Resultado possui boletim: exclua/revise o boletim primeiro."

## Processos
- [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_EXCLUIR_REVISAO_RESULTADO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_resultado", id_resultado));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_EXCLUIR_REVISAO_RESULTADO(IN p_id_resultado INT)
BEGIN
  DECLARE v_origem, v_ultima INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_DESFAZENDO = NULL; ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_RESULTADO_ORIGEM, ULTIMA_REVISAO INTO v_origem, v_ultima
    FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado FOR UPDATE;
  IF v_ultima IS NULL OR v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível excluir a última revisão do resultado.';
  END IF;
  IF EXISTS (SELECT 1 FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado possui boletim: exclua/revise o boletim primeiro.';
  END IF;
  SET @LC_DESFAZENDO = 1;
  DELETE FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado;
  IF v_origem IS NOT NULL THEN
    UPDATE LAB_RESULTADOS_ANALISES SET ULTIMA_REVISAO = 1 WHERE ID_LAB_RESULTADOS_ANALISES = v_origem;
  END IF;
  SET @LC_DESFAZENDO = NULL;
  COMMIT;
END
```
