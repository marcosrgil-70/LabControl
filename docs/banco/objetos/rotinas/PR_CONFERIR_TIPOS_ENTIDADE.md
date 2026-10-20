# PR_CONFERIR_TIPOS_ENTIDADE

Recusa (45000) entidade "empresa usuária" marcada também com outro tipo (cliente, fornecedor, funcionário, comercial, transportador).

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
un_Entidades (teEMP): "não pode coexistir com nenhum outro tipo"

Comentário do script: _PR_CONFERIR_TIPOS_ENTIDADE: a empresa usuária "não pode coexistir com nenhum outro tipo" (regra do legado, un_Entidades teEMP)._

## Assinatura
```sql
CREATE PROCEDURE PR_CONFERIR_TIPOS_ENTIDADE(IN p_empresa_usuaria TINYINT, IN p_outros INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_empresa_usuaria` | TINYINT |
| IN | `p_outros` | INT |

## Quem chama
- **Uso previsto:** `TG_ENT_BI/BU`.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_ENT_BI`](../triggers/TG_ENT_BI.md), [`TG_ENT_BU`](../triggers/TG_ENT_BU.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "A empresa usuária não pode ser também cliente, fornecedor, funcionário, comercial ou transportador."

## Processos
- `P25-cadastro-entidades-contatos`

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CONFERIR_TIPOS_ENTIDADE";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_empresa_usuaria", empresa_usuaria));
cmd.Parameters.Add(new MySqlParameter("p_outros", outros));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_CONFERIR_TIPOS_ENTIDADE(IN p_empresa_usuaria TINYINT, IN p_outros INT)
BEGIN
  IF p_empresa_usuaria = 1 AND p_outros > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A empresa usuária não pode ser também cliente, fornecedor, funcionário, comercial ou transportador.';
  END IF;
END
```
