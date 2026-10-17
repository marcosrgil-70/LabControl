# FN_FORMATA_RESULTADO

Monta o texto do resultado/especificação para o boletim: numérico `"< 1,0e2 UFC/g"` ou descritivo (descrição do tipo de resultado).

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
CASE em `qry_Param` e em `SP_GERAR_REVISAO_BOLETIM` (truncava em 15 caracteres)

Comentário do script: _FN_FORMATA_RESULTADO — texto do resultado/especificação para o boletim.   numérico  : "<simbolo> <valor>e<exp> <unidade>"  ex.: "< 1,0e2 UFC/g"   descritivo: descrição do LAB_TIPOS_RESULTADOS   ex.: "Ausência em 25 g" (No Firebird: CASE em qry_Param e em SP_GERAR_REVISAO_BOLETIM.)_

## Assinatura
```sql
CREATE FUNCTION FN_FORMATA_RESULTADO(p_tipo TINYINT, p_simbolo VARCHAR(2), p_valor VARCHAR(20),
                                     p_exp SMALLINT, p_id_unidade INT, p_id_tipo_res INT)
RETURNS VARCHAR(80) READS SQL DATA
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_tipo` | TINYINT |
| IN | `p_simbolo` | VARCHAR(2) |
| IN | `p_valor` | VARCHAR(20) |
| IN | `p_exp` | SMALLINT |
| IN | `p_id_unidade` | INT |
| IN | `p_id_tipo_res` | INT |

## Quem chama
- **Uso previsto:** Triggers `TG_RESPAR_BI/BU`.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_RESPAR_BI`](../triggers/TG_RESPAR_BI.md), [`TG_RESPAR_BU`](../triggers/TG_RESPAR_BU.md)
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** [`UNIDADES`](../tabelas/UNIDADES.md), [`LAB_TIPOS_RESULTADOS`](../tabelas/LAB_TIPOS_RESULTADOS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md)
- [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Notas
- O PDF transforma `e2` em "× 10²" (expoente sobrescrito).

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_FORMATA_RESULTADO({0}, {1}, {2}, {3}, {4}, {5}) AS Value", tipo, simbolo, valor, exp, id_unidade, id_tipo_res).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_FORMATA_RESULTADO(p_tipo TINYINT, p_simbolo VARCHAR(2), p_valor VARCHAR(20),
                                     p_exp SMALLINT, p_id_unidade INT, p_id_tipo_res INT)
RETURNS VARCHAR(80) READS SQL DATA
BEGIN
  DECLARE v_txt VARCHAR(80) DEFAULT '';
  DECLARE v_uni VARCHAR(15);
  IF p_tipo = 1 THEN
    IF COALESCE(TRIM(p_valor), '') = '' THEN
      RETURN NULL;
    END IF;
    SET v_txt = TRIM(p_valor);
    IF COALESCE(p_exp, 0) <> 0 THEN
      SET v_txt = CONCAT(v_txt, 'e', p_exp);
    END IF;
    IF COALESCE(TRIM(p_simbolo), '') <> '' THEN
      SET v_txt = CONCAT(TRIM(p_simbolo), ' ', v_txt);
    END IF;
    IF p_id_unidade IS NOT NULL THEN
      SELECT SIGLA INTO v_uni FROM UNIDADES WHERE ID_UNIDADES = p_id_unidade;
      IF v_uni IS NOT NULL AND v_uni <> '' THEN
        SET v_txt = CONCAT(v_txt, ' ', v_uni);
      END IF;
    END IF;
    RETURN v_txt;
  END IF;
  IF p_id_tipo_res IS NULL THEN
    RETURN NULL;
  END IF;
  SELECT DESCRICAO INTO v_txt FROM LAB_TIPOS_RESULTADOS WHERE ID_LAB_TIPOS_RESULTADOS = p_id_tipo_res;
  RETURN v_txt;
END
```
