# PR_CONFERIR_EMAIL

Recusa (45000) um segundo e-mail principal na entidade.

**Tipo:** Procedure interna (PR_) · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
Delphi desmarcava os outros na tela

## Assinatura
```sql
CREATE PROCEDURE PR_CONFERIR_EMAIL(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id` | INT |
| IN | `p_entidade` | INT |
| IN | `p_principal` | TINYINT |

## Quem chama
- **Uso previsto:** `TG_ENTEML_BI/BU`; o `ContatosService` desmarca o anterior na mesma transação.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_ENTEML_BI`](../triggers/TG_ENTEML_BI.md), [`TG_ENTEML_BU`](../triggers/TG_ENTEML_BU.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** —
- **Lê:** [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Já existe um e-mail principal para este cadastro."

## Processos
- `P25-cadastro-entidades-contatos`

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CONFERIR_EMAIL";
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
CREATE PROCEDURE PR_CONFERIR_EMAIL(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
BEGIN
  IF p_principal = 1 AND EXISTS (SELECT 1 FROM ENTIDADES_EMAILS
                                  WHERE ID_ENTIDADES = p_entidade AND PRINCIPAL = 1 AND ID_ENTIDADES_EMAILS <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um e-mail principal para este cadastro.';
  END IF;
END
```
