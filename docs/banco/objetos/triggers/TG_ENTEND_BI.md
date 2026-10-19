# TG_ENTEND_BI

Endereço herda `ID_EMPRESAS` da entidade.

**Tabela:** [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _Entidades: filhos herdam ID_EMPRESAS do pai_

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
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Código
```sql
CREATE TRIGGER TG_ENTEND_BI BEFORE INSERT ON ENTIDADES_ENDERECOS FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM ENTIDADES WHERE ID_ENTIDADES = NEW.ID_ENTIDADES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
END
```
