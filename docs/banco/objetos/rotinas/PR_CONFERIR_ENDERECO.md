# PR_CONFERIR_ENDERECO

Recusa (45000) um segundo endereço **ativo** do mesmo tipo na entidade; endereço inativo (histórico) é aceito.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
Firebird: `UNIQUE (ID_ENTIDADES, ID_ENDERECOS_TIPOS)` (impedia guardar o antigo)

Comentário do script: _Contatos das entidades (clientes, fornecedores, funcionários e empresa usuária), chamadas pelas triggers BEFORE INSERT/UPDATE de ENTIDADES_ENDERECOS/FONES/EMAILS. PR_CONFERIR_ENDERECO: no máximo um endereço ATIVO por tipo na entidade (no Firebird era UNIQUE (entidade, tipo), que impedia guardar o endereço antigo inativo)._

## Assinatura
```sql
CREATE PROCEDURE PR_CONFERIR_ENDERECO(IN p_id INT, IN p_entidade INT, IN p_tipo INT, IN p_inativo TINYINT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id` | INT |
| IN | `p_entidade` | INT |
| IN | `p_tipo` | INT |
| IN | `p_inativo` | TINYINT |

## Quem chama
- **Uso previsto:** `TG_ENTEND_BI/BU` (fora da migração).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_ENTEND_BI`](../triggers/TG_ENTEND_BI.md), [`TG_ENTEND_BU`](../triggers/TG_ENTEND_BU.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** —
- **Lê:** [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Já existe um endereço ativo deste tipo para este cadastro. Inative o anterior ou altere-o."

## Processos
- `P25-cadastro-entidades-contatos`

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CONFERIR_ENDERECO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id", id));
cmd.Parameters.Add(new MySqlParameter("p_entidade", entidade));
cmd.Parameters.Add(new MySqlParameter("p_tipo", tipo));
cmd.Parameters.Add(new MySqlParameter("p_inativo", inativo));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_CONFERIR_ENDERECO(IN p_id INT, IN p_entidade INT, IN p_tipo INT, IN p_inativo TINYINT)
BEGIN
  IF p_inativo = 0 AND EXISTS (SELECT 1 FROM ENTIDADES_ENDERECOS
                                WHERE ID_ENTIDADES = p_entidade AND ID_ENDERECOS_TIPOS = p_tipo AND INATIVO = 0
                                  AND ID_ENTIDADES_ENDERECOS <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um endereço ativo deste tipo para este cadastro. Inative o anterior ou altere-o.';
  END IF;
END
```
