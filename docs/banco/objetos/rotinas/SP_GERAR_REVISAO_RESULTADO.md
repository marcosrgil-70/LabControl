# SP_GERAR_REVISAO_RESULTADO

Gera revisão do resultado (e do boletim, se `p_gerar_boletim = 1`).

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_RESULTADOS`

## Assinatura
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                            IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                            OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_func` | INT |
| IN | `p_id_usuario` | INT |
| IN | `p_gerar_boletim` | TINYINT |
| OUT | `p_id_novo` | INT |
| OUT | `p_revisao` | INT |
| OUT | `p_id_boletim_novo` | INT |

## Quem chama
- **Uso previsto:** "Criar revisão" da T26 (`RESULTADOS.REVISAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Informe o motivo da revisão."

## Processos
- [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_GERAR_REVISAO_RESULTADO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_func", id_func));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
cmd.Parameters.Add(new MySqlParameter("p_gerar_boletim", gerar_boletim));
var o_id_novo = new MySqlParameter("p_id_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_novo);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
var o_id_boletim_novo = new MySqlParameter("p_id_boletim_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_boletim_novo);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                            IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                            OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  CALL PR_REVISAO_RESULTADO(p_id_origem, NULL, p_motivo, p_id_func, p_id_usuario, p_gerar_boletim,
                            p_id_novo, p_revisao, p_id_boletim_novo);
  COMMIT;
END
```
