# TG_PEDVEN_BI

Número sequencial do pedido de venda (por empresa, não anual) quando não informado.

**Tabela:** [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GET_ULT_PED_VENDA`

Comentário do script: _Pedido de Venda: número sequencial por empresa (não anual)_

## Colunas que a trigger preenche/ajusta
- `NEW.NR_PEDIDO`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Chama:** [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `NR_PEDIDO`. Reler a linha depois de gravar.
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)

## Código
```sql
CREATE TRIGGER TG_PEDVEN_BI BEFORE INSERT ON PEDIDOS_VENDA FOR EACH ROW
BEGIN
  DECLARE v_num INT;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, (SELECT ID_EMPRESAS FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS));
  -- número SEMPRE do banco; só na migração o número legado (ID_PEDIDOS_VENDAS) é informado e preservado
  IF COALESCE(@LC_MIGRACAO, 0) = 0 OR COALESCE(NEW.NR_PEDIDO, 0) = 0 THEN
    CALL SP_PROXIMO_NUMERO(NEW.ID_EMPRESAS, 'PEDIDO_VENDA', 0, v_num);
    SET NEW.NR_PEDIDO = v_num;
  END IF;
END
```
