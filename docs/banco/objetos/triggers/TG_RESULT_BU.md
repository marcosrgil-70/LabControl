# TG_RESULT_BU

Revisão antiga somente leitura; amostra/revisão imutáveis.

**Tabela:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) · **Momento:** BEFORE UPDATE · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga do resultado é somente leitura."
- "Amostra/revisão do resultado não podem ser alteradas."

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
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Código
```sql
CREATE TRIGGER TG_RESULT_BU BEFORE UPDATE ON LAB_RESULTADOS_ANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NOT (NEW.ULTIMA_REVISAO <> OLD.ULTIMA_REVISAO) THEN
    IF OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga do resultado é somente leitura.';
    END IF;
    IF NEW.ID_LAB_HIST_AMOSTRAS <> OLD.ID_LAB_HIST_AMOSTRAS OR NEW.REVISAO <> OLD.REVISAO THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra/revisão do resultado não podem ser alteradas.';
    END IF;
  END IF;
END
```
