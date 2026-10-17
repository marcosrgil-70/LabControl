# TG_PRODAN_AI

Recalcula os totais da proposta após incluir análise.

**Tabela:** [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) · **Momento:** AFTER INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`TotalizarAnalises`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`SP_RECALCULAR_PROPOSTA`](../rotinas/SP_RECALCULAR_PROPOSTA.md)

## Impacto na aplicação
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Código
```sql
CREATE TRIGGER TG_PRODAN_AI AFTER INSERT ON LAB_PROPOSTAS_PRODANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN CALL SP_RECALCULAR_PROPOSTA(NEW.ID_LAB_PROPOSTAS); END IF;
END
```
