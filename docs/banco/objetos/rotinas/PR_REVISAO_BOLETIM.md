# PR_REVISAO_BOLETIM

Núcleo da revisão de boletim: nova revisão não emitida, apontando para o resultado novo/vigente, só com os parâmetros escolhidos e metodologias recalculadas.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_BOLETIM` (erros B1–B9)

Comentário do script: _PR_REVISAO_BOLETIM (corrige SP_GERAR_REVISAO_BOLETIM)   p_id_resultado_novo NULL = revisão só do boletim (mesmo resultado)._

## Assinatura
```sql
CREATE PROCEDURE PR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_id_resultado_novo INT, IN p_motivo VARCHAR(500),
                                    IN p_id_func INT, IN p_id_usuario INT,
                                    OUT p_id_novo INT, OUT p_revisao INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_id_resultado_novo` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_func` | INT |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_novo` | INT |
| OUT | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** `SP_GERAR_REVISAO_BOLETIM`, `PR_REVISAO_RESULTADO`.
- **Chamada por rotinas:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md), [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md)
- **Lê:** [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md), [`UNIDADES`](../tabelas/UNIDADES.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md)
- **Chama:** [`FN_OBS_METODOS`](../rotinas/FN_OBS_METODOS.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Boletim de origem não encontrado."
- "Só é possível revisar a última revisão do boletim."
- "Resultado do boletim não encontrado nesta empresa."

## Processos
- [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_REVISAO_BOLETIM";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_id_resultado_novo", id_resultado_novo));
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
CREATE PROCEDURE PR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_id_resultado_novo INT, IN p_motivo VARCHAR(500),
                                    IN p_id_func INT, IN p_id_usuario INT,
                                    OUT p_id_novo INT, OUT p_revisao INT)
BEGIN
  DECLARE v_emp, v_res_old, v_res, v_amostra, v_ultima, v_max, v_status_ini INT;
  DECLARE v_cod VARCHAR(30);

  SELECT ID_EMPRESAS, ID_LAB_RESULTADOS_ANALISES, COD_BOLETIM, ULTIMA_REVISAO
    INTO v_emp, v_res_old, v_cod, v_ultima
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Boletim de origem não encontrado.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão do boletim.';
  END IF;

  SET v_res = COALESCE(p_id_resultado_novo, v_res_old);
  SELECT ID_LAB_HIST_AMOSTRAS INTO v_amostra FROM LAB_RESULTADOS_ANALISES
   WHERE ID_LAB_RESULTADOS_ANALISES = v_res AND ID_EMPRESAS = v_emp;
  IF v_amostra IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado do boletim não encontrado nesta empresa.';
  END IF;

  SELECT MAX(REVISAO) INTO v_max FROM LAB_BOLETIM_ANALISES
   WHERE ID_EMPRESAS = v_emp AND COD_BOLETIM = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  SELECT ID_BOLETINS_STATUS_INICIAL INTO v_status_ini FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = v_emp;

  -- Nova revisão nasce NÃO emitida, com datas atuais e apontando para o resultado NOVO
  INSERT INTO LAB_BOLETIM_ANALISES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_LAB_RESULTADOS_ANALISES, COD_BOLETIM, REVISAO,
         ID_BOLETIM_ORIGEM, ULTIMA_REVISAO, MOTIVO_REVISAO, ID_BOLETINS_STATUS, DT_BOLETIM, DH_GERACAO,
         DH_EMISSAO, ID_ENTIDADES_FUNC_GER, ID_ENTIDADES_FUNC_EMI, ID_ENTIDADES_FUNC_RESP,
         ID_ENTIDADES_FUNC_RESP2, TXT_OBSERVACAO, TXT_LEGENDA, TXT_METODOLOGIA_UTIL,
         TXT_METODOLOGIA_ACRED, CRIADO_POR)
  SELECT ID_EMPRESAS, v_amostra, v_res, COD_BOLETIM, p_revisao,
         p_id_origem, 1, p_motivo, COALESCE(v_status_ini, ID_BOLETINS_STATUS), CURDATE(), NOW(),
         NULL, p_id_func, NULL, ID_ENTIDADES_FUNC_RESP,
         ID_ENTIDADES_FUNC_RESP2, TXT_OBSERVACAO, TXT_LEGENDA,
         COALESCE(FN_OBS_METODOS(v_amostra, 0), TXT_METODOLOGIA_UTIL),
         COALESCE(FN_OBS_METODOS(v_amostra, 1), TXT_METODOLOGIA_ACRED), p_id_usuario
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_origem;
  SET p_id_novo = LAST_INSERT_ID();

  UPDATE LAB_BOLETIM_ANALISES SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND COD_BOLETIM = v_cod
     AND ID_LAB_BOLETIM_ANALISES <> p_id_novo AND ULTIMA_REVISAO = 1;

  -- Copia SOMENTE os parâmetros escolhidos no boletim de origem (o legado copiava todos
  -- os parâmetros do resultado antigo), mapeando para as linhas do resultado novo e
  -- regravando o texto a partir dos valores atuais.
  INSERT INTO LAB_BOLETIM_PARAMETROS
        (ID_EMPRESAS, ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADOS_PARAM, ORDEM, DESC_PARAMETRO,
         RESULTADO_CONCAT, TIPO_RESULTADO_DESC, UNIDADE_SG, ESPECIFICACAO, ACREDITADO)
  SELECT v_emp, p_id_novo, RP.ID_LAB_RESULTADOS_PARAM, BP.ORDEM, PA.DESC_REDUZIDA,
         IF(RP.TIPO_RESULTADO = 1, RP.RESULTADO_FORMATADO, NULL),
         IF(RP.TIPO_RESULTADO = 0, RP.RESULTADO_FORMATADO, NULL),
         U.SIGLA, RP.ESPECIF_FORMATADA, PA.AUDITADO
    FROM LAB_BOLETIM_PARAMETROS BP
    JOIN LAB_RESULTADOS_PARAM RP_OLD ON RP_OLD.ID_LAB_RESULTADOS_PARAM = BP.ID_LAB_RESULTADOS_PARAM
    JOIN LAB_RESULTADOS_PARAM RP ON RP.ID_LAB_RESULTADOS_ANALISES = v_res
                                AND (   (v_res = v_res_old AND RP.ID_LAB_RESULTADOS_PARAM = RP_OLD.ID_LAB_RESULTADOS_PARAM)
                                     OR (v_res <> v_res_old AND RP.ID_PARAM_ORIGEM = RP_OLD.ID_LAB_RESULTADOS_PARAM))
    JOIN LAB_HIST_AMOSTRAS_TESTES T ON T.ID_LAB_HIST_AMOSTRAS_TESTES = RP.ID_LAB_HIST_AMOSTRAS_TESTES
    JOIN LAB_PARAMETROS_ANALISES PA ON PA.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
    LEFT JOIN UNIDADES U ON U.ID_UNIDADES = RP.ID_UNIDADES
   WHERE BP.ID_LAB_BOLETIM_ANALISES = p_id_origem
   ORDER BY BP.ORDEM, BP.ID_LAB_BOLETIM_PARAMETROS;
END
```
