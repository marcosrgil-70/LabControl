# TG_PRODAN_BD

Bloqueia exclusão de análise em revisão antiga.

**Tabela:** [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) · **Momento:** BEFORE DELETE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga da proposta é somente leitura."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Código
```sql
CREATE TRIGGER TG_PRODAN_BD BEFORE DELETE ON LAB_PROPOSTAS_PRODANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0
     AND (SELECT ULTIMA_REVISAO FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = OLD.ID_LAB_PROPOSTAS) = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga da proposta é somente leitura.';
  END IF;
END
```
