# SP_GERAR_REVISAO_PROPOSTA

Gera revisão da proposta (motivo obrigatório).

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_PROPOSTAS`

Comentário do script: _REVISÕES — rotinas públicas (com transação)_

## Assinatura
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                           OUT p_id_nova INT, OUT p_revisao INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_nova` | INT |
| OUT | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** Botão "Gerar revisão" da T20 (`PROPOSTAS.REVISAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_PROPOSTAS_BI`](../triggers/TG_PROPOSTAS_BI.md)
- **Usada em views:** —

## Transação
**Com transação própria** (ROLLBACK + RESIGNAL). Não chamar dentro de transação do EF.

## O que acessa
- **Altera:** —
- **Lê:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md), [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md)
- **Variáveis de sessão:** `@LC_REVISAO` ()

## Mensagens de negócio (SQLSTATE 45000)
- "Informe o motivo da revisão."

## Processos
- [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_GERAR_REVISAO_PROPOSTA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
var o_id_nova = new MySqlParameter("p_id_nova", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_nova);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                           OUT p_id_nova INT, OUT p_revisao INT)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  CALL PR_REVISAO_PROPOSTA(p_id_origem, p_motivo, p_id_usuario, p_id_nova, p_revisao);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_nova), 'LAB_PROPOSTAS', p_id_nova, 'R',
                  JSON_OBJECT('ID_ORIGEM', p_id_origem), JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo));
  COMMIT;
END
```
