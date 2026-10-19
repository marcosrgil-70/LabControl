# TG_ENTFUN_BI

Profissional herda `ID_EMPRESAS` da entidade.

**Tabela:** [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`ENTIDADES`](../tabelas/ENTIDADES.md)
- **Chama:** [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.

## Telas e processos relacionados
- **Telas:** [T13 Profissionais](../../../telas/T13-profissionais.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md)

## Código
```sql
CREATE TRIGGER TG_ENTFUN_BI BEFORE INSERT ON ENTIDADES_FUNCIONARIOS FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM ENTIDADES WHERE ID_ENTIDADES = NEW.ID_ENTIDADES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
END
```
