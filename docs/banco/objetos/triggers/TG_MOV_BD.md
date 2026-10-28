# TG_MOV_BD

A entrada automática do recebimento não pode ser excluída; a entrada complementar também não, se o saldo ficaria negativo sem ela (RN-06.15, linha do saldo travada — D-25).

**Tabela:** [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) · **Momento:** BEFORE DELETE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`ERR_MOV_AMOSTRA`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "A entrada automática do recebimento não pode ser excluída."
- "Esta entrada não pode ser excluída: o saldo da amostra ficaria negativo."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS_SALDO`](../tabelas/LAB_HIST_AMOSTRAS_SALDO.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_MOV_BD BEFORE DELETE ON LAB_MOV_AMOSTRAS FOR EACH ROW
BEGIN
  DECLARE v_saldo DECIMAL(15,4);
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF OLD.AUTOMATICA = 1 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A entrada automática do recebimento não pode ser excluída.';
    END IF;
    -- sem a entrada, a saída que ela cobria deixaria o saldo negativo (RN-06.15; trava a linha do saldo)
    IF OLD.E_S = 'E' THEN
      SELECT SALDO_ATUAL INTO v_saldo FROM LAB_HIST_AMOSTRAS_SALDO WHERE ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS FOR UPDATE;
      IF COALESCE(v_saldo, 0) < OLD.QTDE THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esta entrada não pode ser excluída: o saldo da amostra ficaria negativo.';
      END IF;
    END IF;
  END IF;
END
```
