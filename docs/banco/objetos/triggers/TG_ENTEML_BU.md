# TG_ENTEML_BU

E-mail não muda de entidade/empresa; minúsculas; inativo não é principal; um principal por entidade.

**Tabela:** [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) · **Momento:** BEFORE UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- `NEW.EMAIL`
- `NEW.PRINCIPAL`

## Validações (mensagens SQLSTATE 45000)
- "Não é permitido mover o e-mail para outro cadastro."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`PR_CONFERIR_EMAIL`](../rotinas/PR_CONFERIR_EMAIL.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `EMAIL`, `PRINCIPAL`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENTEML_BU BEFORE UPDATE ON ENTIDADES_EMAILS FOR EACH ROW
BEGIN
  IF NEW.ID_ENTIDADES <> OLD.ID_ENTIDADES OR NEW.ID_EMPRESAS <> OLD.ID_EMPRESAS THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não é permitido mover o e-mail para outro cadastro.';
  END IF;
  SET NEW.EMAIL = LOWER(TRIM(NEW.EMAIL));
  IF NEW.INATIVO = 1 THEN SET NEW.PRINCIPAL = 0; END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    CALL PR_CONFERIR_EMAIL(NEW.ID_ENTIDADES_EMAILS, NEW.ID_ENTIDADES, NEW.PRINCIPAL);
  END IF;
END
```
