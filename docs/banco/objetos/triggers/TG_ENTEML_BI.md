# TG_ENTEML_BI

E-mail/contato herda `ID_EMPRESAS` da entidade.

**Tabela:** [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

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
- **Chama:** —

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md)

## Código
```sql
CREATE TRIGGER TG_ENTEML_BI BEFORE INSERT ON ENTIDADES_EMAILS FOR EACH ROW
BEGIN
  SET NEW.ID_EMPRESAS = (SELECT ID_EMPRESAS FROM ENTIDADES WHERE ID_ENTIDADES = NEW.ID_ENTIDADES);
END
```
