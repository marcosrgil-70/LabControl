# TG_PRODAN_BI

Análise herda empresa/proposta do item; bloqueia revisão antiga; calcula subtotal, desconto e total da linha.

**Tabela:** [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`cds_ProdAnalises*Change` (Delphi)

Comentário do script: _Cálculo dos valores da linha (cds_ProdAnalises*Change do Delphi)_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.ID_LAB_PROPOSTAS`
- `NEW.VR_SUBTOTAL`
- `NEW.VR_DESCONTO`
- `NEW.VR_TOTAL`

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga da proposta é somente leitura."
- "Desconto maior que o subtotal da análise."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md)
- **Chama:** —

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `ID_LAB_PROPOSTAS`, `VR_SUBTOTAL`, `VR_DESCONTO`, `VR_TOTAL`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Código
```sql
CREATE TRIGGER TG_PRODAN_BI BEFORE INSERT ON LAB_PROPOSTAS_PRODANALISES FOR EACH ROW
BEGIN
  DECLARE v_emp, v_prop, v_ultima INT;
  SELECT PP.ID_EMPRESAS, PP.ID_LAB_PROPOSTAS, P.ULTIMA_REVISAO INTO v_emp, v_prop, v_ultima
    FROM LAB_PROPOSTAS_PRODUTOS PP JOIN LAB_PROPOSTAS P ON P.ID_LAB_PROPOSTAS = PP.ID_LAB_PROPOSTAS
   WHERE PP.ID_LAB_PROPOSTAS_PRODUTOS = NEW.ID_LAB_PROPOSTAS_PRODUTOS;
  SET NEW.ID_EMPRESAS = v_emp, NEW.ID_LAB_PROPOSTAS = v_prop;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF v_ultima = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga da proposta é somente leitura.';
    END IF;
    SET NEW.VR_SUBTOTAL = ROUND(NEW.QTDE_AMOSTRAS * NEW.VR_UNITARIO, 2);
    IF NEW.PORC_DESCONTO > 0 THEN
      SET NEW.VR_DESCONTO = ROUND(NEW.VR_SUBTOTAL * NEW.PORC_DESCONTO / 100, 2);
    END IF;
    IF NEW.VR_DESCONTO > NEW.VR_SUBTOTAL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Desconto maior que o subtotal da análise.';
    END IF;
    SET NEW.VR_TOTAL = NEW.VR_SUBTOTAL - NEW.VR_DESCONTO;
  END IF;
END
```
