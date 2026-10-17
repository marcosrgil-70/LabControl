# TG_BOLETIM_BD

Emitido não se exclui; revisão > 0 só por "Desfazer revisão".

**Tabela:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) · **Momento:** BEFORE DELETE · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_BOLETIM_ANALISES_BD0/AD0`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- "Este boletim foi emitido. Não é permitido alterar ou excluir."
- "Para excluir uma revisão use "Desfazer revisão"."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_DESFAZENDO` — uso interno de `SP_EXCLUIR_REVISAO_*` para permitir excluir a revisão

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md)
- **Chama:** —

## Impacto na aplicação
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md) · [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Código
```sql
CREATE TRIGGER TG_BOLETIM_BD BEFORE DELETE ON LAB_BOLETIM_ANALISES FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF OLD.DH_EMISSAO IS NOT NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este boletim foi emitido. Não é permitido alterar ou excluir.';
    END IF;
    IF OLD.REVISAO > 0 AND COALESCE(@LC_DESFAZENDO, 0) = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Para excluir uma revisão use "Desfazer revisão".';
    END IF;
  END IF;
  -- filhos (LAB_BOLETIM_PARAMETROS) são removidos pela FK ON DELETE CASCADE
END
```
