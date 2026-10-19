# TG_PROPPROD_BI

Item herda a empresa da proposta; bloqueia revisão antiga; numera o `ITEM`.

**Tabela:** [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
tela de propostas

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.ITEM`

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga da proposta é somente leitura."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Chama:** —

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `ITEM`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Código
```sql
CREATE TRIGGER TG_PROPPROD_BI BEFORE INSERT ON LAB_PROPOSTAS_PRODUTOS FOR EACH ROW
BEGIN
  DECLARE v_emp, v_ultima INT;
  -- FOR UPDATE: duas inclusões simultâneas na MESMA proposta esperam uma pela outra (ITEM = MAX + 1 sem repetir)
  SELECT ID_EMPRESAS, ULTIMA_REVISAO INTO v_emp, v_ultima FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS FOR UPDATE;
  SET NEW.ID_EMPRESAS = v_emp;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF v_ultima = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga da proposta é somente leitura.';
    END IF;
    IF COALESCE(NEW.ITEM, 0) = 0 THEN
      SET NEW.ITEM = (SELECT COALESCE(MAX(ITEM), 0) + 1 FROM LAB_PROPOSTAS_PRODUTOS WHERE ID_LAB_PROPOSTAS = NEW.ID_LAB_PROPOSTAS);
    END IF;
  END IF;
END
```
