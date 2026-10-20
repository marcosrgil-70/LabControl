# SP_ATUALIZAR_PEDIDO_REVISAO

Atualiza o pedido para a revisão vigente da proposta (valores, cobrança, condição, itens).

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (o legado copiava o número do pedido em cada revisão)

Comentário do script: _Atualiza o pedido para a revisão vigente da proposta (após revisão aprovada pelo cliente)_

## Assinatura
```sql

```

| Direção | Parâmetro | Tipo |
|---|---|---|

## Quem chama
- **Uso previsto:** "Atualizar para revisão vigente" da T21 (`PEDIDOS_VENDA.ALTERAR`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md)
- **Lê:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Pedido não encontrado."
- "Pedido concluído/cancelado não pode ser atualizado."
- "Revisão atual da proposta não encontrada."

## Processos
- [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_ATUALIZAR_PEDIDO_REVISAO";
cmd.CommandType = CommandType.StoredProcedure;
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_ATUALIZAR_PEDIDO_REVISAO(IN p_id_pedido INT, IN p_id_usuario INT)
corpo: BEGIN
  DECLARE v_emp, v_cod, v_ano, v_nova, v_antiga INT;
  DECLARE v_sit VARCHAR(20);
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, SITUACAO, ID_LAB_PROPOSTAS INTO v_emp, v_cod, v_ano, v_sit, v_antiga
    FROM PEDIDOS_VENDA WHERE ID_PEDIDOS_VENDA = p_id_pedido FOR UPDATE;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Pedido não encontrado.';
  END IF;
  IF v_sit IN ('CONCLUIDO', 'CANCELADO') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Pedido concluído/cancelado não pode ser atualizado.';
  END IF;
  SELECT ID_LAB_PROPOSTAS INTO v_nova FROM LAB_PROPOSTAS
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod AND ULTIMA_REVISAO = 1;
  -- sem revisão atual os itens seriam apagados e nada reinserido; já na revisão atual não há o que atualizar
  IF v_nova IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão atual da proposta não encontrada.';
  END IF;
  IF v_nova = v_antiga THEN
    COMMIT;
    LEAVE corpo;
  END IF;

  UPDATE PEDIDOS_VENDA PV JOIN LAB_PROPOSTAS P ON P.ID_LAB_PROPOSTAS = v_nova
     SET PV.ID_LAB_PROPOSTAS = v_nova, PV.VR_TOTAL = P.VR_TOTAL_PROPOSTA,
         PV.ID_CONDICOES_PAGTOS = P.ID_CONDICOES_PAGTOS, PV.ID_MOEDAS = P.ID_MOEDAS,
         PV.ID_ENT_COBRANCA = P.ID_ENT_COBRANCA, PV.ALTERADO_POR = p_id_usuario
   WHERE PV.ID_PEDIDOS_VENDA = p_id_pedido;

  DELETE FROM PEDIDOS_VENDA_ITENS WHERE ID_PEDIDOS_VENDA = p_id_pedido;
  INSERT INTO PEDIDOS_VENDA_ITENS (ID_EMPRESAS, ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS, ID_PRODUTOS, QTDE_CONTRATADA, VR_TOTAL)
  SELECT PP.ID_EMPRESAS, p_id_pedido, PP.ID_LAB_PROPOSTAS_PRODUTOS, PP.ID_PRODUTOS, PP.QTDE_AMOSTRAS,
         COALESCE((SELECT SUM(PA.VR_TOTAL) FROM LAB_PROPOSTAS_PRODANALISES PA
                    WHERE PA.ID_LAB_PROPOSTAS_PRODUTOS = PP.ID_LAB_PROPOSTAS_PRODUTOS), 0)
    FROM LAB_PROPOSTAS_PRODUTOS PP WHERE PP.ID_LAB_PROPOSTAS = v_nova;
  CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'A', JSON_OBJECT('ID_LAB_PROPOSTAS', v_antiga),
                  JSON_OBJECT('ID_LAB_PROPOSTAS', v_nova));
  COMMIT;
END
```
