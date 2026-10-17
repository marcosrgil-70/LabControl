# SP_AMOSTRA_GERAR_TESTES

Apaga e recria os testes da amostra a partir das análises do item da proposta (com data prevista). Recusa se houver resultado.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`Grava_Hist_Amostra_Testes` (Delphi)

Comentário do script: _SP_AMOSTRA_GERAR_TESTES — copia os parâmetros do item da proposta para a amostra (Grava_Hist_Amostra_Testes do Delphi). Recusa se já houver resultado. Sem transação (a aplicação chama dentro da transação de gravação da amostra)._

## Assinatura
```sql
CREATE PROCEDURE SP_AMOSTRA_GERAR_TESTES(IN p_id_amostra INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_amostra` | INT |

## Quem chama
- **Uso previsto:** Aplicação, logo após gravar a amostra (inclusão e alteração sem resultado), **na mesma transação**.
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Sem transação (chamar dentro da transação da gravação da amostra).

## O que acessa
- **Altera:** [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md)
- **Lê:** [`PRAZOS`](../tabelas/PRAZOS.md), [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md), [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md), [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Amostra não encontrada."
- "Existem resultados para esta amostra: os parâmetros não podem ser regerados a partir da proposta."

## Processos
- [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_AMOSTRA_GERAR_TESTES";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_amostra", id_amostra));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_AMOSTRA_GERAR_TESTES(IN p_id_amostra INT)
BEGIN
  DECLARE v_emp, v_item INT;
  DECLARE v_dt DATE;

  SELECT ID_EMPRESAS, ID_LAB_PROPOSTAS_PRODUTOS, DT_ENTREGA INTO v_emp, v_item, v_dt
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra não encontrada.';
  END IF;
  IF EXISTS (SELECT 1 FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Existem resultados para esta amostra: os parâmetros não podem ser regerados a partir da proposta.';
  END IF;

  DELETE FROM LAB_HIST_AMOSTRAS_TESTES WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;

  INSERT INTO LAB_HIST_AMOSTRAS_TESTES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_TIPO, ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, DT_PREVISTA)
  SELECT v_emp, p_id_amostra, PA.ID_LAB_PROPOSTAS_PRODANALISES, PA.ID_LAB_PARAMETROS_ANALISES,
         P.ID_ANALISES_TIPO, PA.ID_ANALISES_METODOS, PA.ID_IDIOMAS, PA.ID_PRAZOS,
         DATE_ADD(v_dt, INTERVAL COALESCE(Z.QTDE_DIAS, 0) DAY)
    FROM LAB_PROPOSTAS_PRODANALISES PA
    JOIN LAB_PARAMETROS_ANALISES P ON P.ID_LAB_PARAMETROS_ANALISES = PA.ID_LAB_PARAMETROS_ANALISES
    LEFT JOIN PRAZOS Z ON Z.ID_PRAZOS = PA.ID_PRAZOS
   WHERE PA.ID_LAB_PROPOSTAS_PRODUTOS = v_item
   ORDER BY PA.ID_LAB_PROPOSTAS_PRODANALISES;
END
```
