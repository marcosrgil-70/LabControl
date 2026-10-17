# TG_MOV_BD

A entrada automática do recebimento não pode ser excluída.

**Tabela:** [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) · **Momento:** BEFORE DELETE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`ERR_MOV_AMOSTRA`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "A entrada automática do recebimento não pode ser excluída."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
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
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND OLD.AUTOMATICA = 1 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A entrada automática do recebimento não pode ser excluída.';
  END IF;
END
```
