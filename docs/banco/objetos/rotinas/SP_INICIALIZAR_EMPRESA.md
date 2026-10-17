# SP_INICIALIZAR_EMPRESA

Cria os cadastros padrão de uma empresa (status, idiomas, moedas, tipos, parâmetros e perfil ADMINISTRADOR com todas as permissões). Idempotente.

**Tipo:** Procedure pública (SP_) · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/02_funcoes_procedures.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
— (no legado os cadastros eram globais)

Comentário do script: _SP_INICIALIZAR_EMPRESA — cria os cadastros padrão de uma empresa nova (no legado as tabelas eram globais; agora tudo é por empresa). Idempotente (INSERT IGNORE pelas chaves únicas)._

## Assinatura
```sql
CREATE PROCEDURE SP_INICIALIZAR_EMPRESA(IN p_id_empresa INT)
```

| Direção | Parâmetro | Tipo |
|---|---|---|
| IN | `p_id_empresa` | INT |

## Quem chama
- **Uso previsto:** Criação de empresa (T03) e migração (M-01/M-04).
- **Chamada por rotinas:** —
- **Chamada por triggers:** —
- **Usada em views:** —

## Transação
Sem transação própria (INSERT IGNORE).

## O que acessa
- **Altera:** [`PERFIS`](../tabelas/PERFIS.md), [`PERFIS_PERMISSOES`](../tabelas/PERFIS_PERMISSOES.md), [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md), [`TIPOS_LOGRADOURO`](../tabelas/TIPOS_LOGRADOURO.md), [`ENDERECOS_TIPOS`](../tabelas/ENDERECOS_TIPOS.md), [`FONES_TIPOS`](../tabelas/FONES_TIPOS.md), [`EMAILS_TIPOS`](../tabelas/EMAILS_TIPOS.md), [`TIPOS_REG_PROFISSIONAL`](../tabelas/TIPOS_REG_PROFISSIONAL.md), [`IDIOMAS`](../tabelas/IDIOMAS.md), [`MOEDAS`](../tabelas/MOEDAS.md), [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md), [`ANALISES_STATUS`](../tabelas/ANALISES_STATUS.md), [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md), [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md)
- **Lê:** [`PERMISSOES`](../tabelas/PERMISSOES.md)
- **Chama:** —
- **Variáveis de sessão:** —

## Mensagens de negócio (SQLSTATE 45000)
- — (não lança mensagens próprias)

## Processos
- [P23 Nova empresa e cópia de cadastros](../../../processos/P23-nova-empresa.md)
- [P24 Migração Firebird → MySQL](../../../processos/P24-migracao.md)

## Como chamar no C#
```csharp
// Sem transação própria: pode ser chamada dentro da transação da aplicação.
await using var cmd = db.Database.GetDbConnection().CreateCommand();
cmd.CommandText = "SP_INICIALIZAR_EMPRESA";
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.Add(new MySqlParameter("p_id_empresa", id_empresa));
if (cmd.Connection!.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
try { await cmd.ExecuteNonQueryAsync(); }
catch (MySqlException ex) when (ex.SqlState == "45000") { return Resultado.Erro(ex.Message); } // mensagem de negócio
```

