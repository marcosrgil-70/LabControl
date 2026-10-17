# PR_REVISAO_PROPOSTA

Núcleo da revisão de proposta: copia cabeçalho, textos, produtos e análises para REV+1 e recalcula.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GERAR_REVISAO_PROPOSTAS` (erros P1–P9 do doc 12 §2)

Comentário do script: _PR_REVISAO_PROPOSTA (corrige SP_GERAR_REVISAO_PROPOSTAS)_

## Assinatura
```sql
CREATE PROCEDURE PR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                     OUT p_id_nova INT, OUT p_revisao INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_origem` | INT |
| IN | `p_motivo` | VARCHAR(500) |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_nova` | INT |
| OUT | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** `SP_GERAR_REVISAO_PROPOSTA`.
- **Chamada por rotinas:** [`SP_GERAR_REVISAO_PROPOSTA`](../rotinas/SP_GERAR_REVISAO_PROPOSTA.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_TEXTOS`](../tabelas/LAB_PROPOSTAS_TEXTOS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Lê:** [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md)
- **Chama:** [`SP_RECALCULAR_PROPOSTA`](../rotinas/SP_RECALCULAR_PROPOSTA.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Proposta de origem não encontrada."

## Processos
- [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_REVISAO_PROPOSTA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_origem", id_origem));
cmd.Parameters.Add(new MySqlParameter("p_motivo", motivo));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
var o_id_nova = new MySqlParameter("p_id_nova", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_nova);
var o_revisao = new MySqlParameter("p_revisao", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_revisao);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                     OUT p_id_nova INT, OUT p_revisao INT)
BEGIN
  DECLARE v_emp, v_cod, v_ano, v_max, v_status_ini INT;

  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA INTO v_emp, v_cod, v_ano
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta de origem não encontrada.';
  END IF;

  -- bloqueia todas as revisões da proposta (evita duas revisões simultâneas com o mesmo número)
  SELECT MAX(REV_PROPOSTA) INTO v_max
    FROM LAB_PROPOSTAS
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  SELECT ID_LAB_PROPOSTAS_STATUS_INICIAL INTO v_status_ini
    FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = v_emp;

  INSERT INTO LAB_PROPOSTAS
        (ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, REV_PROPOSTA, NR_PROPOSTA, ID_LAB_PROPOSTAS_ORIGEM,
         ULTIMA_REVISAO, MOTIVO_REVISAO, ID_ENTIDADES, ID_ENT_CONTATO, ID_ENTIDADES_COMERC,
         ID_ENTIDADES_FUNC, ID_ENT_END_LAUDO, ID_ENT_CONTATO_RESULTADO, ID_ENT_COBRANCA,
         ID_ENT_END_COBRANCA, ID_ENT_CONTATO_NF, DT_SOLICITACAO, DT_VALIDADE, DT_ENVIO_CLIENTE,
         DT_AUTORIZACAO, ID_LAB_PROPOSTAS_STATUS, ID_CONDICOES_PAGTOS, ID_MOEDAS, PORC_DESCONTO,
         VR_DESCONTO, CRIADO_POR)
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, p_revisao, '', p_id_origem,
         1, p_motivo, ID_ENTIDADES, ID_ENT_CONTATO, ID_ENTIDADES_COMERC,
         ID_ENTIDADES_FUNC, ID_ENT_END_LAUDO, ID_ENT_CONTATO_RESULTADO, ID_ENT_COBRANCA,
         ID_ENT_END_COBRANCA, ID_ENT_CONTATO_NF, DT_SOLICITACAO, DT_VALIDADE, NULL,
         NULL, COALESCE(v_status_ini, ID_LAB_PROPOSTAS_STATUS), ID_CONDICOES_PAGTOS, ID_MOEDAS, PORC_DESCONTO,
         VR_DESCONTO, p_id_usuario
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_origem;
  SET p_id_nova = LAST_INSERT_ID();

  UPDATE LAB_PROPOSTAS SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod
     AND ID_LAB_PROPOSTAS <> p_id_nova AND ULTIMA_REVISAO = 1;

  INSERT INTO LAB_PROPOSTAS_TEXTOS
        (ID_LAB_PROPOSTAS, ID_EMPRESAS, TX_ITENS_SOLICITADOS, TX_ITENS_ADQUIRIDOS, TX_OBSERVACAO,
         TX_CONTRATO, TX_AUTORIZACAO)
  SELECT p_id_nova, ID_EMPRESAS, TX_ITENS_SOLICITADOS, TX_ITENS_ADQUIRIDOS, TX_OBSERVACAO,
         TX_CONTRATO, NULL
    FROM LAB_PROPOSTAS_TEXTOS WHERE ID_LAB_PROPOSTAS = p_id_origem;

  INSERT INTO LAB_PROPOSTAS_PRODUTOS (ID_EMPRESAS, ID_LAB_PROPOSTAS, ITEM, ID_PRODUTOS, QTDE_AMOSTRAS, ID_ORIGEM)
  SELECT ID_EMPRESAS, p_id_nova, ITEM, ID_PRODUTOS, QTDE_AMOSTRAS, ID_LAB_PROPOSTAS_PRODUTOS
    FROM LAB_PROPOSTAS_PRODUTOS WHERE ID_LAB_PROPOSTAS = p_id_origem;

  -- ST_REV_ANTERIOR = 1: a linha veio da revisão anterior (o legado calculava esse
  -- flag com uma consulta não correlacionada e marcava todas as linhas igual).
  INSERT INTO LAB_PROPOSTAS_PRODANALISES
        (ID_EMPRESAS, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, TIPO_DOCUMENTO, QTDE_AMOSTRAS, VR_UNITARIO,
         PORC_DESCONTO, VR_DESCONTO, ST_REV_ANTERIOR, ID_ORIGEM)
  SELECT PA.ID_EMPRESAS, p_id_nova, NP.ID_LAB_PROPOSTAS_PRODUTOS, PA.ID_LAB_PARAMETROS_ANALISES,
         PA.ID_ANALISES_METODOS, PA.ID_IDIOMAS, PA.ID_PRAZOS, PA.TIPO_DOCUMENTO, PA.QTDE_AMOSTRAS, PA.VR_UNITARIO,
         PA.PORC_DESCONTO, PA.VR_DESCONTO, 1, PA.ID_LAB_PROPOSTAS_PRODANALISES
    FROM LAB_PROPOSTAS_PRODANALISES PA
    JOIN LAB_PROPOSTAS_PRODUTOS NP ON NP.ID_LAB_PROPOSTAS = p_id_nova
                                  AND NP.ID_ORIGEM = PA.ID_LAB_PROPOSTAS_PRODUTOS
   WHERE PA.ID_LAB_PROPOSTAS = p_id_origem
   ORDER BY PA.ID_LAB_PROPOSTAS_PRODANALISES;

  CALL SP_RECALCULAR_PROPOSTA(p_id_nova);
END
```
