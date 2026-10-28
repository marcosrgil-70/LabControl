# PR_REVISAO_RESULTADO

Núcleo da revisão de resultado (na mesma amostra ou na amostra revisada), com mapeamento por `ID_TESTE_ORIGEM` e revisão opcional do boletim.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_RESULTADOS` (erros R1–R6)

Comentário do script: _PR_REVISAO_RESULTADO (corrige SP_GERAR_REVISAO_RESULTADOS)   p_id_amostra_destino NULL = mesma amostra (revisão só do resultado).   p_gerar_boletim = 1: se o resultado de origem tem boletim, revisa também o boletim._

## Assinatura
```sql
CREATE PROCEDURE PR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_id_amostra_destino INT, IN p_motivo VARCHAR(500),
                                      IN p_id_func INT, IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                      OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_id_amostra_destino` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_func` | INT |
| IN | `p_id_usuario` | INT |
| IN | `p_gerar_boletim` | TINYINT |
| OUT | `p_id_novo` | INT |
| OUT | `p_revisao` | INT |
| OUT | `p_id_boletim_novo` | INT |

## Quem chama
- **Uso previsto:** `SP_GERAR_REVISAO_RESULTADO`, `PR_REVISAO_AMOSTRA`.
- **Chamada por rotinas:** [`PR_REVISAO_AMOSTRA`](../rotinas/PR_REVISAO_AMOSTRA.md), [`SP_GERAR_REVISAO_RESULTADO`](../rotinas/SP_GERAR_REVISAO_RESULTADO.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md)
- **Lê:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Resultado de origem não encontrado."
- "Só é possível revisar a última revisão do resultado."
- "Amostra de destino não encontrada nesta empresa."
- "Há resultados cujo teste não existe na amostra de destino (revisão cancelada)."

