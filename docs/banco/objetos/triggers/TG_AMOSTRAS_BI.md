# TG_AMOSTRAS_BI

**Regra central do recebimento**: consistência com a proposta (empresa, cliente, item, última revisão, status), proteção da data de entrega, número com ano do servidor, pedido de venda, datas parciais e `NR_AMOSTRA`.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** BEFORE INSERT · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_BI0` + `SP_GET_COD_AMOSTRA` + validações de `act_GravarExecute` (Delphi)

Comentário do script: _Amostras (Recebimento / Histórico de Amostras)_

## Colunas que a trigger preenche/ajusta
- `NEW.VALIDADE_DIA`
- `NEW.VALIDADE_MES`
- `NEW.VALIDADE_ANO`
- `NEW.FABRICACAO_DIA`
- `NEW.FABRICACAO_MES`
- `NEW.FABRICACAO_ANO`
- `NEW.ID_PRODUTOS`
- `NEW.ANO_AMOSTRA`
- `NEW.COD_AMOSTRA`
- `NEW.ID_AMOSTRA_RAIZ`
- `NEW.ID_AMOSTRA_ORIGEM`
- `NEW.ID_PEDIDOS_VENDA`
- `NEW.DT_VALIDADE`
- `NEW.DT_FABRICACAO`
- `NEW.NR_AMOSTRA`

## Validações (mensagens SQLSTATE 45000)
- "Proposta não pertence a esta empresa."
- "A proposta selecionada não é do cliente da amostra."
- "O produto selecionado não pertence à proposta."
- "Revisões só podem ser geradas pela rotina de revisão."
- "Selecione a última revisão da proposta."
- "O status da proposta não permite receber amostras."
- "Data de entrega maior que a data de hoje."
- "Data de entrega muito antiga (verifique o ano digitado). Requer confirmação e permissão."
- "Validade: informe mês e ano quando informar o dia."
- "Validade: informe o ano quando informar o mês."
- "Fabricação: informe mês e ano quando informar o dia."
- "Fabricação: informe o ano quando informar o mês."
- "Data de validade inválida."
- "Data de fabricação inválida."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_REVISAO` — 
- `@LC_PERMITE_RETROATIVO` — 1 = libera data de entrega além de `DIAS_RETROATIVOS_ENTREGA` (só após confirmação + permissão `AMOSTRAS.ENTREGA_RETROATIVA`)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md), [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md), [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md)
- **Chama:** [`FN_NR_AMOSTRA_ID`](../rotinas/FN_NR_AMOSTRA_ID.md), [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `VALIDADE_DIA`, `VALIDADE_MES`, `VALIDADE_ANO`, `FABRICACAO_DIA`, `FABRICACAO_MES`, `FABRICACAO_ANO`, `ID_PRODUTOS`, `ANO_AMOSTRA`, `COD_AMOSTRA`, `ID_AMOSTRA_RAIZ`, `ID_AMOSTRA_ORIGEM`, `ID_PEDIDOS_VENDA`, `DT_VALIDADE`, `DT_FABRICACAO`, `NR_AMOSTRA`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_BI BEFORE INSERT ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  DECLARE v_num, v_dias, v_prop_emp, v_prop_cli, v_prop_ultima, v_item_prop, v_item_prod, v_pedido, v_permite INT;

  -- zero -> NULL (LAB_HIST_AMOSTRAS_BI0 do legado)
  IF NEW.VALIDADE_DIA = 0 THEN SET NEW.VALIDADE_DIA = NULL; END IF;
  IF NEW.VALIDADE_MES = 0 THEN SET NEW.VALIDADE_MES = NULL; END IF;
  IF NEW.VALIDADE_ANO = 0 THEN SET NEW.VALIDADE_ANO = NULL; END IF;
  IF NEW.FABRICACAO_DIA = 0 THEN SET NEW.FABRICACAO_DIA = NULL; END IF;
  IF NEW.FABRICACAO_MES = 0 THEN SET NEW.FABRICACAO_MES = NULL; END IF;
  IF NEW.FABRICACAO_ANO = 0 THEN SET NEW.FABRICACAO_ANO = NULL; END IF;

  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    -- consistência multiempresa com a proposta
    SELECT ID_EMPRESAS, ID_ENTIDADES, ULTIMA_REVISAO INTO v_prop_emp, v_prop_cli, v_prop_ultima
      FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS;
    SELECT ID_LAB_PROPOSTAS, ID_PRODUTOS INTO v_item_prop, v_item_prod
      FROM LAB_PROPOSTAS_PRODUTOS WHERE ID_LAB_PROPOSTAS_PRODUTOS = NEW.ID_LAB_PROPOSTAS_PRODUTOS;
    IF v_prop_emp IS NULL OR v_prop_emp <> NEW.ID_EMPRESAS THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta não pertence a esta empresa.';
    END IF;
    IF v_prop_cli <> NEW.ID_ENTIDADES THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A proposta selecionada não é do cliente da amostra.';
    END IF;
    IF v_item_prop IS NULL OR v_item_prop <> NEW.ID_LAB_PROPOSTAS THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O produto selecionado não pertence à proposta.';
    END IF;
    SET NEW.ID_PRODUTOS = v_item_prod;

    IF NEW.REVISAO <> 0 AND COALESCE(@LC_REVISAO, 0) = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisões só podem ser geradas pela rotina de revisão.';
    END IF;
    IF NEW.REVISAO = 0 THEN
      IF v_prop_ultima = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Selecione a última revisão da proposta.';
      END IF;
      SELECT S.PERMITE_AMOSTRA INTO v_permite FROM LAB_PROPOSTAS P
        JOIN LAB_PROPOSTAS_STATUS S ON S.ID_LAB_PROPOSTAS_STATUS = P.ID_LAB_PROPOSTAS_STATUS
       WHERE P.ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS;
      IF COALESCE(v_permite, 1) = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O status da proposta não permite receber amostras.';
      END IF;
      IF NEW.DT_ENTREGA > CURDATE() THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de entrega maior que a data de hoje.';
      END IF;
      SELECT DIAS_RETROATIVOS_ENTREGA INTO v_dias FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = NEW.ID_EMPRESAS;
      IF DATEDIFF(CURDATE(), NEW.DT_ENTREGA) > COALESCE(v_dias, 7) AND COALESCE(@LC_PERMITE_RETROATIVO, 0) = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de entrega muito antiga (verifique o ano digitado). Requer confirmação e permissão.';
      END IF;
      -- ANO = ano do SERVIDOR. Uma data digitada com ano errado nunca reinicia/duplica a numeração.
      SET NEW.ANO_AMOSTRA = YEAR(CURDATE());
      CALL SP_PROXIMO_NUMERO(NEW.ID_EMPRESAS, 'AMOSTRA', NEW.ANO_AMOSTRA, v_num);
      SET NEW.COD_AMOSTRA = v_num;
      SET NEW.ID_AMOSTRA_RAIZ = NULL;
      SET NEW.ID_AMOSTRA_ORIGEM = NULL;
    END IF;

    -- pedido de venda da proposta (qualquer revisão)
    IF NEW.ID_PEDIDOS_VENDA IS NULL THEN
      SELECT PV.ID_PEDIDOS_VENDA INTO v_pedido
        FROM PEDIDOS_VENDA PV JOIN LAB_PROPOSTAS P
          ON P.ID_EMPRESAS = PV.ID_EMPRESAS AND P.ANO_PROPOSTA = PV.ANO_PROPOSTA AND P.COD_PROPOSTA = PV.COD_PROPOSTA
       WHERE P.ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS AND PV.SITUACAO <> 'CANCELADO'
       LIMIT 1;
      SET NEW.ID_PEDIDOS_VENDA = v_pedido;
    END IF;
  END IF;

  -- datas completas a partir das partes (validação de dia do mês)
  IF NEW.VALIDADE_DIA IS NOT NULL AND (NEW.VALIDADE_MES IS NULL OR NEW.VALIDADE_ANO IS NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Validade: informe mês e ano quando informar o dia.';
  END IF;
  IF NEW.VALIDADE_MES IS NOT NULL AND NEW.VALIDADE_ANO IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Validade: informe o ano quando informar o mês.';
  END IF;
  IF NEW.FABRICACAO_DIA IS NOT NULL AND (NEW.FABRICACAO_MES IS NULL OR NEW.FABRICACAO_ANO IS NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Fabricação: informe mês e ano quando informar o dia.';
  END IF;
  IF NEW.FABRICACAO_MES IS NOT NULL AND NEW.FABRICACAO_ANO IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Fabricação: informe o ano quando informar o mês.';
  END IF;
  IF NEW.VALIDADE_DIA IS NOT NULL THEN
    IF NEW.VALIDADE_DIA > DAY(LAST_DAY(MAKEDATE(NEW.VALIDADE_ANO, 1) + INTERVAL (NEW.VALIDADE_MES - 1) MONTH)) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de validade inválida.';
    END IF;
    SET NEW.DT_VALIDADE = MAKEDATE(NEW.VALIDADE_ANO, 1) + INTERVAL (NEW.VALIDADE_MES - 1) MONTH + INTERVAL (NEW.VALIDADE_DIA - 1) DAY;
  ELSEIF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    SET NEW.DT_VALIDADE = NULL;
  END IF;
  IF NEW.FABRICACAO_DIA IS NOT NULL THEN
    IF NEW.FABRICACAO_DIA > DAY(LAST_DAY(MAKEDATE(NEW.FABRICACAO_ANO, 1) + INTERVAL (NEW.FABRICACAO_MES - 1) MONTH)) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de fabricação inválida.';
    END IF;
    SET NEW.DT_FABRICACAO = MAKEDATE(NEW.FABRICACAO_ANO, 1) + INTERVAL (NEW.FABRICACAO_MES - 1) MONTH + INTERVAL (NEW.FABRICACAO_DIA - 1) DAY;
  ELSEIF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    SET NEW.DT_FABRICACAO = NULL;
  END IF;

  SET NEW.NR_AMOSTRA = FN_NR_AMOSTRA_ID(NEW.ID_AMOSTRAS_TIPO, NEW.COD_AMOSTRA, NEW.ID_ANALISES_TIPO,
                                        NEW.ANO_AMOSTRA, NEW.REVISAO);
END
```
