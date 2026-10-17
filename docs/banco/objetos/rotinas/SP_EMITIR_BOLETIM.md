# SP_EMITIR_BOLETIM

Emite (congela) o boletim: grava data de emissão, emissor, status emitido, caminho e hash do PDF.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (o legado nunca gravava `DH_EMISSAO`; a trava `ERR_BOLETIM_EMITIDO` nunca funcionou)

Comentário do script: _SP_EMITIR_BOLETIM — congela o boletim (o legado nunca preenchia DH_EMISSAO: 0 de 5.312 boletins emitidos, logo a trava ERR_BOLETIM_EMITIDO nunca funcionou)._

## Assinatura
```sql
CREATE PROCEDURE SP_EMITIR_BOLETIM(IN p_id_boletim INT, IN p_id_func INT, IN p_arquivo VARCHAR(255), IN p_hash CHAR(64))
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_boletim` | INT |
| IN | `p_id_func` | INT |
| IN | `p_arquivo` | VARCHAR(255) |
| IN | `p_hash` | CHAR(64) |

## Quem chama
- **Uso previsto:** Botão "Emitir" da T27 (`BOLETINS.EMITIR`), depois de gerar e salvar o PDF.
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
**Com transação própria.**

## O que acessa
- **Altera:** [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md)
- **Lê:** [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md), [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- "Boletim não encontrado."
- "Boletim já emitido."
- "Só a última revisão do boletim pode ser emitida."
- "Informe o(s) responsável(is) técnico(s) do boletim."
- "Boletim sem parâmetros selecionados."

## Processos
- [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md)

## Como chamar no C#
```csharp
// Abre a própria transação: NÃO chamar dentro de db.Database.BeginTransaction().
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_EMITIR_BOLETIM";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_boletim", id_boletim));
cmd.Parameters.Add(new MySqlParameter("p_id_func", id_func));
cmd.Parameters.Add(new MySqlParameter("p_arquivo", arquivo));
cmd.Parameters.Add(new MySqlParameter("p_hash", hash));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_EMITIR_BOLETIM(IN p_id_boletim INT, IN p_id_func INT, IN p_arquivo VARCHAR(255), IN p_hash CHAR(64))
BEGIN
  DECLARE v_emp, v_ultima, v_resp, v_resp2, v_exige2, v_status INT;
  DECLARE v_emissao DATETIME;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_EMPRESAS, ULTIMA_REVISAO, ID_ENTIDADES_FUNC_RESP, ID_ENTIDADES_FUNC_RESP2, DH_EMISSAO
    INTO v_emp, v_ultima, v_resp, v_resp2, v_emissao
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim FOR UPDATE;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Boletim não encontrado.';
  END IF;
  IF v_emissao IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Boletim já emitido.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só a última revisão do boletim pode ser emitida.';
  END IF;
  SELECT EXIGE_RESP2_BOLETIM, ID_BOLETINS_STATUS_EMITIDO INTO v_exige2, v_status
    FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = v_emp;
  IF v_resp IS NULL OR (COALESCE(v_exige2, 0) = 1 AND v_resp2 IS NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o(s) responsável(is) técnico(s) do boletim.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM LAB_BOLETIM_PARAMETROS WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Boletim sem parâmetros selecionados.';
  END IF;
  UPDATE LAB_BOLETIM_ANALISES
     SET DH_EMISSAO = NOW(), ID_ENTIDADES_FUNC_EMI = p_id_func,
         ID_BOLETINS_STATUS = COALESCE(v_status, ID_BOLETINS_STATUS),
         ARQUIVO_PDF = p_arquivo, HASH_PDF = p_hash
   WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim;
  COMMIT;
END
```
