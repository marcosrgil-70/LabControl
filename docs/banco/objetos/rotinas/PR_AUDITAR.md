# PR_AUDITAR

Grava um evento em `AUDITORIA` (usuário `@LC_ID_USUARIOS`, IP `@LC_IP`) dentro da transação da procedure que a chama: revisão R, emissão M, desfazer revisão D, pedido I/A, troca de produto A.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _PR_AUDITAR — evento em AUDITORIA gravado pela própria procedure, dentro da transação dela (revisão R, emissão M, desfazer D, pedido I/A, troca de produto A). Usuário e IP vêm da sessão (@LC_ID_USUARIOS / @LC_IP, definidos pela aplicação)._

## Assinatura
```sql
CREATE PROCEDURE PR_AUDITAR(IN p_id_empresa INT, IN p_tabela VARCHAR(60), IN p_id_registro INT, IN p_acao CHAR(1),
                            IN p_antes JSON, IN p_depois JSON)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_empresa` | INT |
| IN | `p_tabela` | VARCHAR(60) |
| IN | `p_id_registro` | INT |
| IN | `p_acao` | CHAR(1) |
| IN | `p_antes` | JSON |
| IN | `p_depois` | JSON |

## Quem chama
- **Uso previsto:** As 10 `SP_*` com transação própria (antes do COMMIT). **Não chamar pela aplicação** (eventos do C#: `IAuditoria`).
- **Chamada por rotinas:** [`SP_GERAR_REVISAO_PROPOSTA`](../rotinas/SP_GERAR_REVISAO_PROPOSTA.md), [`SP_GERAR_REVISAO_AMOSTRA`](../rotinas/SP_GERAR_REVISAO_AMOSTRA.md), [`SP_GERAR_REVISAO_RESULTADO`](../rotinas/SP_GERAR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_RESULTADO`](../rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação (roda na transação de quem chama; ROLLBACK desfaz o evento junto).

## O que acessa
- **Altera:** [`AUDITORIA`](../tabelas/AUDITORIA.md)
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** `@LC_ID_USUARIOS` (usuário logado (gravado em logs); definido pelo interceptor de conexão a cada request); `@LC_IP` ()

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P21 Auditoria e log](../../../processos/P21-auditoria-log.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_AUDITAR";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_empresa", id_empresa));
cmd.Parameters.Add(new MySqlParameter("p_tabela", tabela));
cmd.Parameters.Add(new MySqlParameter("p_id_registro", id_registro));
cmd.Parameters.Add(new MySqlParameter("p_acao", acao));
cmd.Parameters.Add(new MySqlParameter("p_antes", antes));
cmd.Parameters.Add(new MySqlParameter("p_depois", depois));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_AUDITAR(IN p_id_empresa INT, IN p_tabela VARCHAR(60), IN p_id_registro INT, IN p_acao CHAR(1),
                            IN p_antes JSON, IN p_depois JSON)
BEGIN
  INSERT INTO AUDITORIA (ID_EMPRESAS, ID_USUARIOS, TABELA, ID_REGISTRO, ACAO, DADOS_ANTES, DADOS_DEPOIS, IP)
  VALUES (p_id_empresa, @LC_ID_USUARIOS, p_tabela, p_id_registro, p_acao, p_antes, p_depois, @LC_IP);
END
```
