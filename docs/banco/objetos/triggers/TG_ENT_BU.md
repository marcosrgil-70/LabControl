# TG_ENT_BU

Empresa usuária não coexiste com outro tipo e não deixa de ser empresa usuária enquanto `EMPRESAS` apontar para ela.

**Tabela:** [`ENTIDADES`](../tabelas/ENTIDADES.md) · **Momento:** BEFORE UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
un_Entidades (teEMP)

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Este cadastro é o da própria empresa e não pode deixar de ser empresa usuária."

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`EMPRESAS`](../tabelas/EMPRESAS.md)
- **Chama:** [`PR_CONFERIR_TIPOS_ENTIDADE`](../rotinas/PR_CONFERIR_TIPOS_ENTIDADE.md)

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).

## Telas e processos relacionados
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · `P25-cadastro-entidades-contatos`

## Código
```sql
CREATE TRIGGER TG_ENT_BU BEFORE UPDATE ON ENTIDADES FOR EACH ROW
BEGIN
  CALL PR_CONFERIR_TIPOS_ENTIDADE(NEW.TIPO_EMPRESA_USUARIA,
       NEW.TIPO_CLIENTE + NEW.TIPO_FORNECEDOR + NEW.TIPO_FUNCIONARIO + NEW.TIPO_VENDEDOR + NEW.TIPO_TRANSPORTADOR);
  IF OLD.TIPO_EMPRESA_USUARIA = 1 AND NEW.TIPO_EMPRESA_USUARIA = 0
     AND EXISTS (SELECT 1 FROM EMPRESAS WHERE ID_ENTIDADES = OLD.ID_ENTIDADES) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este cadastro é o da própria empresa e não pode deixar de ser empresa usuária.';
  END IF;
END
```
