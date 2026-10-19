# TG_BOLETIM_BI

Resultado da mesma amostra; `COD_BOLETIM` = número base da amostra; data padrão.

**Tabela:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) · **Momento:** BEFORE INSERT · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (o código herdava " R01" da amostra)

Comentário do script: _Boletins (LAB_BOLETIM_ANALISES_BU0/BD0/AD0 do legado, corrigidas)_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.COD_BOLETIM`
- `NEW.DT_BOLETIM`

## Validações (mensagens SQLSTATE 45000)
- "O resultado não pertence à amostra do boletim."
- "Revisões só podem ser geradas pela rotina de revisão."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_REVISAO` — 

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Chama:** [`FN_NR_AMOSTRA_ID`](../rotinas/FN_NR_AMOSTRA_ID.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `COD_BOLETIM`, `DT_BOLETIM`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Código
```sql
CREATE TRIGGER TG_BOLETIM_BI BEFORE INSERT ON LAB_BOLETIM_ANALISES FOR EACH ROW
BEGIN
  DECLARE v_emp, v_amo, v_tipo, v_cod, v_analise, v_ano INT;
  SELECT R.ID_EMPRESAS, R.ID_LAB_HIST_AMOSTRAS INTO v_emp, v_amo
    FROM LAB_RESULTADOS_ANALISES R WHERE R.ID_LAB_RESULTADOS_ANALISES = NEW.ID_LAB_RESULTADOS_ANALISES;
  IF v_amo IS NULL OR v_amo <> NEW.ID_LAB_HIST_AMOSTRAS THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O resultado não pertence à amostra do boletim.';
  END IF;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.REVISAO <> 0 AND COALESCE(@LC_REVISAO, 0) = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisões só podem ser geradas pela rotina de revisão.';
  END IF;
  -- COD_BOLETIM = número base da amostra física (sem o " Rnn"): constante entre revisões
  IF COALESCE(NEW.COD_BOLETIM, '') = '' THEN
    SELECT ID_AMOSTRAS_TIPO, COD_AMOSTRA, ID_ANALISES_TIPO, ANO_AMOSTRA INTO v_tipo, v_cod, v_analise, v_ano
      FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS;
    SET NEW.COD_BOLETIM = FN_NR_AMOSTRA_ID(v_tipo, v_cod, v_analise, v_ano, 0);
  END IF;
  IF NEW.DT_BOLETIM IS NULL THEN SET NEW.DT_BOLETIM = CURDATE(); END IF;
END
```
