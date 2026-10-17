# TG_RESULT_BD

Exclusão só da última revisão, sem boletim; revisão > 0 só por "Desfazer revisão".

**Tabela:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) · **Momento:** BEFORE DELETE · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_RESULTADOS_ANALISES_BD0` + `ERR_DELETE_RESULTADO`

Comentário do script: _LAB_RESULTADOS_ANALISES_BD0 do legado: só a última revisão (via SP_EXCLUIR_REVISAO_RESULTADO)_

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Só é possível excluir a última revisão."
- "Para excluir uma revisão use "Desfazer revisão"."
- "Resultado possui boletim e não pode ser excluído."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_DESFAZENDO` — uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Código
```sql
CREATE TRIGGER TG_RESULT_BD BEFORE DELETE ON LAB_RESULTADOS_ANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível excluir a última revisão.';
    END IF;
    IF OLD.REVISAO > 0 AND COALESCE(@LC_DESFAZENDO, 0) = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Para excluir uma revisão use "Desfazer revisão".';
    END IF;
    IF EXISTS (SELECT 1 FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = OLD.ID_LAB_RESULTADOS_ANALISES) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado possui boletim e não pode ser excluído.';
    END IF;
  END IF;
END
```
