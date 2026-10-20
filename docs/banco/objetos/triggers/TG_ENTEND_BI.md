# TG_ENTEND_BI

Endereço herda `ID_EMPRESAS` da entidade; um endereço ativo por tipo (`PR_CONFERIR_ENDERECO`).

**Tabela:** [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
UNIQUE (entidade, tipo) do Firebird

Comentário do script: _Contatos das entidades: filhos herdam ID_EMPRESAS do pai; um endereço ativo por tipo; um telefone e um e-mail principal (inativo não é principal)._

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`ENTIDADES`](../tabelas/ENTIDADES.md)
- **Chama:** [`PR_CONFERIR_ENDERECO`](../rotinas/PR_CONFERIR_ENDERECO.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENTEND_BI BEFORE INSERT ON ENTIDADES_ENDERECOS FOR EACH ROW
BEGIN
  DECLARE v_emp INT;
  SELECT ID_EMPRESAS INTO v_emp FROM ENTIDADES WHERE ID_ENTIDADES = NEW.ID_ENTIDADES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    CALL PR_CONFERIR_ENDERECO(NULL, NEW.ID_ENTIDADES, NEW.ID_ENDERECOS_TIPOS, NEW.INATIVO);
  END IF;
END
```
