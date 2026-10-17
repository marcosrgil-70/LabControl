# TG_PROPPROD_AU

Quantidade do produto propaga para todas as análises do item.

**Tabela:** [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) · **Momento:** AFTER UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`cds_PropProdQTDE_AMOSTRASChange` (Delphi)

Comentário do script: _Alterar a quantidade de amostras do produto altera a quantidade das análises (cds_PropProdQTDE_AMOSTRASChange do Delphi)_

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Lê outras tabelas:** —
- **Chama:** —

## Impacto na aplicação
- —

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Código
```sql
CREATE TRIGGER TG_PROPPROD_AU AFTER UPDATE ON LAB_PROPOSTAS_PRODUTOS FOR EACH ROW
BEGIN
  IF NEW.QTDE_AMOSTRAS <> OLD.QTDE_AMOSTRAS THEN
    UPDATE LAB_PROPOSTAS_PRODANALISES SET QTDE_AMOSTRAS = NEW.QTDE_AMOSTRAS
     WHERE ID_LAB_PROPOSTAS_PRODUTOS = NEW.ID_LAB_PROPOSTAS_PRODUTOS;
  END IF;
END
```
