# TG_RESULT_BI

Resultado herda a empresa da amostra.

**Tabela:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _Resultados_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- "Revisões só podem ser geradas pela rotina de revisão."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_REVISAO` — 

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Chama:** [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Código
```sql
CREATE TRIGGER TG_RESULT_BI BEFORE INSERT ON LAB_RESULTADOS_ANALISES FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.REVISAO <> 0 AND COALESCE(@LC_REVISAO, 0) = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisões só podem ser geradas pela rotina de revisão.';
  END IF;
END
```
