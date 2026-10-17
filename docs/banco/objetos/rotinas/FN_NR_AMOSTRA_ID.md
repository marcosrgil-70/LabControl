# FN_NR_AMOSTRA_ID

Igual a `FN_NR_AMOSTRA`, mas recebe os IDs dos tipos e resolve os `CODIGO`s.

**Tipo:** Função · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _Versão por IDs (resolve os CODIGOs dos tipos)_

## Assinatura
```sql
CREATE FUNCTION FN_NR_AMOSTRA_ID(p_id_tipo_amostra INT, p_cod_amostra INT, p_id_tipo_analise INT,
                                 p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) READS SQL DATA
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_tipo_amostra` | INT |
| IN | `p_cod_amostra` | INT |
| IN | `p_id_tipo_analise` | INT |
| IN | `p_ano` | INT |
| IN | `p_revisao` | INT |

## Quem chama
- **Uso previsto:** Triggers `TG_AMOSTRAS_BI/BU`, `TG_BOLETIM_BI`; migração (M-14).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md), [`TG_BOLETIM_BI`](../triggers/TG_BOLETIM_BI.md)
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** [`AMOSTRAS_TIPO`](../tabelas/AMOSTRAS_TIPO.md), [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md)
- **Chama:** [`FN_NR_AMOSTRA`](../rotinas/FN_NR_AMOSTRA.md)
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_NR_AMOSTRA_ID({0}, {1}, {2}, {3}, {4}) AS Value", id_tipo_amostra, cod_amostra, id_tipo_analise, ano, revisao).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_NR_AMOSTRA_ID(p_id_tipo_amostra INT, p_cod_amostra INT, p_id_tipo_analise INT,
                                 p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) READS SQL DATA
BEGIN
  DECLARE v_ta INT;
  DECLARE v_tn INT;
  SELECT CODIGO INTO v_ta FROM AMOSTRAS_TIPO WHERE ID_AMOSTRAS_TIPO = p_id_tipo_amostra;
  SELECT CODIGO INTO v_tn FROM ANALISES_TIPO WHERE ID_ANALISES_TIPO = p_id_tipo_analise;
  RETURN FN_NR_AMOSTRA(v_ta, p_cod_amostra, v_tn, p_ano, p_revisao);
END
```
