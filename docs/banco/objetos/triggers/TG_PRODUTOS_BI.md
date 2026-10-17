# TG_PRODUTOS_BI

Código automático do produto (6 dígitos) quando vazio.

**Tabela:** [`PRODUTOS`](../tabelas/PRODUTOS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_SYS_GERAR_CODIGO` / `SYS_TABELAS_ID`

Comentário do script: _Produtos: código automático (SYS_TABELAS_ID 'PRODUTOS' no legado)_

## Colunas que a trigger preenche/ajusta
- `NEW.CODIGO`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `CODIGO`. Reler a linha depois de gravar.

## Telas e processos relacionados
- **Telas:** [T14 Produtos](../../../telas/T14-produtos.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Código
```sql
CREATE TRIGGER TG_PRODUTOS_BI BEFORE INSERT ON PRODUTOS FOR EACH ROW
BEGIN
  DECLARE v_num INT;
  IF COALESCE(TRIM(NEW.CODIGO), '') = '' THEN
    CALL SP_PROXIMO_NUMERO(NEW.ID_EMPRESAS, 'PRODUTO', 0, v_num);
    SET NEW.CODIGO = LPAD(v_num, 6, '0');
  END IF;
END
```
