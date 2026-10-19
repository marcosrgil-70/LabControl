# SP_TROCAR_PRODUTO_PROPOSTA

Troca um produto por outro na proposta e nas amostras **dessa** proposta.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_TROCAPRODUTO` (bug: alterava amostras de todas as propostas)

Comentário do script: _SP_TROCAR_PRODUTO_PROPOSTA — corrige SP_TROCAPRODUTO, cujo FOR SELECT percorria TODAS as amostras do banco (sem filtrar a proposta)._

## Assinatura
```sql
CREATE PROCEDURE SP_TROCAR_PRODUTO_PROPOSTA(IN p_id_proposta INT, IN p_id_prod_velho INT, IN p_id_prod_novo INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_proposta` | INT |
| IN | `p_id_prod_velho` | INT |
| IN | `p_id_prod_novo` | INT |

## Quem chama
- **Uso previsto:** Utilitário "Trocar produto" da T20 (`PROPOSTAS.ALTERAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Lê:** [`PRODUTOS`](../tabelas/PRODUTOS.md), [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Proposta não encontrada."
- "Produto novo não encontrado nesta empresa."

## Processos
- [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_TROCAR_PRODUTO_PROPOSTA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_proposta", id_proposta));
cmd.Parameters.Add(new MySqlParameter("p_id_prod_velho", id_prod_velho));
cmd.Parameters.Add(new MySqlParameter("p_id_prod_novo", id_prod_novo));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_TROCAR_PRODUTO_PROPOSTA(IN p_id_proposta INT, IN p_id_prod_velho INT, IN p_id_prod_novo INT)
BEGIN
  DECLARE v_emp INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta não encontrada.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM PRODUTOS WHERE ID_PRODUTOS = p_id_prod_novo AND ID_EMPRESAS = v_emp) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Produto novo não encontrado nesta empresa.';
  END IF;
  UPDATE LAB_PROPOSTAS_PRODUTOS SET ID_PRODUTOS = p_id_prod_novo
   WHERE ID_LAB_PROPOSTAS = p_id_proposta AND ID_PRODUTOS = p_id_prod_velho;
  UPDATE LAB_HIST_AMOSTRAS SET ID_PRODUTOS = p_id_prod_novo
   WHERE ID_LAB_PROPOSTAS = p_id_proposta AND ID_PRODUTOS = p_id_prod_velho;
  CALL PR_AUDITAR(v_emp, 'LAB_PROPOSTAS', p_id_proposta, 'A', JSON_OBJECT('ID_PRODUTOS', p_id_prod_velho),
                  JSON_OBJECT('ID_PRODUTOS', p_id_prod_novo));
  COMMIT;
END
```
