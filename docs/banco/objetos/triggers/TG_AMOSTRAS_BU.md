# TG_AMOSTRAS_BU

Travas de alteração da amostra (revisão antiga, boletim emitido, resultados, movimentação), datas parciais e `NR_AMOSTRA`.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** BEFORE UPDATE · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_BU0/AU0`

## Colunas que a trigger preenche/ajusta
- `NEW.VALIDADE_DIA`
- `NEW.VALIDADE_MES`
- `NEW.VALIDADE_ANO`
- `NEW.FABRICACAO_DIA`
- `NEW.FABRICACAO_MES`
- `NEW.FABRICACAO_ANO`
- `NEW.ID_PRODUTOS`
- `NEW.DT_VALIDADE`
- `NEW.DT_FABRICACAO`
- `NEW.NR_AMOSTRA`

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga da amostra é somente leitura."
- "Número/ano/revisão/empresa da amostra não podem ser alterados."
- "Amostra com boletim emitido: gere uma revisão da amostra."
- "Existem resultados: tipo, proposta e produto da amostra não podem ser alterados (gere revisão)."
- "Não é permitido alterar a quantidade de amostras com movimentação."
- "Data de entrega maior que a data de hoje."
- "Validade: informe mês e ano quando informar o dia."
- "Validade: informe o ano quando informar o mês."
- "Fabricação: informe mês e ano quando informar o dia."
- "Fabricação: informe o ano quando informar o mês."
- "Data de validade inválida."
- "Data de fabricação inválida."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** [`FN_NR_AMOSTRA_ID`](../rotinas/FN_NR_AMOSTRA_ID.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `VALIDADE_DIA`, `VALIDADE_MES`, `VALIDADE_ANO`, `FABRICACAO_DIA`, `FABRICACAO_MES`, `FABRICACAO_ANO`, `ID_PRODUTOS`, `DT_VALIDADE`, `DT_FABRICACAO`, `NR_AMOSTRA`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_BU BEFORE UPDATE ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  IF NEW.VALIDADE_DIA = 0 THEN SET NEW.VALIDADE_DIA = NULL; END IF;
  IF NEW.VALIDADE_MES = 0 THEN SET NEW.VALIDADE_MES = NULL; END IF;
  IF NEW.VALIDADE_ANO = 0 THEN SET NEW.VALIDADE_ANO = NULL; END IF;
  IF NEW.FABRICACAO_DIA = 0 THEN SET NEW.FABRICACAO_DIA = NULL; END IF;
  IF NEW.FABRICACAO_MES = 0 THEN SET NEW.FABRICACAO_MES = NULL; END IF;
  IF NEW.FABRICACAO_ANO = 0 THEN SET NEW.FABRICACAO_ANO = NULL; END IF;

  -- Mudança só de ULTIMA_REVISAO (rotina de revisão) ou só do vínculo com o pedido
  -- (SP_GERAR_PEDIDO_VENDA) não passa pelas travas.
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NOT (NEW.ULTIMA_REVISAO <> OLD.ULTIMA_REVISAO)
     AND NOT (NOT (NEW.ID_PEDIDOS_VENDA <=> OLD.ID_PEDIDOS_VENDA) AND NEW.ID_LAB_PROPOSTAS = OLD.ID_LAB_PROPOSTAS
              AND NEW.QTDE_EMBALAGENS_ENTREGUE = OLD.QTDE_EMBALAGENS_ENTREGUE AND NEW.ID_AMOSTRAS_STATUS = OLD.ID_AMOSTRAS_STATUS) THEN
    IF OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga da amostra é somente leitura.';
    END IF;
    IF NEW.ID_EMPRESAS <> OLD.ID_EMPRESAS OR NEW.ANO_AMOSTRA <> OLD.ANO_AMOSTRA
       OR NEW.COD_AMOSTRA <> OLD.COD_AMOSTRA OR NEW.REVISAO <> OLD.REVISAO THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Número/ano/revisão/empresa da amostra não podem ser alterados.';
    END IF;
    IF EXISTS (SELECT 1 FROM LAB_BOLETIM_ANALISES B WHERE B.ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS
                AND B.DH_EMISSAO IS NOT NULL) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra com boletim emitido: gere uma revisão da amostra.';
    END IF;
    IF (NEW.ID_AMOSTRAS_TIPO <> OLD.ID_AMOSTRAS_TIPO OR NEW.ID_ANALISES_TIPO <> OLD.ID_ANALISES_TIPO
        OR NEW.ID_LAB_PROPOSTAS <> OLD.ID_LAB_PROPOSTAS OR NEW.ID_LAB_PROPOSTAS_PRODUTOS <> OLD.ID_LAB_PROPOSTAS_PRODUTOS)
       AND EXISTS (SELECT 1 FROM LAB_RESULTADOS_ANALISES R WHERE R.ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Existem resultados: tipo, proposta e produto da amostra não podem ser alterados (gere revisão).';
    END IF;
    -- regra do legado (LAB_HIST_AMOSTRAS_AU0): quantidade só muda se não houver movimentação manual
    IF NEW.QTDE_EMBALAGENS_ENTREGUE <> OLD.QTDE_EMBALAGENS_ENTREGUE AND OLD.REVISAO = 0
       AND EXISTS (SELECT 1 FROM LAB_MOV_AMOSTRAS M WHERE M.ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS
                    AND M.AUTOMATICA = 0) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não é permitido alterar a quantidade de amostras com movimentação.';
    END IF;
    IF NEW.DT_ENTREGA > CURDATE() THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de entrega maior que a data de hoje.';
    END IF;
    IF NEW.ID_LAB_PROPOSTAS_PRODUTOS <> OLD.ID_LAB_PROPOSTAS_PRODUTOS THEN
      SET NEW.ID_PRODUTOS = (SELECT ID_PRODUTOS FROM LAB_PROPOSTAS_PRODUTOS WHERE ID_LAB_PROPOSTAS_PRODUTOS = NEW.ID_LAB_PROPOSTAS_PRODUTOS);
    END IF;

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
    ELSE
      SET NEW.DT_VALIDADE = NULL;
    END IF;
    IF NEW.FABRICACAO_DIA IS NOT NULL THEN
      IF NEW.FABRICACAO_DIA > DAY(LAST_DAY(MAKEDATE(NEW.FABRICACAO_ANO, 1) + INTERVAL (NEW.FABRICACAO_MES - 1) MONTH)) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de fabricação inválida.';
      END IF;
      SET NEW.DT_FABRICACAO = MAKEDATE(NEW.FABRICACAO_ANO, 1) + INTERVAL (NEW.FABRICACAO_MES - 1) MONTH + INTERVAL (NEW.FABRICACAO_DIA - 1) DAY;
    ELSE
      SET NEW.DT_FABRICACAO = NULL;
    END IF;
  END IF;

  SET NEW.NR_AMOSTRA = FN_NR_AMOSTRA_ID(NEW.ID_AMOSTRAS_TIPO, NEW.COD_AMOSTRA, NEW.ID_ANALISES_TIPO,
                                        NEW.ANO_AMOSTRA, NEW.REVISAO);
END
```
