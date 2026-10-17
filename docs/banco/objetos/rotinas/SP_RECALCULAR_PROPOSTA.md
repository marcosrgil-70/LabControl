# SP_RECALCULAR_PROPOSTA

Recalcula os totais da proposta a partir das análises e do desconto geral.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`TotalizarAnalises` (Delphi)

Comentário do script: _SP_RECALCULAR_PROPOSTA — totais do cabeçalho (TotalizarAnalises do Delphi)   VR_SOMA_* = Σ das análises; se PORC_DESCONTO > 0 recalcula VR_DESCONTO;   VR_TOTAL_PROPOSTA = VR_SOMA_VR_TOT_ANALIS − VR_DESCONTO. Chamada pelas triggers de LAB_PROPOSTAS_PRODANALISES. Sem transação._

## Assinatura
```sql
CREATE PROCEDURE SP_RECALCULAR_PROPOSTA(IN p_id_proposta INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_proposta` | INT |

## Quem chama
- **Uso previsto:** Triggers `TG_PRODAN_AI/AU/AD`; `PR_REVISAO_PROPOSTA`. A aplicação pode chamar após mudar `PORC_DESCONTO`/`VR_DESCONTO` do cabeçalho.
- **Chamada por rotinas:** [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md)
- **Chamada por triggers:** [`TG_PRODAN_AI`](../triggers/TG_PRODAN_AI.md), [`TG_PRODAN_AU`](../triggers/TG_PRODAN_AU.md), [`TG_PRODAN_AD`](../triggers/TG_PRODAN_AD.md)
- **Usada em views:** —

## Transação
Sem transação (pode ser chamada dentro da transação da aplicação).

## O que acessa
- **Altera:** [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md)
- **Lê:** [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_RECALCULAR_PROPOSTA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_proposta", id_proposta));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_RECALCULAR_PROPOSTA(IN p_id_proposta INT)
BEGIN
  DECLARE v_sub, v_desc, v_tot DECIMAL(15,2);
  SELECT COALESCE(SUM(VR_SUBTOTAL), 0), COALESCE(SUM(VR_DESCONTO), 0), COALESCE(SUM(VR_TOTAL), 0)
    INTO v_sub, v_desc, v_tot
    FROM LAB_PROPOSTAS_PRODANALISES WHERE ID_LAB_PROPOSTAS = p_id_proposta;

  UPDATE LAB_PROPOSTAS
     SET VR_SOMA_VR_ANALIS     = v_sub,
         VR_SOMA_DESC_ANALIS   = v_desc,
         VR_SOMA_VR_TOT_ANALIS = v_tot,
         VR_DESCONTO           = IF(PORC_DESCONTO > 0, ROUND(v_tot * PORC_DESCONTO / 100, 2), VR_DESCONTO),
         VR_TOTAL_PROPOSTA     = v_tot - IF(PORC_DESCONTO > 0, ROUND(v_tot * PORC_DESCONTO / 100, 2), VR_DESCONTO)
   WHERE ID_LAB_PROPOSTAS = p_id_proposta;
END
```
