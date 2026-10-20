# PR_CONFERIR_FONE

Recusa (45000) um segundo telefone principal na entidade.

**Tipo:** Procedure interna (PR_) · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
Delphi desmarcava os outros na tela

Comentário do script: _PR_CONFERIR_FONE / PR_CONFERIR_EMAIL: um só principal por entidade (a aplicação desmarca o anterior na mesma transação; o banco garante)._

## Assinatura
```sql
CREATE PROCEDURE PR_CONFERIR_FONE(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id` | INT |
| IN | `p_entidade` | INT |
| IN | `p_principal` | TINYINT |

## Quem chama
- **Uso previsto:** `TG_ENTFON_BI/BU`; o `ContatosService` desmarca o anterior na mesma transação.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_ENTFON_BI`](../triggers/TG_ENTFON_BI.md), [`TG_ENTFON_BU`](../triggers/TG_ENTFON_BU.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** —
- **Lê:** [`ENTIDADES_FONES`](../tabelas/ENTIDADES_FONES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Já existe um telefone principal para este cadastro."

## Processos
- `P25-cadastro-entidades-contatos`

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CONFERIR_FONE";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id", id));
cmd.Parameters.Add(new MySqlParameter("p_entidade", entidade));
cmd.Parameters.Add(new MySqlParameter("p_principal", principal));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_CONFERIR_FONE(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
BEGIN
  IF p_principal = 1 AND EXISTS (SELECT 1 FROM ENTIDADES_FONES
                                  WHERE ID_ENTIDADES = p_entidade AND PRINCIPAL = 1 AND ID_ENTIDADES_FONES <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um telefone principal para este cadastro.';
  END IF;
END
```
