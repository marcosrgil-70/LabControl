# TG_RE_BI

Número anual do relatório de ensaio.

**Tabela:** [`LAB_RELATORIO_ENSAIO`](../tabelas/LAB_RELATORIO_ENSAIO.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_RELATORIO_ENSAIO_BI` (gravava `ULT_COD_RE = ID_EMPRESAS`)

Comentário do script: _Relatório de Ensaio: número anual por empresa (o legado gravava ULT_COD_RE = ID_EMPRESAS por engano)_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.ANO_RELATORIO`
- `NEW.NR_RELATORIO`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Chama:** [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `ANO_RELATORIO`, `NR_RELATORIO`. Reler a linha depois de gravar.
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md)
- **Processos:** [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Código
```sql
CREATE TRIGGER TG_RE_BI BEFORE INSERT ON LAB_RELATORIO_ENSAIO FOR EACH ROW
BEGIN
  DECLARE v_num, v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp; -- a numeração abaixo usa a empresa: nunca 0/NULL
  -- número e ano SEMPRE do banco; só na migração o número legado é informado e preservado
  IF COALESCE(@LC_MIGRACAO, 0) = 0 OR COALESCE(NEW.NR_RELATORIO, 0) = 0 THEN
    SET NEW.ANO_RELATORIO = YEAR(CURDATE());
    CALL SP_PROXIMO_NUMERO(NEW.ID_EMPRESAS, 'RELATORIO_ENSAIO', NEW.ANO_RELATORIO, v_num);
    SET NEW.NR_RELATORIO = v_num;
  ELSEIF COALESCE(NEW.ANO_RELATORIO, 0) = 0 THEN
    SET NEW.ANO_RELATORIO = YEAR(NEW.DH_GERACAO);
  END IF;
END
```
