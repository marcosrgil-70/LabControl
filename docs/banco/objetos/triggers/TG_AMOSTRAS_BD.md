# TG_AMOSTRAS_BD

Exclusão só da revisão 0, sem resultados e sem movimentação manual.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** BEFORE DELETE · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_AD0/AD1`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Revisões de amostra não podem ser excluídas."
- "Amostra possui resultados e não pode ser excluída."
- "Não é permitido excluir amostras com movimentação."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_BD BEFORE DELETE ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF OLD.REVISAO > 0 OR OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisões de amostra não podem ser excluídas.';
    END IF;
    IF EXISTS (SELECT 1 FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra possui resultados e não pode ser excluída.';
    END IF;
    IF EXISTS (SELECT 1 FROM LAB_MOV_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = OLD.ID_LAB_HIST_AMOSTRAS AND AUTOMATICA = 0) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não é permitido excluir amostras com movimentação.';
    END IF;
  END IF;
END
```
