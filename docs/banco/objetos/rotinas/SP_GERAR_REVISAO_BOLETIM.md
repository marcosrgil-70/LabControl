# SP_GERAR_REVISAO_BOLETIM

Gera revisão do boletim apontando para o resultado vigente.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_BOLETIM`

## Assinatura
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                          IN p_id_usuario INT, OUT p_id_novo INT, OUT p_revisao INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_func` | INT |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_novo` | INT |
| OUT | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** "Criar revisão" da T27 (`BOLETINS.REVISAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** —
- **Lê:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md), [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Variáveis de sessão:** `@LC_REVISAO` ()

## Mensagens de negócio (SQLSTATE 45000)
- "Informe o motivo da revisão."

## Processos
- [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_GERAR_REVISAO_BOLETIM";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_func", id_func));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
var o_id_novo = new MySqlParameter("p_id_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_novo);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_GERAR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                          IN p_id_usuario INT, OUT p_id_novo INT, OUT p_revisao INT)
BEGIN
  DECLARE v_res, v_res_ultimo INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  -- Se o resultado foi revisado depois do boletim, a revisão do boletim já aponta para o resultado vigente
  SELECT B.ID_LAB_RESULTADOS_ANALISES INTO v_res FROM LAB_BOLETIM_ANALISES B
   WHERE B.ID_LAB_BOLETIM_ANALISES = p_id_origem;
  SELECT R2.ID_LAB_RESULTADOS_ANALISES INTO v_res_ultimo
    FROM LAB_RESULTADOS_ANALISES R2
   WHERE R2.ID_LAB_HIST_AMOSTRAS = (SELECT ID_LAB_HIST_AMOSTRAS FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = v_res)
     AND R2.ULTIMA_REVISAO = 1
   LIMIT 1;
  CALL PR_REVISAO_BOLETIM(p_id_origem, IF(v_res_ultimo IS NOT NULL AND v_res_ultimo <> v_res, v_res_ultimo, NULL),
                          p_motivo, p_id_func, p_id_usuario, p_id_novo, p_revisao);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_novo),
                  'LAB_BOLETIM_ANALISES', p_id_novo, 'R', JSON_OBJECT('ID_ORIGEM', p_id_origem),
                  JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo));
  COMMIT;
END
```
