# TG_MOV_BI

Movimento vai sempre para a amostra raiz; saída sem saldo é recusada, com a linha do saldo travada até o fim da transação (duas saídas ao mesmo tempo não passam juntas — D-25).

**Tabela:** [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) · **Momento:** BEFORE INSERT · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (o legado permitia saldo negativo)

Comentário do script: _Movimentação de amostras e saldo (TG_LAB_MOV_AMOSTRAS_AIUD do legado)_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_LAB_HIST_AMOSTRAS`
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- "Saldo insuficiente da amostra para esta saída."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_SALDO`](../tabelas/LAB_HIST_AMOSTRAS_SALDO.md)
- **Chama:** [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_LAB_HIST_AMOSTRAS`, `ID_EMPRESAS`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_MOV_BI BEFORE INSERT ON LAB_MOV_AMOSTRAS FOR EACH ROW
BEGIN
  DECLARE v_raiz, v_emp INT;
  DECLARE v_saldo DECIMAL(15,4);
  -- a movimentação é sempre da amostra física (revisão 0)
  SELECT COALESCE(ID_AMOSTRA_RAIZ, ID_LAB_HIST_AMOSTRAS), ID_EMPRESAS INTO v_raiz, v_emp
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_LAB_HIST_AMOSTRAS = v_raiz, NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.E_S = 'S' THEN
    -- trava a linha do saldo até o fim da transação: duas saídas ao mesmo tempo não passam juntas (pendência 31)
    SELECT SALDO_ATUAL INTO v_saldo FROM LAB_HIST_AMOSTRAS_SALDO WHERE ID_LAB_HIST_AMOSTRAS = v_raiz FOR UPDATE;
    IF COALESCE(v_saldo, 0) < NEW.QTDE THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Saldo insuficiente da amostra para esta saída.';
    END IF;
  END IF;
END
```
