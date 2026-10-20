# PR_CONFERIR_EMPRESA

Recusa (45000 "Registro pertence a outra empresa.") um filho gravado com `ID_EMPRESAS` diferente da empresa do pai; 0/NULL = não informado.

**Tipo:** Procedure interna (PR_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _PR_CONFERIR_EMPRESA — usada nas triggers BEFORE INSERT dos filhos, que herdam a empresa do pai: recusa ID_EMPRESAS informado diferente da empresa do pai (0/NULL = não informado, a trigger preenche)._

## Assinatura
```sql
CREATE PROCEDURE PR_CONFERIR_EMPRESA(IN p_informada INT, IN p_do_pai INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_informada` | INT |
| IN | `p_do_pai` | INT |

## Quem chama
- **Uso previsto:** Triggers BEFORE INSERT dos filhos que herdam a empresa do pai (`TG_ENT*_BI`, `TG_PROPPROD_BI`, `TG_PRODAN_BI`, `TG_TESTES_BI`, `TG_RESULT_BI`, `TG_RESPAR_BI`, `TG_BOLETIM_BI`, `TG_BOLPAR_BI`, `TG_PEDVEN_BI`, `TG_RE_BI`).
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_ENTEND_BI`](../triggers/TG_ENTEND_BI.md), [`TG_ENTFON_BI`](../triggers/TG_ENTFON_BI.md), [`TG_ENTEML_BI`](../triggers/TG_ENTEML_BI.md), [`TG_ENTFUN_BI`](../triggers/TG_ENTFUN_BI.md), [`TG_PROPPROD_BI`](../triggers/TG_PROPPROD_BI.md), [`TG_PRODAN_BI`](../triggers/TG_PRODAN_BI.md), [`TG_PEDVEN_BI`](../triggers/TG_PEDVEN_BI.md), [`TG_TESTES_BI`](../triggers/TG_TESTES_BI.md), [`TG_MOV_BI`](../triggers/TG_MOV_BI.md), [`TG_RESULT_BI`](../triggers/TG_RESULT_BI.md), [`TG_RESPAR_BI`](../triggers/TG_RESPAR_BI.md), [`TG_BOLETIM_BI`](../triggers/TG_BOLETIM_BI.md), [`TG_BOLPAR_BI`](../triggers/TG_BOLPAR_BI.md), [`TG_RE_BI`](../triggers/TG_RE_BI.md)
- **Usada em views:** —

## Transação
Interna, sem transação.

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Registro pertence a outra empresa."

## Processos
- [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "PR_CONFERIR_EMPRESA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_informada", informada));
cmd.Parameters.Add(new MySqlParameter("p_do_pai", do_pai));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE PR_CONFERIR_EMPRESA(IN p_informada INT, IN p_do_pai INT)
BEGIN
  IF COALESCE(p_informada, 0) <> 0 AND p_do_pai IS NOT NULL AND p_informada <> p_do_pai THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro pertence a outra empresa.';
  END IF;
END
```