## Processos
- [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md)
- [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_REVISAO_RESULTADO";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_id_amostra_destino", id_amostra_destino));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_func", id_func));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
cmd.Parameters.Add(new MySqlParameter("p_gerar_boletim", gerar_boletim));
var o_id_novo = new MySqlParameter("p_id_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_novo);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
var o_id_boletim_novo = new MySqlParameter("p_id_boletim_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_boletim_novo);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_id_amostra_destino INT, IN p_motivo VARCHAR(500),
                                      IN p_id_func INT, IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                      OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE v_emp, v_amo_old, v_dest, v_raiz, v_ultima, v_max, v_bol, v_rev_bol, v_faltam INT;

  SELECT R.ID_EMPRESAS, R.ID_LAB_HIST_AMOSTRAS, R.ULTIMA_REVISAO INTO v_emp, v_amo_old, v_ultima
    FROM LAB_RESULTADOS_ANALISES R WHERE R.ID_LAB_RESULTADOS_ANALISES = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado de origem não encontrado.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão do resultado.';
  END IF;

  SET v_dest = COALESCE(p_id_amostra_destino, v_amo_old);
  SELECT COALESCE(ID_AMOSTRA_RAIZ, ID_LAB_HIST_AMOSTRAS) INTO v_raiz
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = v_dest AND ID_EMPRESAS = v_emp;
  IF v_raiz IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra de destino não encontrada nesta empresa.';
  END IF;

  -- Numeração contínua por amostra física (todas as revisões da amostra). A trava fica só na linha da
  -- amostra raiz (fila entre revisões da mesma amostra); o filtro por COALESCE(...) = v_raiz não usa índice
  -- e o FOR UPDATE travava a tabela inteira de amostras (deadlock com a emissão de outro pedido — 6.3).
  SELECT ID_LAB_HIST_AMOSTRAS INTO v_raiz FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = v_raiz FOR UPDATE;
  SELECT MAX(R.REVISAO) INTO v_max
    FROM LAB_RESULTADOS_ANALISES R
    JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = R.ID_LAB_HIST_AMOSTRAS
   WHERE (A.ID_AMOSTRA_RAIZ = v_raiz OR A.ID_LAB_HIST_AMOSTRAS = v_raiz);
  SET p_revisao = COALESCE(v_max, -1) + 1;

  INSERT INTO LAB_RESULTADOS_ANALISES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, REVISAO, ID_RESULTADO_ORIGEM, ULTIMA_REVISAO, MOTIVO_REVISAO,
         ID_ENTIDADES_FUNC_DIG, CRIADO_POR)
  VALUES (v_emp, v_dest, p_revisao, p_id_origem, 1, p_motivo, p_id_func, p_id_usuario);
  SET p_id_novo = LAST_INSERT_ID();

  UPDATE LAB_RESULTADOS_ANALISES R
    JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = R.ID_LAB_HIST_AMOSTRAS
     SET R.ULTIMA_REVISAO = 0
   WHERE (A.ID_AMOSTRA_RAIZ = v_raiz OR A.ID_LAB_HIST_AMOSTRAS = v_raiz)
     AND R.ID_LAB_RESULTADOS_ANALISES <> p_id_novo AND R.ULTIMA_REVISAO = 1;

  -- Mapeamento de testes pela coluna ID_TESTE_ORIGEM (o legado casava por
  -- tipo de análise + parâmetro com SELECT singleton e quebrava quando a amostra
  -- tinha o mesmo parâmetro duas vezes — 18 amostras no banco atual).
  INSERT INTO LAB_RESULTADOS_PARAM
        (ID_EMPRESAS, ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRAS_TESTES, DT_INICIAL, DT_RESULTADO,
         ID_ENTIDADES_FUNC_ANALISE, ID_ENTIDADES_FUNC_DIG, QT_AMOSTRA, ID_UNIDADES_AMOSTRA, TIPO_RESULTADO,
         SIMBOLO_GRANDEZA, VR_RESULTADO, VR_EXP_RESULTADO, ID_UNIDADES, ID_LAB_TIPOS_RESULTADOS,
         TIPO_RESULTADO_ESPECIF, SIMBOLO_GR_ESPECIF, VR_RESULTADO_ESPECIF, VR_EXP_ESPECIF,
         ID_UNIDADES_ESPECIF, ID_LAB_TIPO_RESULTADOS_ESP, VR_SATISFEITO, ID_PARAM_ORIGEM)
  SELECT P.ID_EMPRESAS, p_id_novo, IF(v_dest = v_amo_old, P.ID_LAB_HIST_AMOSTRAS_TESTES, TN.ID_LAB_HIST_AMOSTRAS_TESTES),
         P.DT_INICIAL, P.DT_RESULTADO, P.ID_ENTIDADES_FUNC_ANALISE, P.ID_ENTIDADES_FUNC_DIG, P.QT_AMOSTRA,
         P.ID_UNIDADES_AMOSTRA, P.TIPO_RESULTADO, P.SIMBOLO_GRANDEZA, P.VR_RESULTADO, P.VR_EXP_RESULTADO,
         P.ID_UNIDADES, P.ID_LAB_TIPOS_RESULTADOS, P.TIPO_RESULTADO_ESPECIF, P.SIMBOLO_GR_ESPECIF,
         P.VR_RESULTADO_ESPECIF, P.VR_EXP_ESPECIF, P.ID_UNIDADES_ESPECIF, P.ID_LAB_TIPO_RESULTADOS_ESP,
         P.VR_SATISFEITO, P.ID_LAB_RESULTADOS_PARAM
    FROM LAB_RESULTADOS_PARAM P
    LEFT JOIN LAB_HIST_AMOSTRAS_TESTES TN ON TN.ID_LAB_HIST_AMOSTRAS = v_dest
                                         AND TN.ID_TESTE_ORIGEM = P.ID_LAB_HIST_AMOSTRAS_TESTES
   WHERE P.ID_LAB_RESULTADOS_ANALISES = p_id_origem
     AND (v_dest = v_amo_old OR TN.ID_LAB_HIST_AMOSTRAS_TESTES IS NOT NULL)
   ORDER BY P.ID_LAB_RESULTADOS_PARAM;

  -- avisa se algum resultado não pôde ser levado (teste removido na revisão da amostra)
  SELECT COUNT(*) INTO v_faltam
    FROM LAB_RESULTADOS_PARAM P
   WHERE P.ID_LAB_RESULTADOS_ANALISES = p_id_origem
     AND NOT EXISTS (SELECT 1 FROM LAB_RESULTADOS_PARAM N
                      WHERE N.ID_LAB_RESULTADOS_ANALISES = p_id_novo
                        AND N.ID_PARAM_ORIGEM = P.ID_LAB_RESULTADOS_PARAM);
  IF v_faltam > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Há resultados cujo teste não existe na amostra de destino (revisão cancelada).';
  END IF;

  SET p_id_boletim_novo = NULL;
  IF p_gerar_boletim = 1 THEN
    SELECT ID_LAB_BOLETIM_ANALISES INTO v_bol
      FROM LAB_BOLETIM_ANALISES
     WHERE ID_LAB_RESULTADOS_ANALISES = p_id_origem AND ULTIMA_REVISAO = 1
     ORDER BY REVISAO DESC LIMIT 1;
    IF v_bol IS NOT NULL THEN
      CALL PR_REVISAO_BOLETIM(v_bol, p_id_novo, p_motivo, p_id_func, p_id_usuario, p_id_boletim_novo, v_rev_bol);
    END IF;
  END IF;
END
```
