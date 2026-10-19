# TG_BOLPAR_BI

Parâmetro do mesmo resultado; bloqueia boletim emitido.

**Tabela:** [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md) · **Momento:** BEFORE INSERT · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- "Este boletim foi emitido. Não é permitido alterar ou excluir."
- "O parâmetro não pertence ao resultado do boletim."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md), [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Chama:** [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Código
```sql
CREATE TRIGGER TG_BOLPAR_BI BEFORE INSERT ON LAB_BOLETIM_PARAMETROS FOR EACH ROW
BEGIN
  DECLARE v_emp, v_res, v_res_par INT;
  DECLARE v_emi DATETIME;
  SELECT ID_EMPRESAS, ID_LAB_RESULTADOS_ANALISES, DH_EMISSAO INTO v_emp, v_res, v_emi
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = NEW.ID_LAB_BOLETIM_ANALISES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF v_emi IS NOT NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este boletim foi emitido. Não é permitido alterar ou excluir.';
    END IF;
    SELECT ID_LAB_RESULTADOS_ANALISES INTO v_res_par FROM LAB_RESULTADOS_PARAM
     WHERE ID_LAB_RESULTADOS_PARAM = NEW.ID_LAB_RESULTADOS_PARAM;
    IF v_res_par IS NULL OR v_res_par <> v_res THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O parâmetro não pertence ao resultado do boletim.';
    END IF;
  END IF;
END
```
