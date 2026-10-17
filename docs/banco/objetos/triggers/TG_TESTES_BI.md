# TG_TESTES_BI

Teste herda a empresa da amostra e calcula a data prevista (entrega + prazo).

**Tabela:** [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.DT_PREVISTA`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`PRAZOS`](../tabelas/PRAZOS.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Chama:** —

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `DT_PREVISTA`. Reler a linha depois de gravar.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md)
- **Processos:** [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md)

## Código
```sql
CREATE TRIGGER TG_TESTES_BI BEFORE INSERT ON LAB_HIST_AMOSTRAS_TESTES FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  DECLARE v_dt DATE;
  SELECT ID_EMPRESAS, DT_ENTREGA INTO v_emp, v_dt FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS;
  SET NEW.ID_EMPRESAS = v_emp;
  IF NEW.DT_PREVISTA IS NULL THEN
    SET NEW.DT_PREVISTA = DATE_ADD(v_dt, INTERVAL COALESCE((SELECT QTDE_DIAS FROM PRAZOS WHERE ID_PRAZOS = NEW.ID_PRAZOS), 0) DAY);
  END IF;
END
```
