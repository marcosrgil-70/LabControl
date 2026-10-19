# TG_RESPAR_BI

Teste da mesma amostra; revisão antiga bloqueada; numérico × descritivo exclusivos; zeros → NULL; textos formatados.

**Tabela:** [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) · **Momento:** BEFORE INSERT · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_RESULTADOS_PARAM_BIU0` + `cds_ResultadosTIPO_RESULTADOChange`

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`
- `NEW.ID_UNIDADES`
- `NEW.ID_UNIDADES_AMOSTRA`
- `NEW.ID_UNIDADES_ESPECIF`
- `NEW.ID_LAB_TIPOS_RESULTADOS`
- `NEW.ID_LAB_TIPO_RESULTADOS_ESP`
- `NEW.VR_RESULTADO`
- `NEW.VR_EXP_RESULTADO`
- `NEW.SIMBOLO_GRANDEZA`
- `NEW.VR_RESULTADO_ESPECIF`
- `NEW.VR_EXP_ESPECIF`
- `NEW.SIMBOLO_GR_ESPECIF`
- `NEW.RESULTADO_FORMATADO`
- `NEW.ESPECIF_FORMATADA`

## Validações (mensagens SQLSTATE 45000)
- "Revisão antiga do resultado é somente leitura."
- "O parâmetro não pertence à amostra do resultado."

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Chama:** [`FN_FORMATA_RESULTADO`](../rotinas/FN_FORMATA_RESULTADO.md), [`PR_CONFERIR_EMPRESA`](../rotinas/PR_CONFERIR_EMPRESA.md)

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`, `ID_UNIDADES`, `ID_UNIDADES_AMOSTRA`, `ID_UNIDADES_ESPECIF`, `ID_LAB_TIPOS_RESULTADOS`, `ID_LAB_TIPO_RESULTADOS_ESP`, `VR_RESULTADO`, `VR_EXP_RESULTADO`, `SIMBOLO_GRANDEZA`, `VR_RESULTADO_ESPECIF`, `VR_EXP_ESPECIF`, `SIMBOLO_GR_ESPECIF`, `RESULTADO_FORMATADO`, `ESPECIF_FORMATADA`. Reler a linha depois de gravar.
- Capturar `MySqlException` com `SqlState == "45000"` e mostrar a mensagem ao usuário (é regra de negócio, não erro técnico).
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T30-04 Parâmetros analisados](../../../telas/T30-04-rel-parametros-analisados.md) · [T30-05 Tempo das análises](../../../telas/T30-05-rel-tempo-analises.md) · [T30-06 Produção por período](../../../telas/T30-06-rel-producao.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Código
```sql
CREATE TRIGGER TG_RESPAR_BI BEFORE INSERT ON LAB_RESULTADOS_PARAM FOR EACH ROW
BEGIN
  DECLARE v_emp, v_amo, v_amo_teste, v_ultima INT;
  SELECT ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ULTIMA_REVISAO INTO v_emp, v_amo, v_ultima
    FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = NEW.ID_LAB_RESULTADOS_ANALISES;
  CALL PR_CONFERIR_EMPRESA(NEW.ID_EMPRESAS, v_emp);
  SET NEW.ID_EMPRESAS = v_emp;
  -- zero -> NULL (LAB_RESULTADOS_PARAM_BIU0 do legado)
  IF NEW.ID_UNIDADES = 0 THEN SET NEW.ID_UNIDADES = NULL; END IF;
  IF NEW.ID_UNIDADES_AMOSTRA = 0 THEN SET NEW.ID_UNIDADES_AMOSTRA = NULL; END IF;
  IF NEW.ID_UNIDADES_ESPECIF = 0 THEN SET NEW.ID_UNIDADES_ESPECIF = NULL; END IF;
  IF NEW.ID_LAB_TIPOS_RESULTADOS = 0 THEN SET NEW.ID_LAB_TIPOS_RESULTADOS = NULL; END IF;
  IF NEW.ID_LAB_TIPO_RESULTADOS_ESP = 0 THEN SET NEW.ID_LAB_TIPO_RESULTADOS_ESP = NULL; END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    IF v_ultima = 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão antiga do resultado é somente leitura.';
    END IF;
    SELECT ID_LAB_HIST_AMOSTRAS INTO v_amo_teste FROM LAB_HIST_AMOSTRAS_TESTES
     WHERE ID_LAB_HIST_AMOSTRAS_TESTES = NEW.ID_LAB_HIST_AMOSTRAS_TESTES;
    IF v_amo_teste IS NULL OR v_amo_teste <> v_amo THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O parâmetro não pertence à amostra do resultado.';
    END IF;
    -- regra da tela (cds_ResultadosTIPO_RESULTADOChange): numérico x descritivo são exclusivos
    IF NEW.TIPO_RESULTADO = 1 THEN SET NEW.ID_LAB_TIPOS_RESULTADOS = NULL;
    ELSE SET NEW.VR_RESULTADO = NULL, NEW.VR_EXP_RESULTADO = NULL, NEW.ID_UNIDADES = NULL, NEW.SIMBOLO_GRANDEZA = NULL;
    END IF;
    IF NEW.TIPO_RESULTADO_ESPECIF = 1 THEN SET NEW.ID_LAB_TIPO_RESULTADOS_ESP = NULL;
    ELSE SET NEW.VR_RESULTADO_ESPECIF = NULL, NEW.VR_EXP_ESPECIF = NULL, NEW.ID_UNIDADES_ESPECIF = NULL, NEW.SIMBOLO_GR_ESPECIF = NULL;
    END IF;
  END IF;
  SET NEW.RESULTADO_FORMATADO = FN_FORMATA_RESULTADO(NEW.TIPO_RESULTADO, NEW.SIMBOLO_GRANDEZA, NEW.VR_RESULTADO,
                                                     NEW.VR_EXP_RESULTADO, NEW.ID_UNIDADES, NEW.ID_LAB_TIPOS_RESULTADOS);
  SET NEW.ESPECIF_FORMATADA = FN_FORMATA_RESULTADO(NEW.TIPO_RESULTADO_ESPECIF, NEW.SIMBOLO_GR_ESPECIF, NEW.VR_RESULTADO_ESPECIF,
                                                   NEW.VR_EXP_ESPECIF, NEW.ID_UNIDADES_ESPECIF, NEW.ID_LAB_TIPO_RESULTADOS_ESP);
END
```
