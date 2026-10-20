# TG_ENT_BI

Empresa usuária não coexiste com outro tipo de cadastro.

**Tabela:** [`ENTIDADES`](../tabelas/ENTIDADES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
un_Entidades (teEMP)

Comentário do script: _Entidades: a empresa usuária não coexiste com outro tipo (legado teEMP) e não deixa de ser empresa usuária enquanto a empresa apontar para ela._

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`PR_CONFERIR_TIPOS_ENTIDADE`](../rotinas/PR_CONFERIR_TIPOS_ENTIDADE.md)

## Impacto na aplicação
- —

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENT_BI BEFORE INSERT ON ENTIDADES FOR EACH ROW
BEGIN
  CALL PR_CONFERIR_TIPOS_ENTIDADE(NEW.TIPO_EMPRESA_USUARIA,
       NEW.TIPO_CLIENTE + NEW.TIPO_FORNECEDOR + NEW.TIPO_FUNCIONARIO + NEW.TIPO_VENDEDOR + NEW.TIPO_TRANSPORTADOR);
END
```
