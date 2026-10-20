# PR_CRIAR_PERFIL

Cria um perfil sugerido da empresa com as permissões listadas (códigos ou TELA.*), só se ele ainda não existir — reinicializar não devolve permissão que a empresa retirou.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _PR_CRIAR_PERFIL — perfil sugerido da empresa (T04, dúvida 4 do plano), só se ainda não existir: rodar de novo SP_INICIALIZAR_EMPRESA não devolve permissões que a empresa tirou do perfil. p_permissoes = lista separada por vírgula de códigos (AMOSTRAS.INCLUIR) ou de telas inteiras (BOLETINS.*)._

## Assinatura
```sql
CREATE PROCEDURE PR_CRIAR_PERFIL(IN p_id_empresa INT, IN p_nome VARCHAR(60), IN p_descricao VARCHAR(200), IN p_permissoes TEXT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_empresa` | INT |
| IN | `p_nome` | VARCHAR(60) |
| IN | `p_descricao` | VARCHAR(200) |
| IN | `p_permissoes` | TEXT |

## Quem chama
- **Uso previsto:** `SP_INICIALIZAR_EMPRESA` (RECEPÇÃO, ANALISTA, RESPONSÁVEL TÉCNICO, COMERCIAL — T04).
- **Chamada por rotinas:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`PERFIS`](../tabelas/PERFIS.md), [`PERFIS_PERMISSOES`](../tabelas/PERFIS_PERMISSOES.md)
- **Lê:** [`PERMISSOES`](../tabelas/PERMISSOES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CRIAR_PERFIL";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_empresa", id_empresa));
cmd.Parameters.Add(new MySqlParameter("p_nome", nome));
cmd.Parameters.Add(new MySqlParameter("p_descricao", descricao));
cmd.Parameters.Add(new MySqlParameter("p_permissoes", permissoes));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_CRIAR_PERFIL(IN p_id_empresa INT, IN p_nome VARCHAR(60), IN p_descricao VARCHAR(200), IN p_permissoes TEXT)
BEGIN
  DECLARE v_perfil INT;
  IF NOT EXISTS (SELECT 1 FROM PERFIS WHERE ID_EMPRESAS = p_id_empresa AND NOME = p_nome) THEN
    INSERT INTO PERFIS (ID_EMPRESAS, NOME, DESCRICAO) VALUES (p_id_empresa, p_nome, p_descricao);
    SET v_perfil = LAST_INSERT_ID();
    INSERT INTO PERFIS_PERMISSOES (ID_PERFIS, ID_PERMISSOES)
    SELECT v_perfil, ID_PERMISSOES FROM PERMISSOES
     WHERE FIND_IN_SET(CODIGO, REPLACE(p_permissoes, ' ', '')) > 0
        OR FIND_IN_SET(CONCAT(TELA, '.*'), REPLACE(p_permissoes, ' ', '')) > 0;
  END IF;
END
```
