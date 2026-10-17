# TG_PROPOSTAS_BI

Numeração da proposta (ano do servidor + sequência anual), data de solicitação padrão, validade ≥ solicitação e `NR_PROPOSTA`.

**Tabela:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_GET_COD_PROPOSTAS` chamado pela tela (com `COD_PROPOSTA = ID` provisório)

Comentário do script: _Propostas_

## Colunas que a trigger preenche/ajusta
- `NEW.ANO_PROPOSTA`
- `NEW.COD_PROPOSTA`
- `NEW.DT_SOLICITACAO`
- `NEW.NR_PROPOSTA`

## Validações (mensagens SQLSTATE 45000)
- "Data de validade anterior à data de solicitação."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** [`FN_NR_PROPOSTA`](../rotinas/FN_NR_PROPOSTA.md), [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ANO_PROPOSTA`, `COD_PROPOSTA`, `DT_SOLICITACAO`, `NR_PROPOSTA`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md) · [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Código
```sql
CREATE TRIGGER TG_PROPOSTAS_BI BEFORE INSERT ON LAB_PROPOSTAS FOR EACH ROW
BEGIN
  DECLARE v_num INT;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.REV_PROPOSTA = 0 AND COALESCE(NEW.COD_PROPOSTA, 0) = 0 THEN
    -- ano SEMPRE do servidor; a sequência é por (empresa, ano) e nunca é "zerada"
    SET NEW.ANO_PROPOSTA = YEAR(CURDATE());
    CALL SP_PROXIMO_NUMERO(NEW.ID_EMPRESAS, 'PROPOSTA', NEW.ANO_PROPOSTA, v_num);
    SET NEW.COD_PROPOSTA = v_num;
  END IF;
  IF NEW.DT_SOLICITACAO IS NULL THEN
    SET NEW.DT_SOLICITACAO = CURDATE();
  END IF;
  IF NEW.DT_VALIDADE IS NOT NULL AND NEW.DT_VALIDADE < NEW.DT_SOLICITACAO THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de validade anterior à data de solicitação.';
  END IF;
  SET NEW.NR_PROPOSTA = FN_NR_PROPOSTA(NEW.COD_PROPOSTA, NEW.ANO_PROPOSTA, NEW.REV_PROPOSTA);
END
```