## Código
```sql
CREATE PROCEDURE SP_INICIALIZAR_EMPRESA(IN p_id_empresa INT)
BEGIN
  DECLARE v_perfil INT;

  INSERT IGNORE INTO AMOSTRAS_STATUS (ID_EMPRESAS, DESCRICAO, COR, PERMITE_RESULTADO) VALUES
    (p_id_empresa, 'LIBERADA PARA ANÁLISE', '#00FF00', 1),
    (p_id_empresa, 'BLOQUEADA', '#0000FF', 0),
    (p_id_empresa, 'REPROVADA - DEVOLVIDA AO CLIENTE', '#800080', 0),
    (p_id_empresa, 'REPROVADA - DESCARTADA', '#FF0000', 0);
  INSERT IGNORE INTO ANALISES_STATUS (ID_EMPRESAS, DESCRICAO, COR) VALUES
    (p_id_empresa, 'AGUARDANDO ANÁLISE', '#0000FF'), (p_id_empresa, 'EM ANÁLISE', '#FF00FF'),
    (p_id_empresa, 'CONCLUÍDA', '#00FF00');
  INSERT IGNORE INTO BOLETINS_STATUS (ID_EMPRESAS, DESCRICAO, COR) VALUES
    (p_id_empresa, 'EM ELABORAÇÃO', '#0000FF'), (p_id_empresa, 'EM REVISÃO', '#FF00FF'),
    (p_id_empresa, 'ENVIADO AO CLIENTE', '#00FF00');
  INSERT IGNORE INTO LAB_PROPOSTAS_STATUS (ID_EMPRESAS, DESCRICAO, COR, PERMITE_AMOSTRA) VALUES
    (p_id_empresa, 'EM ELABORAÇÃO', '#C0C0C0', 1),
    (p_id_empresa, 'AGUARDANDO INFORMAÇÃO DO CLIENTE', '#A6CAF0', 1),
    (p_id_empresa, 'ENVIADO AO CLIENTE', '#FFFFFF', 1);
  INSERT IGNORE INTO IDIOMAS (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'PORTUGUÊS'), (p_id_empresa, 'INGLÊS'), (p_id_empresa, 'ESPANHOL');
  INSERT IGNORE INTO MOEDAS (ID_EMPRESAS, DESCRICAO, SIGLA) VALUES
    (p_id_empresa, 'REAL', 'R$'), (p_id_empresa, 'DÓLAR', 'US$');
  INSERT IGNORE INTO ENDERECOS_TIPOS (ID_EMPRESAS, DESCRICAO, FINALIDADE) VALUES
    (p_id_empresa, 'NOTA FISCAL', 'NOTA_FISCAL'), (p_id_empresa, 'CORRESPONDÊNCIA', 'CORRESPONDENCIA'),
    (p_id_empresa, 'ENTREGA', 'ENTREGA'), (p_id_empresa, 'COBRANÇA', 'COBRANCA');
  INSERT IGNORE INTO FONES_TIPOS (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'COMERCIAL'), (p_id_empresa, 'CELULAR'), (p_id_empresa, 'RESIDENCIAL'), (p_id_empresa, 'FAX');
  INSERT IGNORE INTO EMAILS_TIPOS (ID_EMPRESAS, DESCRICAO) VALUES (p_id_empresa, 'COMERCIAL');
  INSERT IGNORE INTO TIPOS_REG_PROFISSIONAL (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'CRF'), (p_id_empresa, 'CRQ');
  INSERT IGNORE INTO TIPOS_LOGRADOURO (ID_EMPRESAS, DESCRICAO, ABREVIATURA) VALUES
    (p_id_empresa, 'RUA', 'R.'), (p_id_empresa, 'AVENIDA', 'AV.'), (p_id_empresa, 'ALAMEDA', 'AL.'),
    (p_id_empresa, 'TRAVESSA', 'TV.'), (p_id_empresa, 'RODOVIA', 'ROD.'), (p_id_empresa, 'ESTRADA', 'EST.'),
    (p_id_empresa, 'PRAÇA', 'PÇA.');

  INSERT IGNORE INTO EMPRESAS_PARAMETROS (ID_EMPRESAS) VALUES (p_id_empresa);
  UPDATE EMPRESAS_PARAMETROS EP
     SET ID_AMOSTRAS_STATUS_INICIAL = COALESCE(ID_AMOSTRAS_STATUS_INICIAL,
           (SELECT MIN(ID_AMOSTRAS_STATUS) FROM AMOSTRAS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'LIBERADA PARA ANÁLISE')),
         ID_AMOSTRAS_STATUS_RESULTADOS = COALESCE(ID_AMOSTRAS_STATUS_RESULTADOS,
           (SELECT MIN(ID_AMOSTRAS_STATUS) FROM AMOSTRAS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'LIBERADA PARA ANÁLISE')),
         ID_LAB_PROPOSTAS_STATUS_INICIAL = COALESCE(ID_LAB_PROPOSTAS_STATUS_INICIAL,
           (SELECT MIN(ID_LAB_PROPOSTAS_STATUS) FROM LAB_PROPOSTAS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'EM ELABORAÇÃO')),
         ID_LAB_PROPOSTAS_STATUS_ENVCLI = COALESCE(ID_LAB_PROPOSTAS_STATUS_ENVCLI,
           (SELECT MIN(ID_LAB_PROPOSTAS_STATUS) FROM LAB_PROPOSTAS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'ENVIADO AO CLIENTE')),
         ID_BOLETINS_STATUS_INICIAL = COALESCE(ID_BOLETINS_STATUS_INICIAL,
           (SELECT MIN(ID_BOLETINS_STATUS) FROM BOLETINS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'EM ELABORAÇÃO')),
         ID_BOLETINS_STATUS_EMITIDO = COALESCE(ID_BOLETINS_STATUS_EMITIDO,
           (SELECT MIN(ID_BOLETINS_STATUS) FROM BOLETINS_STATUS WHERE ID_EMPRESAS = p_id_empresa AND DESCRICAO = 'ENVIADO AO CLIENTE'))
   WHERE EP.ID_EMPRESAS = p_id_empresa;

  -- Perfil ADMINISTRADOR da empresa com todas as permissões
  INSERT IGNORE INTO PERFIS (ID_EMPRESAS, NOME, DESCRICAO) VALUES (p_id_empresa, 'ADMINISTRADOR', 'Acesso total à empresa');
  SELECT ID_PERFIS INTO v_perfil FROM PERFIS WHERE ID_EMPRESAS = p_id_empresa AND NOME = 'ADMINISTRADOR';
  INSERT IGNORE INTO PERFIS_PERMISSOES (ID_PERFIS, ID_PERMISSOES) SELECT v_perfil, ID_PERMISSOES FROM PERMISSOES;
END
```
