# TG_BOLETIM_BU

Boletim emitido congelado (só o status muda); revisão antiga somente leitura; código/revisão/amostra imutáveis.

**Tabela:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) · **Momento:** BEFORE UPDATE · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_BOLETIM_ANALISES_BU0` (nunca funcionou: `DH_EMISSAO > 0`)

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Este boletim foi emitido. Não é permitido alterar ou excluir."
- "Revisão antiga do boletim é somente leitura."
- "Código/revisão/amostra do boletim não podem ser alterados."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** —
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Código
```sql
CREATE TRIGGER TG_BOLETIM_BU BEFORE UPDATE ON LAB_BOLETIM_ANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NOT (NEW.ULTIMA_REVISAO <> OLD.ULTIMA_REVISAO) THEN
    IF OLD.DH_EMISSAO IS NOT NULL THEN
      -- emitido: só o status pode mudar (ex.: Enviado ao cliente)
      IF NOT (NEW.ID_LAB_RESULTADOS_ANALISES <=> OLD.ID_LAB_RESULTADOS_ANALISES
          AND NEW.DT_BOLETIM <=> OLD.DT_BOLETIM AND NEW.DH_EMISSAO <=> OLD.DH_EMISSAO
          AND NEW.ID_ENTIDADES_FUNC_RESP <=> OLD.ID_ENTIDADES_FUNC_RESP
          AND NEW.ID_ENTIDADES_FUNC_RESP2 <=> OLD.ID_ENTIDADES_FUNC_RESP2
          AND NEW.TXT_OBSERVACAO <=> OLD.TXT_OBSERVACAO AND NEW.TXT_LEGENDA <=> OLD.TXT_LEGENDA
          AND NEW.TXT_METODOLOGIA_UTIL <=> OLD.TXT_METODOLOGIA_UTIL
          AND NEW.TXT_METODOLOGIA_ACRED <=> OLD.TXT_METODOLOGIA_ACRED
          AND NEW.ARQUIVO_PDF <=> OLD.ARQUIVO_PDF AND NEW.HASH_PDF <=> OLD.HASH_PDF) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este boletim foi emitido. Não é permitido alterar ou excluir.';
      END IF;
    ELSEIF OLD.ULTIMA_REVISAO = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga do boletim é somente leitura.';
    END IF;
    IF NEW.COD_BOLETIM <> OLD.COD_BOLETIM OR NEW.REVISAO <> OLD.REVISAO OR NEW.ID_LAB_HIST_AMOSTRAS <> OLD.ID_LAB_HIST_AMOSTRAS THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Código/revisão/amostra do boletim não podem ser alterados.';
    END IF;
  END IF;
END
```
