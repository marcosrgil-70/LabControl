# TG_PROPOSTAS_BU

Número/revisão/empresa imutáveis; revisão antiga somente leitura; status "Enviado ao cliente" grava `DT_ENVIO_CLIENTE`; recalcula `NR_PROPOSTA`.

**Tabela:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) · **Momento:** BEFORE UPDATE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`cds_prinID_STATUS_PROPOSTASChange` (Delphi)

## Colunas que a trigger preenche/ajusta
- `NEW.DT_ENVIO_CLIENTE`
- `NEW.NR_PROPOSTA`

## Validações (mensagens SQLSTATE 45000)
- "Número/revisão/empresa da proposta não podem ser alterados."
- "Revisão antiga da proposta é somente leitura."
- "Data de validade anterior à data de solicitação."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md)
- **Chama:** [`FN_NR_PROPOSTA`](../rotinas/FN_NR_PROPOSTA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `DT_ENVIO_CLIENTE`, `NR_PROPOSTA`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md) · [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Código
```sql
CREATE TRIGGER TG_PROPOSTAS_BU BEFORE UPDATE ON LAB_PROPOSTAS FOR EACH ROW
BEGIN
  DECLARE v_envcli INT;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NOT (NEW.ULTIMA_REVISAO <> OLD.ULTIMA_REVISAO) THEN
    IF NEW.ID_EMPRESAS <> OLD.ID_EMPRESAS OR NEW.COD_PROPOSTA <> OLD.COD_PROPOSTA
       OR NEW.ANO_PROPOSTA <> OLD.ANO_PROPOSTA OR NEW.REV_PROPOSTA <> OLD.REV_PROPOSTA THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Número/revisão/empresa da proposta não podem ser alterados.';
    END IF;
    IF OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga da proposta é somente leitura.';
    END IF;
    -- regra do legado (cds_prinID_STATUS_PROPOSTASChange): status "Enviado ao cliente" grava a data
    SELECT ID_LAB_PROPOSTAS_STATUS_ENVCLI INTO v_envcli FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = NEW.ID_EMPRESAS;
    IF NEW.ID_LAB_PROPOSTAS_STATUS <> OLD.ID_LAB_PROPOSTAS_STATUS AND NEW.ID_LAB_PROPOSTAS_STATUS = v_envcli
       AND NEW.DT_ENVIO_CLIENTE IS NULL THEN
      SET NEW.DT_ENVIO_CLIENTE = CURDATE();
    END IF;
    IF NEW.DT_VALIDADE IS NOT NULL AND NEW.DT_VALIDADE < NEW.DT_SOLICITACAO THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de validade anterior à data de solicitação.';
    END IF;
  END IF;
  SET NEW.NR_PROPOSTA = FN_NR_PROPOSTA(NEW.COD_PROPOSTA, NEW.ANO_PROPOSTA, NEW.REV_PROPOSTA);
END
```
