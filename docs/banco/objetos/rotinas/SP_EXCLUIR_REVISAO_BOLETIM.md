# SP_EXCLUIR_REVISAO_BOLETIM

Desfaz a última revisão do boletim (não emitida) e reativa a anterior.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_ULT_REVISAO_BOLETIM` + trigger de exclusão

Comentário do script: _Desfaz (exclui) a última revisão de um boletim NÃO emitido, reativando a anterior._

## Assinatura
```sql
CREATE PROCEDURE SP_EXCLUIR_REVISAO_BOLETIM(IN p_id_boletim INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_boletim` | INT |

## Quem chama
- **Uso previsto:** "Desfazer revisão" da T27 (`BOLETINS.DESFAZER_REVISAO`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria**; usa `@LC_DESFAZENDO`.

## O que acessa
- **Altera:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Lê:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md), [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md)
- **Variáveis de sessão:** `@LC_DESFAZENDO` (uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão)

## Mensagens de negócio (SQLSTATE 45000)
- "Só é possível excluir a última revisão do boletim."
- "Este boletim foi emitido. Não é permitido alterar ou excluir."

## Processos
- [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_EXCLUIR_REVISAO_BOLETIM";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_boletim", id_boletim));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_EXCLUIR_REVISAO_BOLETIM(IN p_id_boletim INT)
BEGIN
  DECLARE v_origem, v_ultima, v_emp, v_pedido INT;
  DECLARE v_emissao DATETIME;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_DESFAZENDO = NULL; ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT B.ID_BOLETIM_ORIGEM, B.ULTIMA_REVISAO, B.DH_EMISSAO, B.ID_EMPRESAS, A.ID_PEDIDOS_VENDA
    INTO v_origem, v_ultima, v_emissao, v_emp, v_pedido
    FROM LAB_BOLETIM_ANALISES B JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = B.ID_LAB_HIST_AMOSTRAS
   WHERE B.ID_LAB_BOLETIM_ANALISES = p_id_boletim FOR UPDATE;
  IF v_ultima IS NULL OR v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível excluir a última revisão do boletim.';
  END IF;
  IF v_emissao IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este boletim foi emitido. Não é permitido alterar ou excluir.';
  END IF;
  SET @LC_DESFAZENDO = 1;
  DELETE FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim;
  IF v_origem IS NOT NULL THEN
    UPDATE LAB_BOLETIM_ANALISES SET ULTIMA_REVISAO = 1 WHERE ID_LAB_BOLETIM_ANALISES = v_origem;
  END IF;
  SET @LC_DESFAZENDO = NULL;
  CALL PR_AUDITAR(v_emp, 'LAB_BOLETIM_ANALISES', p_id_boletim, 'D', NULL, JSON_OBJECT('ID_REATIVADO', v_origem));
  CALL PR_ATUALIZAR_SITUACAO_PEDIDO(v_pedido);
  COMMIT;
END
```
