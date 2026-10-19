# SP_GERAR_PEDIDO_VENDA

Gera o pedido de venda da proposta (um por proposta), com itens, e liga as amostras já recebidas.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`Btn_PedVendaClick` + `SP_GET_ULT_PED_VENDA`

Comentário do script: _SP_GERAR_PEDIDO_VENDA — substitui Btn_PedVendaClick + SP_GET_ULT_PED_VENDA. Um pedido por proposta (todas as revisões). Exige data de autorização._

## Assinatura
```sql
CREATE PROCEDURE SP_GERAR_PEDIDO_VENDA(IN p_id_proposta INT, IN p_dt_autorizacao DATE,
                                       IN p_autorizado_por VARCHAR(100), IN p_nr_pedido_cliente VARCHAR(40),
                                       IN p_id_usuario INT, OUT p_id_pedido INT, OUT p_nr_pedido INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_proposta` | INT |
| IN | `p_dt_autorizacao` | DATE |
| IN | `p_autorizado_por` | VARCHAR(100) |
| IN | `p_nr_pedido_cliente` | VARCHAR(40) |
| IN | `p_id_usuario` | INT |
| OUT | `p_id_pedido` | INT |
| OUT | `p_nr_pedido` | INT |

## Quem chama
- **Uso previsto:** "Gerar pedido" da T20/T21 (`PEDIDOS_VENDA.GERAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md)
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Lê:** [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "É obrigatório informar a Data de Autorização para gerar o Pedido de Venda."
- "Data de Autorização não pode ser futura."
- "Proposta não encontrada."
- "Gere o pedido a partir da última revisão da proposta."
- "Já existe Pedido de Venda para esta proposta."
- "Proposta sem análises."

## Processos
- [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_GERAR_PEDIDO_VENDA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_proposta", id_proposta));
cmd.Parameters.Add(new MySqlParameter("p_dt_autorizacao", dt_autorizacao));
cmd.Parameters.Add(new MySqlParameter("p_autorizado_por", autorizado_por));
cmd.Parameters.Add(new MySqlParameter("p_nr_pedido_cliente", nr_pedido_cliente));
cmd.Parameters.Add(new MySqlParameter("p_id_usuario", id_usuario));
var o_id_pedido = new MySqlParameter("p_id_pedido", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_id_pedido);
var o_nr_pedido = new MySqlParameter("p_nr_pedido", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
cmd.Parameters.Add(o_nr_pedido);
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_GERAR_PEDIDO_VENDA(IN p_id_proposta INT, IN p_dt_autorizacao DATE,
                                       IN p_autorizado_por VARCHAR(100), IN p_nr_pedido_cliente VARCHAR(40),
                                       IN p_id_usuario INT, OUT p_id_pedido INT, OUT p_nr_pedido INT)
BEGIN
  DECLARE v_emp, v_cod, v_ano, v_ultima INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  IF p_dt_autorizacao IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'É obrigatório informar a Data de Autorização para gerar o Pedido de Venda.';
  END IF;
  IF p_dt_autorizacao > CURDATE() THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de Autorização não pode ser futura.';
  END IF;
  START TRANSACTION;
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, ULTIMA_REVISAO INTO v_emp, v_cod, v_ano, v_ultima
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta FOR UPDATE;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta não encontrada.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gere o pedido a partir da última revisão da proposta.';
  END IF;
  IF EXISTS (SELECT 1 FROM PEDIDOS_VENDA WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe Pedido de Venda para esta proposta.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM LAB_PROPOSTAS_PRODANALISES WHERE ID_LAB_PROPOSTAS = p_id_proposta) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta sem análises.';
  END IF;

  UPDATE LAB_PROPOSTAS SET DT_AUTORIZACAO = p_dt_autorizacao, ALTERADO_POR = p_id_usuario
   WHERE ID_LAB_PROPOSTAS = p_id_proposta;

  INSERT INTO PEDIDOS_VENDA
        (ID_EMPRESAS, NR_PEDIDO, DT_PEDIDO, ID_LAB_PROPOSTAS, COD_PROPOSTA, ANO_PROPOSTA, ID_ENTIDADES,
         ID_ENT_COBRANCA, ID_CONDICOES_PAGTOS, ID_MOEDAS, DT_AUTORIZACAO, AUTORIZADO_POR, NR_PEDIDO_CLIENTE,
         VR_TOTAL, CRIADO_POR)
  SELECT ID_EMPRESAS, 0, CURDATE(), ID_LAB_PROPOSTAS, COD_PROPOSTA, ANO_PROPOSTA, ID_ENTIDADES,
         ID_ENT_COBRANCA, ID_CONDICOES_PAGTOS, ID_MOEDAS, p_dt_autorizacao, p_autorizado_por, p_nr_pedido_cliente,
         VR_TOTAL_PROPOSTA, p_id_usuario
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta;
  SET p_id_pedido = LAST_INSERT_ID();
  SELECT NR_PEDIDO INTO p_nr_pedido FROM PEDIDOS_VENDA WHERE ID_PEDIDOS_VENDA = p_id_pedido;

  INSERT INTO PEDIDOS_VENDA_ITENS (ID_EMPRESAS, ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS, ID_PRODUTOS, QTDE_CONTRATADA, VR_TOTAL)
  SELECT PP.ID_EMPRESAS, p_id_pedido, PP.ID_LAB_PROPOSTAS_PRODUTOS, PP.ID_PRODUTOS, PP.QTDE_AMOSTRAS,
         COALESCE((SELECT SUM(PA.VR_TOTAL) FROM LAB_PROPOSTAS_PRODANALISES PA
                    WHERE PA.ID_LAB_PROPOSTAS_PRODUTOS = PP.ID_LAB_PROPOSTAS_PRODUTOS), 0)
    FROM LAB_PROPOSTAS_PRODUTOS PP WHERE PP.ID_LAB_PROPOSTAS = p_id_proposta;

  -- amostras já recebidas desta proposta (qualquer revisão) passam a apontar para o pedido
  UPDATE LAB_HIST_AMOSTRAS A
    JOIN LAB_PROPOSTAS P ON P.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS
     SET A.ID_PEDIDOS_VENDA = p_id_pedido
   WHERE P.ID_EMPRESAS = v_emp AND P.ANO_PROPOSTA = v_ano AND P.COD_PROPOSTA = v_cod
     AND A.ID_PEDIDOS_VENDA IS NULL AND A.ULTIMA_REVISAO = 1;
  CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'I', NULL,
                  JSON_OBJECT('NR_PEDIDO', p_nr_pedido, 'ID_LAB_PROPOSTAS', p_id_proposta, 'DT_AUTORIZACAO', p_dt_autorizacao,
                              'AUTORIZADO_POR', p_autorizado_por, 'NR_PEDIDO_CLIENTE', p_nr_pedido_cliente));
  COMMIT;
END
```
