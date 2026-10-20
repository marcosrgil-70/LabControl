# TG_ENTEND_BU

Endereço não muda de entidade/empresa; um endereço ativo por tipo.

**Tabela:** [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) · **Momento:** BEFORE UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Não é permitido mover o endereço para outro cadastro."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`PR_CONFERIR_ENDERECO`](../rotinas/PR_CONFERIR_ENDERECO.md)

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENTEND_BU BEFORE UPDATE ON ENTIDADES_ENDERECOS FOR EACH ROW
BEGIN
  IF NEW.ID_ENTIDADES <> OLD.ID_ENTIDADES OR NEW.ID_EMPRESAS <> OLD.ID_EMPRESAS THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não é permitido mover o endereço para outro cadastro.';
  END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    CALL PR_CONFERIR_ENDERECO(NEW.ID_ENTIDADES_ENDERECOS, NEW.ID_ENTIDADES, NEW.ID_ENDERECOS_TIPOS, NEW.INATIVO);
  END IF;
END
```
