# TG_MOV_AD

Recalcula o saldo após excluir movimento.

**Tabela:** [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) · **Momento:** AFTER DELETE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`TG_LAB_MOV_AMOSTRAS_AIUD`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`PR_ATUALIZA_SALDO`](../rotinas/PR_ATUALIZA_SALDO.md)

## Impacto na aplicação
- —

## Telas e processos relacionados
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_MOV_AD AFTER DELETE ON LAB_MOV_AMOSTRAS FOR EACH ROW
BEGIN
  CALL PR_ATUALIZA_SALDO(OLD.ID_LAB_HIST_AMOSTRAS);
END
```
