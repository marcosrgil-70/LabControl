# PR_REVISAO_AMOSTRA

Núcleo da revisão de amostra: cópia completa (inclusive coletor), testes com `ID_TESTE_ORIGEM` e cascata resultado → boletim.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_AMOSTRA` (erros A1–A9)

Comentário do script: _PR_REVISAO_AMOSTRA (corrige SP_GERAR_REVISAO_AMOSTRA)_

## Assinatura
```sql
CREATE PROCEDURE PR_REVISAO_AMOSTRA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                    IN p_id_usuario INT, OUT p_id_nova INT, OUT p_revisao INT,
                                    OUT p_id_resultado_novo INT, OUT p_id_boletim_novo INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_func` | INT |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_nova` | INT |
| OUT | `p_revisao` | INT |
| OUT | `p_id_resultado_novo` | INT |
| OUT | `p_id_boletim_novo` | INT |

## Quem chama
- **Uso previsto:** `SP_GERAR_REVISAO_AMOSTRA`.
- **Chamada por rotinas:** [`SP_GERAR_REVISAO_AMOSTRA`](../rotinas/SP_GERAR_REVISAO_AMOSTRA.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md)
- **Lê:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Chama:** [`PR_REVISAO_RESULTADO`](../rotinas/PR_REVISAO_RESULTADO.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Amostra de origem não encontrada."
- "Só é possível revisar a última revisão da amostra."
- "Não existem resultados para esta amostra: altere a amostra diretamente."

## Processos
- [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_REVISAO_AMOSTRA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_func", id_func));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
var o_id_nova = new MySqlParameter("p_id_nova", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_nova);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
var o_id_resultado_novo = new MySqlParameter("p_id_resultado_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_resultado_novo);
var o_id_boletim_novo = new MySqlParameter("p_id_boletim_novo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_boletim_novo);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_REVISAO_AMOSTRA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                    IN p_id_usuario INT, OUT p_id_nova INT, OUT p_revisao INT,
                                    OUT p_id_resultado_novo INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE v_emp, v_ano, v_cod, v_raiz, v_ultima, v_max, v_res, v_rev_res INT;

  SELECT ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, COALESCE(ID_AMOSTRA_RAIZ, ID_LAB_HIST_AMOSTRAS), ULTIMA_REVISAO
    INTO v_emp, v_ano, v_cod, v_raiz, v_ultima
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra de origem não encontrada.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão da amostra.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não existem resultados para esta amostra: altere a amostra diretamente.';
  END IF;

  SELECT MAX(REVISAO) INTO v_max FROM LAB_HIST_AMOSTRAS
   WHERE ID_EMPRESAS = v_emp AND ANO_AMOSTRA = v_ano AND COD_AMOSTRA = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  -- Cópia COMPLETA (o legado esquecia COLETOR e DATAHORACOLETA — 61 revisões perderam o dado)
  INSERT INTO LAB_HIST_AMOSTRAS
        (ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, REVISAO, ID_AMOSTRAS_TIPO, ID_ANALISES_TIPO, NR_AMOSTRA,
         ID_AMOSTRA_ORIGEM, ID_AMOSTRA_RAIZ, ULTIMA_REVISAO, MOTIVO_REVISAO, TIPO_DOCUMENTO, ID_ENTIDADES,
         NOME_CONTATO, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_PEDIDOS_VENDA, ID_PRODUTOS, DT_ENTREGA,
         HR_ENTREGA, LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE, NR_LOTE,
         FABRICACAO_DIA, FABRICACAO_MES, FABRICACAO_ANO, VALIDADE_DIA, VALIDADE_MES, VALIDADE_ANO,
         DT_FABRICACAO, DT_VALIDADE, NOTA_ROTULO, DT_ENTREGA_VERIFICACAO, HR_ENTREGA_VERIFICACAO,
         ESPECIE_AMOSTRA, ASPECTO_AMOSTRA, COR, OUTRAS_CARACTERISTICAS, QTDE_AMOSTRA_VERIFICACAO,
         TEMPERATURA_VERIFICACAO, OBSERVACAO_VERIFICACAO, ACOMPANHA_FICHA_TECNICA, ACOMPANHA_PADRAO_ANALITICO,
         ACOMPANHA_CA_CLIENTE, ENVIAR_OUTRO_LABORATORIO, QTDE_ENVIO_OUTRO_LABORATORIO, ID_AMOSTRAS_STATUS,
         ID_ENTIDADES_FUNC_RESP, ID_ENTIDADES_FUNC_DIG, NOME_COLETOR, DT_HR_COLETA, TX_OBSERVACAO, CRIADO_POR)
  SELECT ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, p_revisao, ID_AMOSTRAS_TIPO, ID_ANALISES_TIPO, '',
         p_id_origem, v_raiz, 1, p_motivo, TIPO_DOCUMENTO, ID_ENTIDADES,
         NOME_CONTATO, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_PEDIDOS_VENDA, ID_PRODUTOS, DT_ENTREGA,
         HR_ENTREGA, LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE, NR_LOTE,
         FABRICACAO_DIA, FABRICACAO_MES, FABRICACAO_ANO, VALIDADE_DIA, VALIDADE_MES, VALIDADE_ANO,
         DT_FABRICACAO, DT_VALIDADE, NOTA_ROTULO, DT_ENTREGA_VERIFICACAO, HR_ENTREGA_VERIFICACAO,
         ESPECIE_AMOSTRA, ASPECTO_AMOSTRA, COR, OUTRAS_CARACTERISTICAS, QTDE_AMOSTRA_VERIFICACAO,
         TEMPERATURA_VERIFICACAO, OBSERVACAO_VERIFICACAO, ACOMPANHA_FICHA_TECNICA, ACOMPANHA_PADRAO_ANALITICO,
         ACOMPANHA_CA_CLIENTE, ENVIAR_OUTRO_LABORATORIO, QTDE_ENVIO_OUTRO_LABORATORIO, ID_AMOSTRAS_STATUS,
         ID_ENTIDADES_FUNC_RESP, p_id_func, NOME_COLETOR, DT_HR_COLETA, TX_OBSERVACAO, p_id_usuario
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem;
  SET p_id_nova = LAST_INSERT_ID();

  UPDATE LAB_HIST_AMOSTRAS SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND ANO_AMOSTRA = v_ano AND COD_AMOSTRA = v_cod
     AND ID_LAB_HIST_AMOSTRAS <> p_id_nova AND ULTIMA_REVISAO = 1;

  INSERT INTO LAB_HIST_AMOSTRAS_TESTES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_TIPO, ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, DT_PREVISTA, ID_ANALISES_STATUS,
         TX_ANALISES_TIPO, ID_TESTE_ORIGEM)
  SELECT ID_EMPRESAS, p_id_nova, ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_TIPO, ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, DT_PREVISTA, ID_ANALISES_STATUS,
         TX_ANALISES_TIPO, ID_LAB_HIST_AMOSTRAS_TESTES
    FROM LAB_HIST_AMOSTRAS_TESTES WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem
   ORDER BY ID_LAB_HIST_AMOSTRAS_TESTES;

  -- Leva a ÚLTIMA revisão do resultado para a nova amostra (e revisa o boletim)
  SELECT ID_LAB_RESULTADOS_ANALISES INTO v_res
    FROM LAB_RESULTADOS_ANALISES
   WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem AND ULTIMA_REVISAO = 1
   ORDER BY REVISAO DESC LIMIT 1;
  SET p_id_resultado_novo = NULL;
  SET p_id_boletim_novo = NULL;
  IF v_res IS NOT NULL THEN
    CALL PR_REVISAO_RESULTADO(v_res, p_id_nova, p_motivo, p_id_func, p_id_usuario, 1,
                              p_id_resultado_novo, v_rev_res, p_id_boletim_novo);
  END IF;
END
```
