# TG_ENTFON_BI

Telefone herda `ID_EMPRESAS` da entidade; inativo não é principal; um principal por entidade (`PR_CONFERIR_FONE`).

**Tabela:** [`ENTIDADES_FONES`](../tabelas/ENTIDADES_FONES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.PRINCIPAL`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`ENTIDADES`](../tabelas/ENTIDADES.md)
- **Chama:** [`PR_CONFERIR_FONE`](../rotinas/PR_CONFERIR_FONE.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `PRINCIPAL`. Reler a linha depois de gravar.
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENTFON_BI BEFORE INSERT ON ENTIDADES_FONES FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM ENTIDADES WHERE ID_ENTIDADES = NEW.ID_ENTIDADES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  IF NEW.INATIVO = 1 THEN SET NEW.PRINCIPAL = 0; END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    CALL PR_CONFERIR_FONE(NULL, NEW.ID_ENTIDADES, NEW.PRINCIPAL);
  END IF;
END
```
