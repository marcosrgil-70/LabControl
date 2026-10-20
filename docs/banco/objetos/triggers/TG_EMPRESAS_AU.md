# TG_EMPRESAS_AU

Espelha a identidade da empresa (razão social, fantasia, CNPJ, IE, IM, ativa) na entidade da empresa (`ID_ENTIDADES`).

**Tabela:** [`EMPRESAS`](../tabelas/EMPRESAS.md) · **Momento:** AFTER UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
no legado a identidade ficava só na entidade

Comentário do script: _Empresa usuária: a identidade (razão social, fantasia, CNPJ, inscrições, situação) é mantida em EMPRESAS e espelhada na entidade da empresa, que guarda os endereços, telefones e e-mails (EMPRESAS.ID_ENTIDADES)._

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** [`ENTIDADES`](../tabelas/ENTIDADES.md)
- **Lê outras tabelas:** —
- **Chama:** —

## Impacto na aplicação
- —

## Telas e processos relacionados
- **Telas:** [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md) · [T00 Login e seleção de empresa](../../../telas/T00-login-empresa.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · [P23 Nova empresa e cópia de cadastros](../../../processos/P23-nova-empresa.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_EMPRESAS_AU AFTER UPDATE ON EMPRESAS FOR EACH ROW
BEGIN
  IF NEW.ID_ENTIDADES IS NOT NULL THEN
    UPDATE ENTIDADES
       SET NOME = NEW.RAZAO_SOCIAL, NOME_FANTASIA = NEW.NOME_FANTASIA, CPF_CNPJ = NEW.CNPJ,
           RG_IE = NEW.INSC_ESTADUAL, INSC_MUNICIPAL = NEW.INSC_MUNICIPAL, INATIVO = 1 - NEW.ATIVA
     WHERE ID_ENTIDADES = NEW.ID_ENTIDADES;
  END IF;
END
```
