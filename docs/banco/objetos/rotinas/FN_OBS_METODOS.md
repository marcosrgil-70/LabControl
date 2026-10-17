# FN_OBS_METODOS

Texto das metodologias do boletim ("DESCR_METODO: método"), separado por acreditado (1) e não acreditado (0).

**Tipo:** Função · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`SP_OBSMETODOS` (buscava o método na proposta pelo parâmetro — podia trazer o de outro produto)

Comentário do script: _FN_OBS_METODOS — texto de metodologias para o boletim (SP_OBSMETODOS). Correção: usa o método do TESTE da amostra (o legado buscava na proposta pelo parâmetro e podia trazer métodos de outro produto da mesma proposta)._

## Assinatura
```sql
CREATE FUNCTION FN_OBS_METODOS(p_id_amostra INT, p_auditado TINYINT)
RETURNS TEXT READS SQL DATA
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_amostra` | INT |
| IN | `p_auditado` | TINYINT |

## Quem chama
- **Uso previsto:** `PR_REVISAO_BOLETIM`; sugestão dos textos na tela de boletim.
- **Chamada por rotinas:** [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Chamada por triggers:** —
- **Usada em views:** —

## O que acessa
- **Altera:** —
- **Lê:** [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)
- [P18 Revisão e desfazer de boletim](../../../processos/P18-revisao-boletim.md)

## Como chamar no C#
```csharp
// Função escalar: usada por views/triggers. Se precisar na aplicação, chamar dentro de um SELECT:
var valor = await db.Database.SqlQueryRaw<string>("SELECT FN_OBS_METODOS({0}, {1}) AS Value", id_amostra, auditado).FirstAsync();
```

## Código
```sql
CREATE FUNCTION FN_OBS_METODOS(p_id_amostra INT, p_auditado TINYINT)
RETURNS TEXT READS SQL DATA
BEGIN
  DECLARE v_txt TEXT;
  SELECT GROUP_CONCAT(DISTINCT CONCAT(COALESCE(NULLIF(P.DESCR_METODO, ''), P.DESC_REDUZIDA), ': ', M.DESCRICAO)
                      ORDER BY P.DESC_REDUZIDA SEPARATOR '\r\n')
    INTO v_txt
    FROM LAB_HIST_AMOSTRAS_TESTES T
    JOIN LAB_PARAMETROS_ANALISES P ON P.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
    JOIN ANALISES_METODOS M ON M.ID_ANALISES_METODOS = T.ID_ANALISES_METODOS
   WHERE T.ID_LAB_HIST_AMOSTRAS = p_id_amostra
     AND P.AUDITADO = p_auditado;
  RETURN v_txt;
END
```
