# SP_PROXIMO_NUMERO

Numeração segura por (empresa, tipo, ano) com bloqueio de linha (`SELECT … FOR UPDATE`).

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GET_COD_AMOSTRA`, `SP_GET_COD_PROPOSTAS`, `SP_GET_COD_BOLETIM`, `SP_GET_ULT_PED_VENDA`, `SP_SYS_GERAR_CODIGO` (erros A1–A6 do doc 10 §4)

Comentário do script: _SP_PROXIMO_NUMERO — numeração segura (bloqueio de linha) por empresa/tipo/ano. Substitui SP_GET_COD_AMOSTRA, SP_GET_COD_PROPOSTAS, SP_GET_COD_BOLETIM, SP_GET_ULT_PED_VENDA e SP_SYS_GERAR_CODIGO. O ano NUNCA vem de data digitada: quem chama passa YEAR(CURDATE()) do servidor. Não abre transação (é chamada também por triggers)._

## Assinatura
```sql
CREATE PROCEDURE SP_PROXIMO_NUMERO(IN p_id_empresa INT, IN p_tipo VARCHAR(30), IN p_ano SMALLINT,
                                   OUT p_numero INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_empresa` | INT |
| IN | `p_tipo` | VARCHAR(30) |
| IN | `p_ano` | SMALLINT |
| OUT | `p_numero` | INT |

## Quem chama
- **Uso previsto:** Triggers (`TG_AMOSTRAS_BI`, `TG_PROPOSTAS_BI`, `TG_PEDVEN_BI`, `TG_PRODUTOS_BI`, `TG_RE_BI`). **Não chamar pela aplicação.**
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_PRODUTOS_BI`](../triggers/TG_PRODUTOS_BI.md), [`TG_PROPOSTAS_BI`](../triggers/TG_PROPOSTAS_BI.md), [`TG_PEDVEN_BI`](../triggers/TG_PEDVEN_BI.md), [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md), [`TG_RE_BI`](../triggers/TG_RE_BI.md)
- **Usada em views:** —

## Transação
Sem transação própria: o bloqueio dura até o COMMIT da transação de quem inseriu o documento.

## O que acessa
- **Altera:** [`SEQUENCIAS_DOCUMENTOS`](../tabelas/SEQUENCIAS_DOCUMENTOS.md)
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)
- [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_PROXIMO_NUMERO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_empresa", id_empresa));
cmd.Parameters.Add(new MySqlParameter("p_tipo", tipo));
cmd.Parameters.Add(new MySqlParameter("p_ano", ano));
var o_numero = new MySqlParameter("p_numero", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_numero);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_PROXIMO_NUMERO(IN p_id_empresa INT, IN p_tipo VARCHAR(30), IN p_ano SMALLINT,
                                   OUT p_numero INT)
BEGIN
  INSERT IGNORE INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO)
  VALUES (p_id_empresa, p_tipo, p_ano, 0);

  SELECT ULTIMO_NUMERO + 1 INTO p_numero
    FROM SEQUENCIAS_DOCUMENTOS
   WHERE ID_EMPRESAS = p_id_empresa AND TIPO_DOCUMENTO = p_tipo AND ANO = p_ano
   FOR UPDATE;

  UPDATE SEQUENCIAS_DOCUMENTOS
     SET ULTIMO_NUMERO = p_numero
   WHERE ID_EMPRESAS = p_id_empresa AND TIPO_DOCUMENTO = p_tipo AND ANO = p_ano;
END
```
