# PR_ATUALIZAR_SITUACAO_PEDIDO

Situação automática do pedido (pendência 24): EM_EXECUCAO → CONCLUIDO quando cada item recebeu o contratado e toda amostra vigente tem o boletim vigente emitido; CONCLUIDO → EM_EXECUCAO quando algo ficou pendente (revisão de boletim/resultado/amostra). ABERTO e CANCELADO não mudam. Grava `AUDITORIA` com o motivo.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (o legado não tinha pedido de venda)

Comentário do script: _PR_ATUALIZAR_SITUACAO_PEDIDO — pendência 24 (P08): situação automática do pedido.   EM_EXECUCAO → CONCLUIDO: cada item recebeu ao menos o contratado (amostras revisão 0 do item, como a     VW_PEDIDOS_VENDA_SALDO) e nada está pendente;   CONCLUIDO → EM_EXECUCAO: algo ficou pendente (decisão do usuário, 27/10/2026: revisão de boletim/resultado/     amostra reabre o pedido). Pendente = amostra vigente do pedido sem boletim vigente emitido (pela amostra raiz) ou boletim vigente não emitido. Mesmo critério da M-17 (M17Boletins.ConcluirPedidosAsync). Chamada dentro da transação de quem chama (emissão, revisões e desfazer revisão de boletim); ABERTO e CANCELADO não mudam aqui._

## Assinatura
```sql

```

| Direção | Parâmetro | Tipo |
|---|---|---|

## Quem chama
- **Uso previsto:** Fim do `SP_EMITIR_BOLETIM`, `SP_GERAR_PEDIDO_VENDA`, `SP_GERAR_REVISAO_AMOSTRA/RESULTADO/BOLETIM` e `SP_EXCLUIR_REVISAO_BOLETIM`.
- **Chamada por rotinas:** [`SP_GERAR_REVISAO_AMOSTRA`](../rotinas/SP_GERAR_REVISAO_AMOSTRA.md), [`SP_GERAR_REVISAO_RESULTADO`](../rotinas/SP_GERAR_REVISAO_RESULTADO.md), [`SP_GERAR_REVISAO_BOLETIM`](../rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Interna, sem transação (roda na transação de quem chama).

## O que acessa
- **Altera:** [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md)
- **Lê:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** [`PR_AUDITAR`](../rotinas/PR_AUDITAR.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)
- [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_ATUALIZAR_SITUACAO_PEDIDO";
cmd.CommandType = CommandType.StoredProcedure;
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_ATUALIZAR_SITUACAO_PEDIDO(IN p_id_pedido INT)
corpo: BEGIN
  DECLARE v_emp, v_cod, v_ano INT;
  DECLARE v_sit VARCHAR(20);
  DECLARE v_pendente, v_faltando TINYINT;
  IF p_id_pedido IS NULL THEN LEAVE corpo; END IF;
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, SITUACAO INTO v_emp, v_cod, v_ano, v_sit
    FROM PEDIDOS_VENDA WHERE ID_PEDIDOS_VENDA = p_id_pedido FOR UPDATE;
  IF v_sit IS NULL OR v_sit NOT IN ('EM_EXECUCAO', 'CONCLUIDO') THEN LEAVE corpo; END IF;

  SET v_pendente = EXISTS (
        SELECT 1 FROM LAB_HIST_AMOSTRAS A
         WHERE A.ID_PEDIDOS_VENDA = p_id_pedido AND A.ULTIMA_REVISAO = 1
           AND NOT EXISTS (SELECT 1 FROM LAB_BOLETIM_ANALISES B
                             JOIN LAB_HIST_AMOSTRAS BA ON BA.ID_LAB_HIST_AMOSTRAS = B.ID_LAB_HIST_AMOSTRAS
                            WHERE COALESCE(BA.ID_AMOSTRA_RAIZ, BA.ID_LAB_HIST_AMOSTRAS) = COALESCE(A.ID_AMOSTRA_RAIZ, A.ID_LAB_HIST_AMOSTRAS)
                              AND B.ULTIMA_REVISAO = 1 AND B.DH_EMISSAO IS NOT NULL))
     OR EXISTS (
        SELECT 1 FROM LAB_BOLETIM_ANALISES B
          JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = B.ID_LAB_HIST_AMOSTRAS
         WHERE A.ID_PEDIDOS_VENDA = p_id_pedido AND B.ULTIMA_REVISAO = 1 AND B.DH_EMISSAO IS NULL);

  IF v_sit = 'CONCLUIDO' THEN
    IF v_pendente THEN
      UPDATE PEDIDOS_VENDA SET SITUACAO = 'EM_EXECUCAO' WHERE ID_PEDIDOS_VENDA = p_id_pedido;
      CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'A', JSON_OBJECT('SITUACAO', 'CONCLUIDO'),
                      JSON_OBJECT('SITUACAO', 'EM_EXECUCAO', 'MOTIVO', 'Reaberto automaticamente: revisão pendente de emissão.'));
    END IF;
    LEAVE corpo;
  END IF;

  IF v_pendente THEN LEAVE corpo; END IF;
  SET v_faltando = EXISTS (
        SELECT 1 FROM PEDIDOS_VENDA_ITENS PI
          JOIN LAB_PROPOSTAS_PRODUTOS PP ON PP.ID_LAB_PROPOSTAS_PRODUTOS = PI.ID_LAB_PROPOSTAS_PRODUTOS
         WHERE PI.ID_PEDIDOS_VENDA = p_id_pedido
           AND PI.QTDE_CONTRATADA > (SELECT COUNT(*) FROM LAB_HIST_AMOSTRAS A
                                       JOIN LAB_PROPOSTAS PA ON PA.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS
                                       JOIN LAB_PROPOSTAS_PRODUTOS PPA ON PPA.ID_LAB_PROPOSTAS_PRODUTOS = A.ID_LAB_PROPOSTAS_PRODUTOS
                                      WHERE PA.ID_EMPRESAS = v_emp AND PA.ANO_PROPOSTA = v_ano AND PA.COD_PROPOSTA = v_cod
                                        AND PPA.ITEM = PP.ITEM AND A.REVISAO = 0));
  IF NOT v_faltando THEN
    UPDATE PEDIDOS_VENDA SET SITUACAO = 'CONCLUIDO' WHERE ID_PEDIDOS_VENDA = p_id_pedido;
    CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'A', JSON_OBJECT('SITUACAO', 'EM_EXECUCAO'),
                    JSON_OBJECT('SITUACAO', 'CONCLUIDO', 'MOTIVO', 'Concluído automaticamente: itens recebidos e boletins emitidos.'));
  END IF;
END
```
