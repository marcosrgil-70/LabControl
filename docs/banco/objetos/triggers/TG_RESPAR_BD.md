# TG_RESPAR_BD

Bloqueia exclusão em revisão antiga ou de parâmetro usado em boletim.

**Tabela:** [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) · **Momento:** BEFORE DELETE · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga do resultado é somente leitura."
- "Resultado usado em boletim: retire-o do boletim antes de excluir."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_DESFAZENDO` — uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T30-04 Parâmetros analisados](../../../telas/T30-04-rel-parametros-analisados.md) · [T30-05 Tempo das análises](../../../telas/T30-05-rel-tempo-analises.md) · [T30-06 Produção por período](../../../telas/T30-06-rel-producao.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Código
```sql
CREATE TRIGGER TG_RESPAR_BD BEFORE DELETE ON LAB_RESULTADOS_PARAM FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND COALESCE(@LC_DESFAZENDO, 0) = 0 THEN
    IF (SELECT ULTIMA_REVISAO FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = OLD.ID_LAB_RESULTADOS_ANALISES) = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga do resultado é somente leitura.';
    END IF;
    IF EXISTS (SELECT 1 FROM LAB_BOLETIM_PARAMETROS WHERE ID_LAB_RESULTADOS_PARAM = OLD.ID_LAB_RESULTADOS_PARAM) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado usado em boletim: retire-o do boletim antes de excluir.';
    END IF;
  END IF;
END
```
