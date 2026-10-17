# FN_NR_PROPOSTA

Formata o número da proposta `000123/26-R0`.

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
montagem na tela

Comentário do script: _FN_NR_PROPOSTA — 000123/26-R0_

## Assinatura
```sql
CREATE FUNCTION FN_NR_PROPOSTA(p_cod INT, p_ano INT, p_rev INT)
RETURNS VARCHAR(20) DETERMINISTIC NO SQL
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_cod` | INT |
| IN | `p_ano` | INT |
| IN | `p_rev` | INT |

## Quem chama
- **Uso previsto:** Triggers `TG_PROPOSTAS_BI/BU`.
- **Chamada por rotinas:** —
- **Chamada por triggers:** [`TG_PROPOSTAS_BI`](../triggers/TG_PROPOSTAS_BI.md), [`TG_PROPOSTAS_BU`](../triggers/TG_PROPOSTAS_BU.md)
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** —
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_NR_PROPOSTA({0}, {1}, {2}) AS Value", cod, ano, rev).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_NR_PROPOSTA(p_cod INT, p_ano INT, p_rev INT)
RETURNS VARCHAR(20) DETERMINISTIC NO SQL
BEGIN
  RETURN CONCAT(LPAD(COALESCE(p_cod, 0), 6, '0'), '/', LPAD(MOD(COALESCE(p_ano, 0), 100), 2, '0'),
                '-R', COALESCE(p_rev, 0));
END
```
