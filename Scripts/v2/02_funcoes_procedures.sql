-- =====================================================================
-- LabControl v2 — Funções e Stored Procedures (MySQL 8.0+)
-- Análise dos erros das procedures Firebird: docs/banco/12-PROCEDURES-REVISOES.md
--
-- Variáveis de sessão usadas pelas rotinas/triggers (definidas pela aplicação):
--   @LC_ID_USUARIOS          usuário logado (auditoria/log)
--   @LC_MIGRACAO = 1         desliga numeração automática/validações (só na migração)
--   @LC_PERMITE_RETROATIVO=1 libera data de entrega além de DIAS_RETROATIVOS_ENTREGA
--                            (somente após confirmação + permissão AMOSTRAS.ENTREGA_RETROATIVA)
--   @LC_DESFAZENDO = 1       uso interno de SP_EXCLUIR_REVISAO_*
--   @LC_REVISAO = 1          uso interno de SP_GERAR_REVISAO_*: só elas incluem revisão (REVISAO/REV_PROPOSTA > 0)
--   @LC_IP                   IP do usuário (AUDITORIA gravada pelas procedures, PR_AUDITAR)
--
-- Padrão transacional:
--   * PR_*  = rotinas internas, SEM controle de transação (podem ser aninhadas).
--   * SP_*  = rotinas públicas chamadas pela aplicação; abrem e fecham a própria
--            transação e fazem ROLLBACK + RESIGNAL em caso de erro.
--            NÃO chamar SP_* dentro de uma transação já aberta pela aplicação.
-- =====================================================================

USE labcontrol_v2;

DROP FUNCTION IF EXISTS FN_FORMATA_DECIMAL;
DROP FUNCTION IF EXISTS FN_FORMATA_RESULTADO;
DROP FUNCTION IF EXISTS FN_NR_AMOSTRA;
DROP FUNCTION IF EXISTS FN_NR_AMOSTRA_ID;
DROP FUNCTION IF EXISTS FN_NR_PROPOSTA;
DROP FUNCTION IF EXISTS FN_DATA_PARCIAL;
DROP FUNCTION IF EXISTS FN_ACREDITADO;
DROP FUNCTION IF EXISTS FN_OBS_METODOS;
DROP PROCEDURE IF EXISTS SP_PROXIMO_NUMERO;
DROP PROCEDURE IF EXISTS PR_AUDITAR;
DROP PROCEDURE IF EXISTS PR_CONFERIR_EMPRESA;
DROP PROCEDURE IF EXISTS PR_CRIAR_PERFIL;
DROP PROCEDURE IF EXISTS PR_CONFERIR_ENDERECO;
DROP PROCEDURE IF EXISTS PR_CONFERIR_FONE;
DROP PROCEDURE IF EXISTS PR_CONFERIR_EMAIL;
DROP PROCEDURE IF EXISTS PR_CONFERIR_TIPOS_ENTIDADE;
DROP PROCEDURE IF EXISTS PR_ATUALIZA_SALDO;
DROP PROCEDURE IF EXISTS SP_RECALCULAR_PROPOSTA;
DROP PROCEDURE IF EXISTS SP_AMOSTRA_GERAR_TESTES;
DROP PROCEDURE IF EXISTS PR_REVISAO_PROPOSTA;
DROP PROCEDURE IF EXISTS PR_REVISAO_BOLETIM;
DROP PROCEDURE IF EXISTS PR_REVISAO_RESULTADO;
DROP PROCEDURE IF EXISTS PR_REVISAO_AMOSTRA;
DROP PROCEDURE IF EXISTS SP_GERAR_REVISAO_PROPOSTA;
DROP PROCEDURE IF EXISTS SP_GERAR_REVISAO_AMOSTRA;
DROP PROCEDURE IF EXISTS SP_GERAR_REVISAO_RESULTADO;
DROP PROCEDURE IF EXISTS SP_GERAR_REVISAO_BOLETIM;
DROP PROCEDURE IF EXISTS SP_EXCLUIR_REVISAO_RESULTADO;
DROP PROCEDURE IF EXISTS SP_EXCLUIR_REVISAO_BOLETIM;
DROP PROCEDURE IF EXISTS SP_EMITIR_BOLETIM;
DROP PROCEDURE IF EXISTS SP_GERAR_PEDIDO_VENDA;
DROP PROCEDURE IF EXISTS SP_ATUALIZAR_PEDIDO_REVISAO;
DROP PROCEDURE IF EXISTS SP_TROCAR_PRODUTO_PROPOSTA;
DROP PROCEDURE IF EXISTS SP_INICIALIZAR_EMPRESA;

DELIMITER $$

-- ---------------------------------------------------------------------
-- FN_FORMATA_DECIMAL — equivalente corrigido de SP_FORMATDOUBLE.
-- Remove zeros à direita e usa vírgula decimal. 0/NULL -> ''.
-- ---------------------------------------------------------------------
CREATE FUNCTION FN_FORMATA_DECIMAL(p_valor DECIMAL(30,10))
RETURNS VARCHAR(40) DETERMINISTIC NO SQL
BEGIN
  DECLARE v VARCHAR(40);
  IF p_valor IS NULL OR p_valor = 0 THEN
    RETURN '';
  END IF;
  SET v = CAST(p_valor AS CHAR);
  IF LOCATE('.', v) > 0 THEN
    SET v = TRIM(TRAILING '0' FROM v);
    SET v = TRIM(TRAILING '.' FROM v);
  END IF;
  RETURN REPLACE(v, '.', ',');
END$$

-- ---------------------------------------------------------------------
-- FN_FORMATA_RESULTADO — texto do resultado/especificação para o boletim.
--   numérico  : "<simbolo> <valor>e<exp> <unidade>"  ex.: "< 1,0e2 UFC/g"
--   descritivo: descrição do LAB_TIPOS_RESULTADOS   ex.: "Ausência em 25 g"
-- (No Firebird: CASE em qry_Param e em SP_GERAR_REVISAO_BOLETIM.)
-- ---------------------------------------------------------------------
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
END$$

-- ---------------------------------------------------------------------
-- FN_NR_AMOSTRA — TT-CCCCC-AA/YY[ Rnn]  (TT/AA = CODIGO dos tipos)
-- ---------------------------------------------------------------------
CREATE FUNCTION FN_NR_AMOSTRA(p_cod_tipo_amostra INT, p_cod_amostra INT, p_cod_tipo_analise INT,
                              p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) DETERMINISTIC NO SQL
BEGIN
  RETURN CONCAT(LPAD(COALESCE(p_cod_tipo_amostra, 0), 2, '0'), '-',
                LPAD(COALESCE(p_cod_amostra, 0), 5, '0'), '-',
                LPAD(COALESCE(p_cod_tipo_analise, 0), 2, '0'), '/',
                LPAD(MOD(COALESCE(p_ano, 0), 100), 2, '0'),
                IF(COALESCE(p_revisao, 0) > 0, CONCAT(' R', LPAD(p_revisao, 2, '0')), ''));
END$$

-- Versão por IDs (resolve os CODIGOs dos tipos)
CREATE FUNCTION FN_NR_AMOSTRA_ID(p_id_tipo_amostra INT, p_cod_amostra INT, p_id_tipo_analise INT,
                                 p_ano INT, p_revisao INT)
RETURNS VARCHAR(25) READS SQL DATA
BEGIN
  DECLARE v_ta INT;
  DECLARE v_tn INT;
  SELECT CODIGO INTO v_ta FROM AMOSTRAS_TIPO WHERE ID_AMOSTRAS_TIPO = p_id_tipo_amostra;
  SELECT CODIGO INTO v_tn FROM ANALISES_TIPO WHERE ID_ANALISES_TIPO = p_id_tipo_analise;
  RETURN FN_NR_AMOSTRA(v_ta, p_cod_amostra, v_tn, p_ano, p_revisao);
END$$

-- ---------------------------------------------------------------------
-- FN_NR_PROPOSTA — 000123/26-R0
-- ---------------------------------------------------------------------
CREATE FUNCTION FN_NR_PROPOSTA(p_cod INT, p_ano INT, p_rev INT)
RETURNS VARCHAR(20) DETERMINISTIC NO SQL
BEGIN
  RETURN CONCAT(LPAD(COALESCE(p_cod, 0), 6, '0'), '/', LPAD(MOD(COALESCE(p_ano, 0), 100), 2, '0'),
                '-R', COALESCE(p_rev, 0));
END$$

-- ---------------------------------------------------------------------
-- FN_DATA_PARCIAL — "dd/mm/aaaa", "mm/aaaa" ou "aaaa" (validade/fabricação parciais)
-- Corrige o legado, que gerava "  /  2026" com espaços sobrando.
-- ---------------------------------------------------------------------
CREATE FUNCTION FN_DATA_PARCIAL(p_dia INT, p_mes INT, p_ano INT)
RETURNS VARCHAR(10) DETERMINISTIC NO SQL
BEGIN
  IF p_ano IS NULL THEN
    RETURN NULL;
  END IF;
  RETURN CONCAT(IF(p_dia IS NOT NULL AND p_mes IS NOT NULL, CONCAT(LPAD(p_dia, 2, '0'), '/'), ''),
                IF(p_mes IS NOT NULL, CONCAT(LPAD(p_mes, 2, '0'), '/'), ''),
                p_ano);
END$$

-- ---------------------------------------------------------------------
-- FN_ACREDITADO — 0 = nenhum parâmetro acreditado, 1 = todos, 2 = misto,
-- NULL = amostra sem testes. (Firebird: FN_ACREDITADO; retornava NULL também
-- quando não havia testes, mas a view fazia INNER JOIN e duplicava linhas.)
-- ---------------------------------------------------------------------
CREATE FUNCTION FN_ACREDITADO(p_id_amostra INT)
RETURNS TINYINT READS SQL DATA
BEGIN
  DECLARE v_ok INT DEFAULT 0;
  DECLARE v_nok INT DEFAULT 0;
  SELECT COALESCE(SUM(P.AUDITADO = 1), 0), COALESCE(SUM(P.AUDITADO = 0), 0)
    INTO v_ok, v_nok
    FROM LAB_HIST_AMOSTRAS_TESTES T
    JOIN LAB_PARAMETROS_ANALISES P ON P.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
   WHERE T.ID_LAB_HIST_AMOSTRAS = p_id_amostra;
  IF v_ok = 0 AND v_nok = 0 THEN RETURN NULL; END IF;
  IF v_ok = 0 THEN RETURN 0; END IF;
  IF v_nok = 0 THEN RETURN 1; END IF;
  RETURN 2;
END$$

-- ---------------------------------------------------------------------
-- FN_OBS_METODOS — texto de metodologias para o boletim (SP_OBSMETODOS).
-- Correção: usa o método do TESTE da amostra (o legado buscava na proposta
-- pelo parâmetro e podia trazer métodos de outro produto da mesma proposta).
-- ---------------------------------------------------------------------
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
END$$

-- ---------------------------------------------------------------------
-- SP_PROXIMO_NUMERO — numeração segura (bloqueio de linha) por empresa/tipo/ano.
-- Substitui SP_GET_COD_AMOSTRA, SP_GET_COD_PROPOSTAS, SP_GET_COD_BOLETIM,
-- SP_GET_ULT_PED_VENDA e SP_SYS_GERAR_CODIGO.
-- O ano NUNCA vem de data digitada: quem chama passa YEAR(CURDATE()) do servidor.
-- Não abre transação (é chamada também por triggers).
-- Uma única instrução com trava EXCLUSIVA (INSERT ... ON DUPLICATE KEY UPDATE): a versão anterior
-- (INSERT IGNORE + SELECT ... FOR UPDATE) pegava trava compartilhada no IGNORE e depois a exclusiva,
-- e duas gravações simultâneas na mesma empresa davam deadlock (teste ConcorrenciaTests, 19/10/2026).
-- A linha fica travada até o COMMIT de quem chamou: números consecutivos e sem repetição.
-- ---------------------------------------------------------------------
CREATE PROCEDURE SP_PROXIMO_NUMERO(IN p_id_empresa INT, IN p_tipo VARCHAR(30), IN p_ano SMALLINT,
                                   OUT p_numero INT)
BEGIN
  INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO)
  VALUES (p_id_empresa, p_tipo, p_ano, 1)
  ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = ULTIMO_NUMERO + 1;

  SELECT ULTIMO_NUMERO INTO p_numero
    FROM SEQUENCIAS_DOCUMENTOS
   WHERE ID_EMPRESAS = p_id_empresa AND TIPO_DOCUMENTO = p_tipo AND ANO = p_ano;
END$$

-- ---------------------------------------------------------------------
-- PR_AUDITAR — evento em AUDITORIA gravado pela própria procedure, dentro da
-- transação dela (revisão R, emissão M, desfazer D, pedido I/A, troca de produto A).
-- Usuário e IP vêm da sessão (@LC_ID_USUARIOS / @LC_IP, definidos pela aplicação).
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_AUDITAR(IN p_id_empresa INT, IN p_tabela VARCHAR(60), IN p_id_registro INT, IN p_acao CHAR(1),
                            IN p_antes JSON, IN p_depois JSON)
BEGIN
  INSERT INTO AUDITORIA (ID_EMPRESAS, ID_USUARIOS, TABELA, ID_REGISTRO, ACAO, DADOS_ANTES, DADOS_DEPOIS, IP)
  VALUES (p_id_empresa, @LC_ID_USUARIOS, p_tabela, p_id_registro, p_acao, p_antes, p_depois, @LC_IP);
END$$

-- ---------------------------------------------------------------------
-- Contatos das entidades (clientes, fornecedores, funcionários e empresa usuária),
-- chamadas pelas triggers BEFORE INSERT/UPDATE de ENTIDADES_ENDERECOS/FONES/EMAILS.
-- PR_CONFERIR_ENDERECO: no máximo um endereço ATIVO por tipo na entidade (no
-- Firebird era UNIQUE (entidade, tipo), que impedia guardar o endereço antigo inativo).
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_CONFERIR_ENDERECO(IN p_id INT, IN p_entidade INT, IN p_tipo INT, IN p_inativo TINYINT)
BEGIN
  IF p_inativo = 0 AND EXISTS (SELECT 1 FROM ENTIDADES_ENDERECOS
                                WHERE ID_ENTIDADES = p_entidade AND ID_ENDERECOS_TIPOS = p_tipo AND INATIVO = 0
                                  AND ID_ENTIDADES_ENDERECOS <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um endereço ativo deste tipo para este cadastro. Inative o anterior ou altere-o.';
  END IF;
END$$

-- PR_CONFERIR_FONE / PR_CONFERIR_EMAIL: um só principal por entidade (a aplicação
-- desmarca o anterior na mesma transação; o banco garante).
CREATE PROCEDURE PR_CONFERIR_FONE(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
BEGIN
  IF p_principal = 1 AND EXISTS (SELECT 1 FROM ENTIDADES_FONES
                                  WHERE ID_ENTIDADES = p_entidade AND PRINCIPAL = 1 AND ID_ENTIDADES_FONES <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um telefone principal para este cadastro.';
  END IF;
END$$

CREATE PROCEDURE PR_CONFERIR_EMAIL(IN p_id INT, IN p_entidade INT, IN p_principal TINYINT)
BEGIN
  IF p_principal = 1 AND EXISTS (SELECT 1 FROM ENTIDADES_EMAILS
                                  WHERE ID_ENTIDADES = p_entidade AND PRINCIPAL = 1 AND ID_ENTIDADES_EMAILS <> COALESCE(p_id, 0)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um e-mail principal para este cadastro.';
  END IF;
END$$

-- PR_CONFERIR_TIPOS_ENTIDADE: a empresa usuária "não pode coexistir com nenhum
-- outro tipo" (regra do legado, un_Entidades teEMP).
CREATE PROCEDURE PR_CONFERIR_TIPOS_ENTIDADE(IN p_empresa_usuaria TINYINT, IN p_outros INT)
BEGIN
  IF p_empresa_usuaria = 1 AND p_outros > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A empresa usuária não pode ser também cliente, fornecedor, funcionário, comercial ou transportador.';
  END IF;
END$$

-- ---------------------------------------------------------------------
-- PR_CONFERIR_EMPRESA — usada nas triggers BEFORE INSERT dos filhos, que herdam a
-- empresa do pai: recusa ID_EMPRESAS informado diferente da empresa do pai
-- (0/NULL = não informado, a trigger preenche).
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_CONFERIR_EMPRESA(IN p_informada INT, IN p_do_pai INT)
BEGIN
  IF COALESCE(p_informada, 0) <> 0 AND p_do_pai IS NOT NULL AND p_informada <> p_do_pai THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Registro pertence a outra empresa.';
  END IF;
END$$

-- ---------------------------------------------------------------------
-- PR_CRIAR_PERFIL — perfil sugerido da empresa (T04, dúvida 4 do plano), só se
-- ainda não existir: rodar de novo SP_INICIALIZAR_EMPRESA não devolve permissões
-- que a empresa tirou do perfil. p_permissoes = lista separada por vírgula de
-- códigos (AMOSTRAS.INCLUIR) ou de telas inteiras (BOLETINS.*).
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_CRIAR_PERFIL(IN p_id_empresa INT, IN p_nome VARCHAR(60), IN p_descricao VARCHAR(200), IN p_permissoes TEXT)
BEGIN
  DECLARE v_perfil INT;
  IF NOT EXISTS (SELECT 1 FROM PERFIS WHERE ID_EMPRESAS = p_id_empresa AND NOME = p_nome) THEN
    INSERT INTO PERFIS (ID_EMPRESAS, NOME, DESCRICAO) VALUES (p_id_empresa, p_nome, p_descricao);
    SET v_perfil = LAST_INSERT_ID();
    INSERT INTO PERFIS_PERMISSOES (ID_PERFIS, ID_PERMISSOES)
    SELECT v_perfil, ID_PERMISSOES FROM PERMISSOES
     WHERE FIND_IN_SET(CODIGO, REPLACE(p_permissoes, ' ', '')) > 0
        OR FIND_IN_SET(CONCAT(TELA, '.*'), REPLACE(p_permissoes, ' ', '')) > 0;
  END IF;
END$$

-- ---------------------------------------------------------------------
-- PR_ATUALIZA_SALDO — recalcula o saldo físico da amostra (TG_LAB_MOV_AMOSTRAS_AIUD)
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_ATUALIZA_SALDO(IN p_id_amostra INT)
BEGIN
  DECLARE v_emp INT;
  DECLARE v_saldo DECIMAL(15,4);
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;
  IF v_emp IS NOT NULL THEN
    SELECT COALESCE(SUM(IF(E_S = 'E', QTDE, -QTDE)), 0) INTO v_saldo
      FROM LAB_MOV_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_amostra;
    INSERT INTO LAB_HIST_AMOSTRAS_SALDO (ID_LAB_HIST_AMOSTRAS, ID_EMPRESAS, SALDO_ATUAL, DATA_ATUALIZACAO)
    VALUES (p_id_amostra, v_emp, v_saldo, NOW())
    ON DUPLICATE KEY UPDATE SALDO_ATUAL = v_saldo, DATA_ATUALIZACAO = NOW();
  END IF;
END$$

-- ---------------------------------------------------------------------
-- SP_RECALCULAR_PROPOSTA — totais do cabeçalho (TotalizarAnalises do Delphi)
--   VR_SOMA_* = Σ das análises; se PORC_DESCONTO > 0 recalcula VR_DESCONTO;
--   VR_TOTAL_PROPOSTA = VR_SOMA_VR_TOT_ANALIS − VR_DESCONTO.
-- Chamada pelas triggers de LAB_PROPOSTAS_PRODANALISES. Sem transação.
-- ---------------------------------------------------------------------
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
END$$

-- ---------------------------------------------------------------------
-- SP_AMOSTRA_GERAR_TESTES — copia os parâmetros do item da proposta para a amostra
-- (Grava_Hist_Amostra_Testes do Delphi). Recusa se já houver resultado.
-- Sem transação (a aplicação chama dentro da transação de gravação da amostra).
-- ---------------------------------------------------------------------
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
END$$

-- =====================================================================
-- REVISÕES — rotinas internas (sem transação)
-- =====================================================================

-- ---------------------------------------------------------------------
-- PR_REVISAO_PROPOSTA (corrige SP_GERAR_REVISAO_PROPOSTAS)
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                     OUT p_id_nova INT, OUT p_revisao INT)
BEGIN
  DECLARE v_emp, v_cod, v_ano, v_max, v_status_ini INT;

  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA INTO v_emp, v_cod, v_ano
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta de origem não encontrada.';
  END IF;

  -- bloqueia todas as revisões da proposta (evita duas revisões simultâneas com o mesmo número)
  SELECT MAX(REV_PROPOSTA) INTO v_max
    FROM LAB_PROPOSTAS
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  SELECT ID_LAB_PROPOSTAS_STATUS_INICIAL INTO v_status_ini
    FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = v_emp;

  INSERT INTO LAB_PROPOSTAS
        (ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, REV_PROPOSTA, NR_PROPOSTA, ID_LAB_PROPOSTAS_ORIGEM,
         ULTIMA_REVISAO, MOTIVO_REVISAO, ID_ENTIDADES, ID_ENT_CONTATO, ID_ENTIDADES_COMERC,
         ID_ENTIDADES_FUNC, ID_ENT_END_LAUDO, ID_ENT_CONTATO_RESULTADO, ID_ENT_COBRANCA,
         ID_ENT_END_COBRANCA, ID_ENT_CONTATO_NF, DT_SOLICITACAO, DT_VALIDADE, DT_ENVIO_CLIENTE,
         DT_AUTORIZACAO, ID_LAB_PROPOSTAS_STATUS, ID_CONDICOES_PAGTOS, ID_MOEDAS, PORC_DESCONTO,
         VR_DESCONTO, CRIADO_POR)
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, p_revisao, '', p_id_origem,
         1, p_motivo, ID_ENTIDADES, ID_ENT_CONTATO, ID_ENTIDADES_COMERC,
         ID_ENTIDADES_FUNC, ID_ENT_END_LAUDO, ID_ENT_CONTATO_RESULTADO, ID_ENT_COBRANCA,
         ID_ENT_END_COBRANCA, ID_ENT_CONTATO_NF, DT_SOLICITACAO, DT_VALIDADE, NULL,
         NULL, COALESCE(v_status_ini, ID_LAB_PROPOSTAS_STATUS), ID_CONDICOES_PAGTOS, ID_MOEDAS, PORC_DESCONTO,
         VR_DESCONTO, p_id_usuario
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_origem;
  SET p_id_nova = LAST_INSERT_ID();

  UPDATE LAB_PROPOSTAS SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod
     AND ID_LAB_PROPOSTAS <> p_id_nova AND ULTIMA_REVISAO = 1;

  INSERT INTO LAB_PROPOSTAS_TEXTOS
        (ID_LAB_PROPOSTAS, ID_EMPRESAS, TX_ITENS_SOLICITADOS, TX_ITENS_ADQUIRIDOS, TX_OBSERVACAO,
         TX_CONTRATO, TX_AUTORIZACAO)
  SELECT p_id_nova, ID_EMPRESAS, TX_ITENS_SOLICITADOS, TX_ITENS_ADQUIRIDOS, TX_OBSERVACAO,
         TX_CONTRATO, NULL
    FROM LAB_PROPOSTAS_TEXTOS WHERE ID_LAB_PROPOSTAS = p_id_origem;

  INSERT INTO LAB_PROPOSTAS_PRODUTOS (ID_EMPRESAS, ID_LAB_PROPOSTAS, ITEM, ID_PRODUTOS, QTDE_AMOSTRAS, ID_ORIGEM)
  SELECT ID_EMPRESAS, p_id_nova, ITEM, ID_PRODUTOS, QTDE_AMOSTRAS, ID_LAB_PROPOSTAS_PRODUTOS
    FROM LAB_PROPOSTAS_PRODUTOS WHERE ID_LAB_PROPOSTAS = p_id_origem;

  -- ST_REV_ANTERIOR = 1: a linha veio da revisão anterior (o legado calculava esse
  -- flag com uma consulta não correlacionada e marcava todas as linhas igual).
  INSERT INTO LAB_PROPOSTAS_PRODANALISES
        (ID_EMPRESAS, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, TIPO_DOCUMENTO, QTDE_AMOSTRAS, VR_UNITARIO,
         PORC_DESCONTO, VR_DESCONTO, ST_REV_ANTERIOR, ID_ORIGEM)
  SELECT PA.ID_EMPRESAS, p_id_nova, NP.ID_LAB_PROPOSTAS_PRODUTOS, PA.ID_LAB_PARAMETROS_ANALISES,
         PA.ID_ANALISES_METODOS, PA.ID_IDIOMAS, PA.ID_PRAZOS, PA.TIPO_DOCUMENTO, PA.QTDE_AMOSTRAS, PA.VR_UNITARIO,
         PA.PORC_DESCONTO, PA.VR_DESCONTO, 1, PA.ID_LAB_PROPOSTAS_PRODANALISES
    FROM LAB_PROPOSTAS_PRODANALISES PA
    JOIN LAB_PROPOSTAS_PRODUTOS NP ON NP.ID_LAB_PROPOSTAS = p_id_nova
                                  AND NP.ID_ORIGEM = PA.ID_LAB_PROPOSTAS_PRODUTOS
   WHERE PA.ID_LAB_PROPOSTAS = p_id_origem
   ORDER BY PA.ID_LAB_PROPOSTAS_PRODANALISES;

  CALL SP_RECALCULAR_PROPOSTA(p_id_nova);
END$$

-- ---------------------------------------------------------------------
-- PR_REVISAO_BOLETIM (corrige SP_GERAR_REVISAO_BOLETIM)
--   p_id_resultado_novo NULL = revisão só do boletim (mesmo resultado).
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_id_resultado_novo INT, IN p_motivo VARCHAR(500),
                                    IN p_id_func INT, IN p_id_usuario INT,
                                    OUT p_id_novo INT, OUT p_revisao INT)
BEGIN
  DECLARE v_emp, v_res_old, v_res, v_amostra, v_ultima, v_max, v_status_ini INT;
  DECLARE v_cod VARCHAR(30);

  SELECT ID_EMPRESAS, ID_LAB_RESULTADOS_ANALISES, COD_BOLETIM, ULTIMA_REVISAO
    INTO v_emp, v_res_old, v_cod, v_ultima
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Boletim de origem não encontrado.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão do boletim.';
  END IF;

  SET v_res = COALESCE(p_id_resultado_novo, v_res_old);
  SELECT ID_LAB_HIST_AMOSTRAS INTO v_amostra FROM LAB_RESULTADOS_ANALISES
   WHERE ID_LAB_RESULTADOS_ANALISES = v_res AND ID_EMPRESAS = v_emp;
  IF v_amostra IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado do boletim não encontrado nesta empresa.';
  END IF;

  SELECT MAX(REVISAO) INTO v_max FROM LAB_BOLETIM_ANALISES
   WHERE ID_EMPRESAS = v_emp AND COD_BOLETIM = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  SELECT ID_BOLETINS_STATUS_INICIAL INTO v_status_ini FROM EMPRESAS_PARAMETROS WHERE ID_EMPRESAS = v_emp;

  -- Nova revisão nasce NÃO emitida, com datas atuais e apontando para o resultado NOVO
  INSERT INTO LAB_BOLETIM_ANALISES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_LAB_RESULTADOS_ANALISES, COD_BOLETIM, REVISAO,
         ID_BOLETIM_ORIGEM, ULTIMA_REVISAO, MOTIVO_REVISAO, ID_BOLETINS_STATUS, DT_BOLETIM, DH_GERACAO,
         DH_EMISSAO, ID_ENTIDADES_FUNC_GER, ID_ENTIDADES_FUNC_EMI, ID_ENTIDADES_FUNC_RESP,
         ID_ENTIDADES_FUNC_RESP2, TXT_OBSERVACAO, TXT_LEGENDA, TXT_METODOLOGIA_UTIL,
         TXT_METODOLOGIA_ACRED, CRIADO_POR)
  SELECT ID_EMPRESAS, v_amostra, v_res, COD_BOLETIM, p_revisao,
         p_id_origem, 1, p_motivo, COALESCE(v_status_ini, ID_BOLETINS_STATUS), CURDATE(), NOW(),
         NULL, p_id_func, NULL, ID_ENTIDADES_FUNC_RESP,
         ID_ENTIDADES_FUNC_RESP2, TXT_OBSERVACAO, TXT_LEGENDA,
         COALESCE(FN_OBS_METODOS(v_amostra, 0), TXT_METODOLOGIA_UTIL),
         COALESCE(FN_OBS_METODOS(v_amostra, 1), TXT_METODOLOGIA_ACRED), p_id_usuario
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_origem;
  SET p_id_novo = LAST_INSERT_ID();

  UPDATE LAB_BOLETIM_ANALISES SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND COD_BOLETIM = v_cod
     AND ID_LAB_BOLETIM_ANALISES <> p_id_novo AND ULTIMA_REVISAO = 1;

  -- Copia SOMENTE os parâmetros escolhidos no boletim de origem (o legado copiava todos
  -- os parâmetros do resultado antigo), mapeando para as linhas do resultado novo e
  -- regravando o texto a partir dos valores atuais.
  INSERT INTO LAB_BOLETIM_PARAMETROS
        (ID_EMPRESAS, ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADOS_PARAM, ORDEM, DESC_PARAMETRO,
         RESULTADO_CONCAT, TIPO_RESULTADO_DESC, UNIDADE_SG, ESPECIFICACAO, ACREDITADO)
  SELECT v_emp, p_id_novo, RP.ID_LAB_RESULTADOS_PARAM, BP.ORDEM, PA.DESC_REDUZIDA,
         IF(RP.TIPO_RESULTADO = 1, RP.RESULTADO_FORMATADO, NULL),
         IF(RP.TIPO_RESULTADO = 0, RP.RESULTADO_FORMATADO, NULL),
         U.SIGLA, RP.ESPECIF_FORMATADA, PA.AUDITADO
    FROM LAB_BOLETIM_PARAMETROS BP
    JOIN LAB_RESULTADOS_PARAM RP_OLD ON RP_OLD.ID_LAB_RESULTADOS_PARAM = BP.ID_LAB_RESULTADOS_PARAM
    JOIN LAB_RESULTADOS_PARAM RP ON RP.ID_LAB_RESULTADOS_ANALISES = v_res
                                AND (   (v_res = v_res_old AND RP.ID_LAB_RESULTADOS_PARAM = RP_OLD.ID_LAB_RESULTADOS_PARAM)
                                     OR (v_res <> v_res_old AND RP.ID_PARAM_ORIGEM = RP_OLD.ID_LAB_RESULTADOS_PARAM))
    JOIN LAB_HIST_AMOSTRAS_TESTES T ON T.ID_LAB_HIST_AMOSTRAS_TESTES = RP.ID_LAB_HIST_AMOSTRAS_TESTES
    JOIN LAB_PARAMETROS_ANALISES PA ON PA.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
    LEFT JOIN UNIDADES U ON U.ID_UNIDADES = RP.ID_UNIDADES
   WHERE BP.ID_LAB_BOLETIM_ANALISES = p_id_origem
   ORDER BY BP.ORDEM, BP.ID_LAB_BOLETIM_PARAMETROS;
END$$

-- ---------------------------------------------------------------------
-- PR_REVISAO_RESULTADO (corrige SP_GERAR_REVISAO_RESULTADOS)
--   p_id_amostra_destino NULL = mesma amostra (revisão só do resultado).
--   p_gerar_boletim = 1: se o resultado de origem tem boletim, revisa também o boletim.
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_id_amostra_destino INT, IN p_motivo VARCHAR(500),
                                      IN p_id_func INT, IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                      OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE v_emp, v_amo_old, v_dest, v_raiz, v_ultima, v_max, v_bol, v_rev_bol, v_faltam INT;

  SELECT R.ID_EMPRESAS, R.ID_LAB_HIST_AMOSTRAS, R.ULTIMA_REVISAO INTO v_emp, v_amo_old, v_ultima
    FROM LAB_RESULTADOS_ANALISES R WHERE R.ID_LAB_RESULTADOS_ANALISES = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado de origem não encontrado.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão do resultado.';
  END IF;

  SET v_dest = COALESCE(p_id_amostra_destino, v_amo_old);
  SELECT COALESCE(ID_AMOSTRA_RAIZ, ID_LAB_HIST_AMOSTRAS) INTO v_raiz
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = v_dest AND ID_EMPRESAS = v_emp;
  IF v_raiz IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra de destino não encontrada nesta empresa.';
  END IF;

  -- Numeração contínua por amostra física (todas as revisões da amostra)
  SELECT MAX(R.REVISAO) INTO v_max
    FROM LAB_RESULTADOS_ANALISES R
    JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = R.ID_LAB_HIST_AMOSTRAS
   WHERE COALESCE(A.ID_AMOSTRA_RAIZ, A.ID_LAB_HIST_AMOSTRAS) = v_raiz
   FOR UPDATE;
  SET p_revisao = COALESCE(v_max, -1) + 1;

  INSERT INTO LAB_RESULTADOS_ANALISES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, REVISAO, ID_RESULTADO_ORIGEM, ULTIMA_REVISAO, MOTIVO_REVISAO,
         ID_ENTIDADES_FUNC_DIG, CRIADO_POR)
  VALUES (v_emp, v_dest, p_revisao, p_id_origem, 1, p_motivo, p_id_func, p_id_usuario);
  SET p_id_novo = LAST_INSERT_ID();

  UPDATE LAB_RESULTADOS_ANALISES R
    JOIN LAB_HIST_AMOSTRAS A ON A.ID_LAB_HIST_AMOSTRAS = R.ID_LAB_HIST_AMOSTRAS
     SET R.ULTIMA_REVISAO = 0
   WHERE COALESCE(A.ID_AMOSTRA_RAIZ, A.ID_LAB_HIST_AMOSTRAS) = v_raiz
     AND R.ID_LAB_RESULTADOS_ANALISES <> p_id_novo AND R.ULTIMA_REVISAO = 1;

  -- Mapeamento de testes pela coluna ID_TESTE_ORIGEM (o legado casava por
  -- tipo de análise + parâmetro com SELECT singleton e quebrava quando a amostra
  -- tinha o mesmo parâmetro duas vezes — 18 amostras no banco atual).
  INSERT INTO LAB_RESULTADOS_PARAM
        (ID_EMPRESAS, ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRAS_TESTES, DT_INICIAL, DT_RESULTADO,
         ID_ENTIDADES_FUNC_ANALISE, ID_ENTIDADES_FUNC_DIG, QT_AMOSTRA, ID_UNIDADES_AMOSTRA, TIPO_RESULTADO,
         SIMBOLO_GRANDEZA, VR_RESULTADO, VR_EXP_RESULTADO, ID_UNIDADES, ID_LAB_TIPOS_RESULTADOS,
         TIPO_RESULTADO_ESPECIF, SIMBOLO_GR_ESPECIF, VR_RESULTADO_ESPECIF, VR_EXP_ESPECIF,
         ID_UNIDADES_ESPECIF, ID_LAB_TIPO_RESULTADOS_ESP, VR_SATISFEITO, ID_PARAM_ORIGEM)
  SELECT P.ID_EMPRESAS, p_id_novo, IF(v_dest = v_amo_old, P.ID_LAB_HIST_AMOSTRAS_TESTES, TN.ID_LAB_HIST_AMOSTRAS_TESTES),
         P.DT_INICIAL, P.DT_RESULTADO, P.ID_ENTIDADES_FUNC_ANALISE, P.ID_ENTIDADES_FUNC_DIG, P.QT_AMOSTRA,
         P.ID_UNIDADES_AMOSTRA, P.TIPO_RESULTADO, P.SIMBOLO_GRANDEZA, P.VR_RESULTADO, P.VR_EXP_RESULTADO,
         P.ID_UNIDADES, P.ID_LAB_TIPOS_RESULTADOS, P.TIPO_RESULTADO_ESPECIF, P.SIMBOLO_GR_ESPECIF,
         P.VR_RESULTADO_ESPECIF, P.VR_EXP_ESPECIF, P.ID_UNIDADES_ESPECIF, P.ID_LAB_TIPO_RESULTADOS_ESP,
         P.VR_SATISFEITO, P.ID_LAB_RESULTADOS_PARAM
    FROM LAB_RESULTADOS_PARAM P
    LEFT JOIN LAB_HIST_AMOSTRAS_TESTES TN ON TN.ID_LAB_HIST_AMOSTRAS = v_dest
                                         AND TN.ID_TESTE_ORIGEM = P.ID_LAB_HIST_AMOSTRAS_TESTES
   WHERE P.ID_LAB_RESULTADOS_ANALISES = p_id_origem
     AND (v_dest = v_amo_old OR TN.ID_LAB_HIST_AMOSTRAS_TESTES IS NOT NULL)
   ORDER BY P.ID_LAB_RESULTADOS_PARAM;

  -- avisa se algum resultado não pôde ser levado (teste removido na revisão da amostra)
  SELECT COUNT(*) INTO v_faltam
    FROM LAB_RESULTADOS_PARAM P
   WHERE P.ID_LAB_RESULTADOS_ANALISES = p_id_origem
     AND NOT EXISTS (SELECT 1 FROM LAB_RESULTADOS_PARAM N
                      WHERE N.ID_LAB_RESULTADOS_ANALISES = p_id_novo
                        AND N.ID_PARAM_ORIGEM = P.ID_LAB_RESULTADOS_PARAM);
  IF v_faltam > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Há resultados cujo teste não existe na amostra de destino (revisão cancelada).';
  END IF;

  SET p_id_boletim_novo = NULL;
  IF p_gerar_boletim = 1 THEN
    SELECT ID_LAB_BOLETIM_ANALISES INTO v_bol
      FROM LAB_BOLETIM_ANALISES
     WHERE ID_LAB_RESULTADOS_ANALISES = p_id_origem AND ULTIMA_REVISAO = 1
     ORDER BY REVISAO DESC LIMIT 1;
    IF v_bol IS NOT NULL THEN
      CALL PR_REVISAO_BOLETIM(v_bol, p_id_novo, p_motivo, p_id_func, p_id_usuario, p_id_boletim_novo, v_rev_bol);
    END IF;
  END IF;
END$$

-- ---------------------------------------------------------------------
-- PR_REVISAO_AMOSTRA (corrige SP_GERAR_REVISAO_AMOSTRA)
-- ---------------------------------------------------------------------
CREATE PROCEDURE PR_REVISAO_AMOSTRA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                    IN p_id_usuario INT, OUT p_id_nova INT, OUT p_revisao INT,
                                    OUT p_id_resultado_novo INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE v_emp, v_ano, v_cod, v_raiz, v_ultima, v_max, v_res, v_rev_res INT;

  SELECT ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, COALESCE(ID_AMOSTRA_RAIZ, ID_LAB_HIST_AMOSTRAS), ULTIMA_REVISAO
    INTO v_emp, v_ano, v_cod, v_raiz, v_ultima
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Amostra de origem não encontrada.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível revisar a última revisão da amostra.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Não existem resultados para esta amostra: altere a amostra diretamente.';
  END IF;

  SELECT MAX(REVISAO) INTO v_max FROM LAB_HIST_AMOSTRAS
   WHERE ID_EMPRESAS = v_emp AND ANO_AMOSTRA = v_ano AND COD_AMOSTRA = v_cod
   FOR UPDATE;
  SET p_revisao = v_max + 1;

  -- Cópia COMPLETA (o legado esquecia COLETOR e DATAHORACOLETA — 61 revisões perderam o dado)
  INSERT INTO LAB_HIST_AMOSTRAS
        (ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, REVISAO, ID_AMOSTRAS_TIPO, ID_ANALISES_TIPO, NR_AMOSTRA,
         ID_AMOSTRA_ORIGEM, ID_AMOSTRA_RAIZ, ULTIMA_REVISAO, MOTIVO_REVISAO, TIPO_DOCUMENTO, ID_ENTIDADES,
         NOME_CONTATO, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_PEDIDOS_VENDA, ID_PRODUTOS, DT_ENTREGA,
         HR_ENTREGA, LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE, NR_LOTE,
         FABRICACAO_DIA, FABRICACAO_MES, FABRICACAO_ANO, VALIDADE_DIA, VALIDADE_MES, VALIDADE_ANO,
         DT_FABRICACAO, DT_VALIDADE, NOTA_ROTULO, DT_ENTREGA_VERIFICACAO, HR_ENTREGA_VERIFICACAO,
         ESPECIE_AMOSTRA, ASPECTO_AMOSTRA, COR, OUTRAS_CARACTERISTICAS, QTDE_AMOSTRA_VERIFICACAO,
         TEMPERATURA_VERIFICACAO, OBSERVACAO_VERIFICACAO, ACOMPANHA_FICHA_TECNICA, ACOMPANHA_PADRAO_ANALITICO,
         ACOMPANHA_CA_CLIENTE, ENVIAR_OUTRO_LABORATORIO, QTDE_ENVIO_OUTRO_LABORATORIO, ID_AMOSTRAS_STATUS,
         ID_ENTIDADES_FUNC_RESP, ID_ENTIDADES_FUNC_DIG, NOME_COLETOR, DT_HR_COLETA, TX_OBSERVACAO, CRIADO_POR)
  SELECT ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, p_revisao, ID_AMOSTRAS_TIPO, ID_ANALISES_TIPO, '',
         p_id_origem, v_raiz, 1, p_motivo, TIPO_DOCUMENTO, ID_ENTIDADES,
         NOME_CONTATO, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_PEDIDOS_VENDA, ID_PRODUTOS, DT_ENTREGA,
         HR_ENTREGA, LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE, NR_LOTE,
         FABRICACAO_DIA, FABRICACAO_MES, FABRICACAO_ANO, VALIDADE_DIA, VALIDADE_MES, VALIDADE_ANO,
         DT_FABRICACAO, DT_VALIDADE, NOTA_ROTULO, DT_ENTREGA_VERIFICACAO, HR_ENTREGA_VERIFICACAO,
         ESPECIE_AMOSTRA, ASPECTO_AMOSTRA, COR, OUTRAS_CARACTERISTICAS, QTDE_AMOSTRA_VERIFICACAO,
         TEMPERATURA_VERIFICACAO, OBSERVACAO_VERIFICACAO, ACOMPANHA_FICHA_TECNICA, ACOMPANHA_PADRAO_ANALITICO,
         ACOMPANHA_CA_CLIENTE, ENVIAR_OUTRO_LABORATORIO, QTDE_ENVIO_OUTRO_LABORATORIO, ID_AMOSTRAS_STATUS,
         ID_ENTIDADES_FUNC_RESP, p_id_func, NOME_COLETOR, DT_HR_COLETA, TX_OBSERVACAO, p_id_usuario
    FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem;
  SET p_id_nova = LAST_INSERT_ID();

  UPDATE LAB_HIST_AMOSTRAS SET ULTIMA_REVISAO = 0
   WHERE ID_EMPRESAS = v_emp AND ANO_AMOSTRA = v_ano AND COD_AMOSTRA = v_cod
     AND ID_LAB_HIST_AMOSTRAS <> p_id_nova AND ULTIMA_REVISAO = 1;

  INSERT INTO LAB_HIST_AMOSTRAS_TESTES
        (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_TIPO, ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, DT_PREVISTA, ID_ANALISES_STATUS,
         TX_ANALISES_TIPO, ID_TESTE_ORIGEM)
  SELECT ID_EMPRESAS, p_id_nova, ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PARAMETROS_ANALISES,
         ID_ANALISES_TIPO, ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, DT_PREVISTA, ID_ANALISES_STATUS,
         TX_ANALISES_TIPO, ID_LAB_HIST_AMOSTRAS_TESTES
    FROM LAB_HIST_AMOSTRAS_TESTES WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem
   ORDER BY ID_LAB_HIST_AMOSTRAS_TESTES;

  -- Leva a ÚLTIMA revisão do resultado para a nova amostra (e revisa o boletim)
  SELECT ID_LAB_RESULTADOS_ANALISES INTO v_res
    FROM LAB_RESULTADOS_ANALISES
   WHERE ID_LAB_HIST_AMOSTRAS = p_id_origem AND ULTIMA_REVISAO = 1
   ORDER BY REVISAO DESC LIMIT 1;
  SET p_id_resultado_novo = NULL;
  SET p_id_boletim_novo = NULL;
  IF v_res IS NOT NULL THEN
    CALL PR_REVISAO_RESULTADO(v_res, p_id_nova, p_motivo, p_id_func, p_id_usuario, 1,
                              p_id_resultado_novo, v_rev_res, p_id_boletim_novo);
  END IF;
END$$

-- =====================================================================
-- REVISÕES — rotinas públicas (com transação)
-- =====================================================================

CREATE PROCEDURE SP_GERAR_REVISAO_PROPOSTA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_usuario INT,
                                           OUT p_id_nova INT, OUT p_revisao INT)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  CALL PR_REVISAO_PROPOSTA(p_id_origem, p_motivo, p_id_usuario, p_id_nova, p_revisao);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_nova), 'LAB_PROPOSTAS', p_id_nova, 'R',
                  JSON_OBJECT('ID_ORIGEM', p_id_origem), JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo));
  COMMIT;
END$$

CREATE PROCEDURE SP_GERAR_REVISAO_AMOSTRA(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                          IN p_id_usuario INT, OUT p_id_nova INT, OUT p_revisao INT,
                                          OUT p_id_resultado_novo INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  CALL PR_REVISAO_AMOSTRA(p_id_origem, p_motivo, p_id_func, p_id_usuario, p_id_nova, p_revisao,
                          p_id_resultado_novo, p_id_boletim_novo);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = p_id_nova), 'LAB_HIST_AMOSTRAS', p_id_nova, 'R',
                  JSON_OBJECT('ID_ORIGEM', p_id_origem),
                  JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo, 'ID_RESULTADO_NOVO', p_id_resultado_novo,
                              'ID_BOLETIM_NOVO', p_id_boletim_novo));
  COMMIT;
END$$

CREATE PROCEDURE SP_GERAR_REVISAO_RESULTADO(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                            IN p_id_usuario INT, IN p_gerar_boletim TINYINT,
                                            OUT p_id_novo INT, OUT p_revisao INT, OUT p_id_boletim_novo INT)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  CALL PR_REVISAO_RESULTADO(p_id_origem, NULL, p_motivo, p_id_func, p_id_usuario, p_gerar_boletim,
                            p_id_novo, p_revisao, p_id_boletim_novo);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_novo),
                  'LAB_RESULTADOS_ANALISES', p_id_novo, 'R', JSON_OBJECT('ID_ORIGEM', p_id_origem),
                  JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo, 'ID_BOLETIM_NOVO', p_id_boletim_novo));
  COMMIT;
END$$

CREATE PROCEDURE SP_GERAR_REVISAO_BOLETIM(IN p_id_origem INT, IN p_motivo VARCHAR(500), IN p_id_func INT,
                                          IN p_id_usuario INT, OUT p_id_novo INT, OUT p_revisao INT)
BEGIN
  DECLARE v_res, v_res_ultimo INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_REVISAO = NULL; ROLLBACK; RESIGNAL; END;
  IF COALESCE(TRIM(p_motivo), '') = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Informe o motivo da revisão.';
  END IF;
  START TRANSACTION;
  SET @LC_REVISAO = 1;
  -- Se o resultado foi revisado depois do boletim, a revisão do boletim já aponta para o resultado vigente
  SELECT B.ID_LAB_RESULTADOS_ANALISES INTO v_res FROM LAB_BOLETIM_ANALISES B
   WHERE B.ID_LAB_BOLETIM_ANALISES = p_id_origem;
  SELECT R2.ID_LAB_RESULTADOS_ANALISES INTO v_res_ultimo
    FROM LAB_RESULTADOS_ANALISES R2
   WHERE R2.ID_LAB_HIST_AMOSTRAS = (SELECT ID_LAB_HIST_AMOSTRAS FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = v_res)
     AND R2.ULTIMA_REVISAO = 1
   LIMIT 1;
  CALL PR_REVISAO_BOLETIM(p_id_origem, IF(v_res_ultimo IS NOT NULL AND v_res_ultimo <> v_res, v_res_ultimo, NULL),
                          p_motivo, p_id_func, p_id_usuario, p_id_novo, p_revisao);
  SET @LC_REVISAO = NULL;
  CALL PR_AUDITAR((SELECT ID_EMPRESAS FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_novo),
                  'LAB_BOLETIM_ANALISES', p_id_novo, 'R', JSON_OBJECT('ID_ORIGEM', p_id_origem),
                  JSON_OBJECT('REVISAO', p_revisao, 'MOTIVO', p_motivo));
  COMMIT;
END$$

-- Desfaz (exclui) a última revisão de um resultado ainda sem boletim, reativando a anterior.
CREATE PROCEDURE SP_EXCLUIR_REVISAO_RESULTADO(IN p_id_resultado INT)
BEGIN
  DECLARE v_origem, v_ultima, v_emp INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_DESFAZENDO = NULL; ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_RESULTADO_ORIGEM, ULTIMA_REVISAO, ID_EMPRESAS INTO v_origem, v_ultima, v_emp
    FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado FOR UPDATE;
  IF v_ultima IS NULL OR v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível excluir a última revisão do resultado.';
  END IF;
  IF EXISTS (SELECT 1 FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Resultado possui boletim: exclua/revise o boletim primeiro.';
  END IF;
  SET @LC_DESFAZENDO = 1;
  DELETE FROM LAB_RESULTADOS_ANALISES WHERE ID_LAB_RESULTADOS_ANALISES = p_id_resultado;
  IF v_origem IS NOT NULL THEN
    UPDATE LAB_RESULTADOS_ANALISES SET ULTIMA_REVISAO = 1 WHERE ID_LAB_RESULTADOS_ANALISES = v_origem;
  END IF;
  SET @LC_DESFAZENDO = NULL;
  CALL PR_AUDITAR(v_emp, 'LAB_RESULTADOS_ANALISES', p_id_resultado, 'D', NULL, JSON_OBJECT('ID_REATIVADO', v_origem));
  COMMIT;
END$$

-- Desfaz (exclui) a última revisão de um boletim NÃO emitido, reativando a anterior.
CREATE PROCEDURE SP_EXCLUIR_REVISAO_BOLETIM(IN p_id_boletim INT)
BEGIN
  DECLARE v_origem, v_ultima, v_emp INT;
  DECLARE v_emissao DATETIME;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET @LC_DESFAZENDO = NULL; ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_BOLETIM_ORIGEM, ULTIMA_REVISAO, DH_EMISSAO, ID_EMPRESAS INTO v_origem, v_ultima, v_emissao, v_emp
    FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim FOR UPDATE;
  IF v_ultima IS NULL OR v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Só é possível excluir a última revisão do boletim.';
  END IF;
  IF v_emissao IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este boletim foi emitido. Não é permitido alterar ou excluir.';
  END IF;
  SET @LC_DESFAZENDO = 1;
  DELETE FROM LAB_BOLETIM_ANALISES WHERE ID_LAB_BOLETIM_ANALISES = p_id_boletim;
  IF v_origem IS NOT NULL THEN
    UPDATE LAB_BOLETIM_ANALISES SET ULTIMA_REVISAO = 1 WHERE ID_LAB_BOLETIM_ANALISES = v_origem;
  END IF;
  SET @LC_DESFAZENDO = NULL;
  CALL PR_AUDITAR(v_emp, 'LAB_BOLETIM_ANALISES', p_id_boletim, 'D', NULL, JSON_OBJECT('ID_REATIVADO', v_origem));
  COMMIT;
END$$

-- ---------------------------------------------------------------------
-- SP_EMITIR_BOLETIM — congela o boletim (o legado nunca preenchia DH_EMISSAO:
-- 0 de 5.312 boletins emitidos, logo a trava ERR_BOLETIM_EMITIDO nunca funcionou).
-- ---------------------------------------------------------------------
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
  CALL PR_AUDITAR(v_emp, 'LAB_BOLETIM_ANALISES', p_id_boletim, 'M', NULL,
                  JSON_OBJECT('ARQUIVO_PDF', p_arquivo, 'HASH_PDF', p_hash, 'ID_ENTIDADES_FUNC_EMI', p_id_func));
  COMMIT;
END$$

-- ---------------------------------------------------------------------
-- SP_GERAR_PEDIDO_VENDA — substitui Btn_PedVendaClick + SP_GET_ULT_PED_VENDA.
-- Um pedido por proposta (todas as revisões). Exige data de autorização.
-- ---------------------------------------------------------------------
CREATE PROCEDURE SP_GERAR_PEDIDO_VENDA(IN p_id_proposta INT, IN p_dt_autorizacao DATE,
                                       IN p_autorizado_por VARCHAR(100), IN p_nr_pedido_cliente VARCHAR(40),
                                       IN p_id_usuario INT, OUT p_id_pedido INT, OUT p_nr_pedido INT)
BEGIN
  DECLARE v_emp, v_cod, v_ano, v_ultima INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  IF p_dt_autorizacao IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'É obrigatório informar a Data de Autorização para gerar o Pedido de Venda.';
  END IF;
  IF p_dt_autorizacao > CURDATE() THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Data de Autorização não pode ser futura.';
  END IF;
  START TRANSACTION;
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, ULTIMA_REVISAO INTO v_emp, v_cod, v_ano, v_ultima
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta FOR UPDATE;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta não encontrada.';
  END IF;
  IF v_ultima = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gere o pedido a partir da última revisão da proposta.';
  END IF;
  IF EXISTS (SELECT 1 FROM PEDIDOS_VENDA WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe Pedido de Venda para esta proposta.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM LAB_PROPOSTAS_PRODANALISES WHERE ID_LAB_PROPOSTAS = p_id_proposta) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta sem análises.';
  END IF;

  UPDATE LAB_PROPOSTAS SET DT_AUTORIZACAO = p_dt_autorizacao, ALTERADO_POR = p_id_usuario
   WHERE ID_LAB_PROPOSTAS = p_id_proposta;

  INSERT INTO PEDIDOS_VENDA
        (ID_EMPRESAS, NR_PEDIDO, DT_PEDIDO, ID_LAB_PROPOSTAS, COD_PROPOSTA, ANO_PROPOSTA, ID_ENTIDADES,
         ID_ENT_COBRANCA, ID_CONDICOES_PAGTOS, ID_MOEDAS, DT_AUTORIZACAO, AUTORIZADO_POR, NR_PEDIDO_CLIENTE,
         VR_TOTAL, CRIADO_POR)
  SELECT ID_EMPRESAS, 0, CURDATE(), ID_LAB_PROPOSTAS, COD_PROPOSTA, ANO_PROPOSTA, ID_ENTIDADES,
         ID_ENT_COBRANCA, ID_CONDICOES_PAGTOS, ID_MOEDAS, p_dt_autorizacao, p_autorizado_por, p_nr_pedido_cliente,
         VR_TOTAL_PROPOSTA, p_id_usuario
    FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta;
  SET p_id_pedido = LAST_INSERT_ID();
  SELECT NR_PEDIDO INTO p_nr_pedido FROM PEDIDOS_VENDA WHERE ID_PEDIDOS_VENDA = p_id_pedido;

  INSERT INTO PEDIDOS_VENDA_ITENS (ID_EMPRESAS, ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS, ID_PRODUTOS, QTDE_CONTRATADA, VR_TOTAL)
  SELECT PP.ID_EMPRESAS, p_id_pedido, PP.ID_LAB_PROPOSTAS_PRODUTOS, PP.ID_PRODUTOS, PP.QTDE_AMOSTRAS,
         COALESCE((SELECT SUM(PA.VR_TOTAL) FROM LAB_PROPOSTAS_PRODANALISES PA
                    WHERE PA.ID_LAB_PROPOSTAS_PRODUTOS = PP.ID_LAB_PROPOSTAS_PRODUTOS), 0)
    FROM LAB_PROPOSTAS_PRODUTOS PP WHERE PP.ID_LAB_PROPOSTAS = p_id_proposta;

  -- amostras já recebidas desta proposta (qualquer revisão) passam a apontar para o pedido
  UPDATE LAB_HIST_AMOSTRAS A
    JOIN LAB_PROPOSTAS P ON P.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS
     SET A.ID_PEDIDOS_VENDA = p_id_pedido
   WHERE P.ID_EMPRESAS = v_emp AND P.ANO_PROPOSTA = v_ano AND P.COD_PROPOSTA = v_cod
     AND A.ID_PEDIDOS_VENDA IS NULL AND A.ULTIMA_REVISAO = 1;
  CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'I', NULL,
                  JSON_OBJECT('NR_PEDIDO', p_nr_pedido, 'ID_LAB_PROPOSTAS', p_id_proposta, 'DT_AUTORIZACAO', p_dt_autorizacao,
                              'AUTORIZADO_POR', p_autorizado_por, 'NR_PEDIDO_CLIENTE', p_nr_pedido_cliente));
  COMMIT;
END$$

-- Atualiza o pedido para a revisão vigente da proposta (após revisão aprovada pelo cliente)
CREATE PROCEDURE SP_ATUALIZAR_PEDIDO_REVISAO(IN p_id_pedido INT, IN p_id_usuario INT)
corpo: BEGIN
  DECLARE v_emp, v_cod, v_ano, v_nova, v_antiga INT;
  DECLARE v_sit VARCHAR(20);
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, SITUACAO, ID_LAB_PROPOSTAS INTO v_emp, v_cod, v_ano, v_sit, v_antiga
    FROM PEDIDOS_VENDA WHERE ID_PEDIDOS_VENDA = p_id_pedido FOR UPDATE;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Pedido não encontrado.';
  END IF;
  IF v_sit IN ('CONCLUIDO', 'CANCELADO') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Pedido concluído/cancelado não pode ser atualizado.';
  END IF;
  SELECT ID_LAB_PROPOSTAS INTO v_nova FROM LAB_PROPOSTAS
   WHERE ID_EMPRESAS = v_emp AND ANO_PROPOSTA = v_ano AND COD_PROPOSTA = v_cod AND ULTIMA_REVISAO = 1;
  -- sem revisão atual os itens seriam apagados e nada reinserido; já na revisão atual não há o que atualizar
  IF v_nova IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Revisão atual da proposta não encontrada.';
  END IF;
  IF v_nova = v_antiga THEN
    COMMIT;
    LEAVE corpo;
  END IF;

  UPDATE PEDIDOS_VENDA PV JOIN LAB_PROPOSTAS P ON P.ID_LAB_PROPOSTAS = v_nova
     SET PV.ID_LAB_PROPOSTAS = v_nova, PV.VR_TOTAL = P.VR_TOTAL_PROPOSTA,
         PV.ID_CONDICOES_PAGTOS = P.ID_CONDICOES_PAGTOS, PV.ID_MOEDAS = P.ID_MOEDAS,
         PV.ID_ENT_COBRANCA = P.ID_ENT_COBRANCA, PV.ALTERADO_POR = p_id_usuario
   WHERE PV.ID_PEDIDOS_VENDA = p_id_pedido;

  DELETE FROM PEDIDOS_VENDA_ITENS WHERE ID_PEDIDOS_VENDA = p_id_pedido;
  INSERT INTO PEDIDOS_VENDA_ITENS (ID_EMPRESAS, ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS, ID_PRODUTOS, QTDE_CONTRATADA, VR_TOTAL)
  SELECT PP.ID_EMPRESAS, p_id_pedido, PP.ID_LAB_PROPOSTAS_PRODUTOS, PP.ID_PRODUTOS, PP.QTDE_AMOSTRAS,
         COALESCE((SELECT SUM(PA.VR_TOTAL) FROM LAB_PROPOSTAS_PRODANALISES PA
                    WHERE PA.ID_LAB_PROPOSTAS_PRODUTOS = PP.ID_LAB_PROPOSTAS_PRODUTOS), 0)
    FROM LAB_PROPOSTAS_PRODUTOS PP WHERE PP.ID_LAB_PROPOSTAS = v_nova;
  CALL PR_AUDITAR(v_emp, 'PEDIDOS_VENDA', p_id_pedido, 'A', JSON_OBJECT('ID_LAB_PROPOSTAS', v_antiga),
                  JSON_OBJECT('ID_LAB_PROPOSTAS', v_nova));
  COMMIT;
END$$

-- ---------------------------------------------------------------------
-- SP_TROCAR_PRODUTO_PROPOSTA — corrige SP_TROCAPRODUTO, cujo FOR SELECT
-- percorria TODAS as amostras do banco (sem filtrar a proposta).
-- ---------------------------------------------------------------------
CREATE PROCEDURE SP_TROCAR_PRODUTO_PROPOSTA(IN p_id_proposta INT, IN p_id_prod_velho INT, IN p_id_prod_novo INT)
BEGIN
  DECLARE v_emp INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
  START TRANSACTION;
  SELECT ID_EMPRESAS INTO v_emp FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = p_id_proposta;
  IF v_emp IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Proposta não encontrada.';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM PRODUTOS WHERE ID_PRODUTOS = p_id_prod_novo AND ID_EMPRESAS = v_emp) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Produto novo não encontrado nesta empresa.';
  END IF;
  UPDATE LAB_PROPOSTAS_PRODUTOS SET ID_PRODUTOS = p_id_prod_novo
   WHERE ID_LAB_PROPOSTAS = p_id_proposta AND ID_PRODUTOS = p_id_prod_velho;
  UPDATE LAB_HIST_AMOSTRAS SET ID_PRODUTOS = p_id_prod_novo
   WHERE ID_LAB_PROPOSTAS = p_id_proposta AND ID_PRODUTOS = p_id_prod_velho;
  CALL PR_AUDITAR(v_emp, 'LAB_PROPOSTAS', p_id_proposta, 'A', JSON_OBJECT('ID_PRODUTOS', p_id_prod_velho),
                  JSON_OBJECT('ID_PRODUTOS', p_id_prod_novo));
  COMMIT;
END$$

-- ---------------------------------------------------------------------
-- SP_INICIALIZAR_EMPRESA — cria os cadastros padrão de uma empresa nova
-- (no legado as tabelas eram globais; agora tudo é por empresa).
-- Idempotente (INSERT IGNORE pelas chaves únicas).
-- ---------------------------------------------------------------------
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
  -- tipos de telefone do legado (FONES_TIPOS do Firebird) + WhatsApp
  INSERT IGNORE INTO FONES_TIPOS (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'COMERCIAL'), (p_id_empresa, 'RESIDENCIAL'), (p_id_empresa, 'CELULAR COMERCIAL'),
    (p_id_empresa, 'CELULAR PARTICULAR'), (p_id_empresa, 'WHATSAPP'), (p_id_empresa, 'RÁDIO');
  INSERT IGNORE INTO EMAILS_TIPOS (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'GERAL'), (p_id_empresa, 'COMERCIAL'), (p_id_empresa, 'FINANCEIRO'), (p_id_empresa, 'TÉCNICO');
  INSERT IGNORE INTO TIPOS_REG_PROFISSIONAL (ID_EMPRESAS, DESCRICAO) VALUES
    (p_id_empresa, 'CRF'), (p_id_empresa, 'CRQ');
  INSERT IGNORE INTO TIPOS_LOGRADOURO (ID_EMPRESAS, DESCRICAO, ABREVIATURA) VALUES
    (p_id_empresa, 'RUA', 'R.'), (p_id_empresa, 'AVENIDA', 'AV.'), (p_id_empresa, 'ALAMEDA', 'AL.'),
    (p_id_empresa, 'TRAVESSA', 'TV.'), (p_id_empresa, 'RODOVIA', 'ROD.'), (p_id_empresa, 'ESTRADA', 'EST.'),
    (p_id_empresa, 'PRAÇA', 'PÇA.');

  -- Entidade da própria empresa (como no legado: EMPRESAS.ID_ENTIDADES, tipo "empresa usuária"):
  -- guarda os endereços, telefones e e-mails da empresa. Identidade copiada de EMPRESAS
  -- (depois mantida por TG_EMPRESAS_AU). Só cria se ainda não existir.
  IF (SELECT ID_ENTIDADES FROM EMPRESAS WHERE ID_EMPRESAS = p_id_empresa) IS NULL THEN
    INSERT INTO ENTIDADES (ID_EMPRESAS, CATEGORIA, NOME, NOME_FANTASIA, CPF_CNPJ, RG_IE, INSC_MUNICIPAL, TIPO_EMPRESA_USUARIA, INATIVO)
    SELECT ID_EMPRESAS, 'J', RAZAO_SOCIAL, NOME_FANTASIA, CNPJ, INSC_ESTADUAL, INSC_MUNICIPAL, 1, 1 - ATIVA
      FROM EMPRESAS WHERE ID_EMPRESAS = p_id_empresa;
    UPDATE EMPRESAS SET ID_ENTIDADES = LAST_INSERT_ID() WHERE ID_EMPRESAS = p_id_empresa;
  END IF;

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

  -- Perfis sugeridos (T04; editáveis pela empresa). Toda ação vem com o CONSULTAR da tela.
  CALL PR_CRIAR_PERFIL(p_id_empresa, 'RECEPÇÃO', 'Recebimento de amostras, etiquetas e movimentação',
       'CLIENTES.CONSULTAR,CLIENTES.INCLUIR,CLIENTES.ALTERAR,CLIENTES.IMPRIMIR,PROPOSTAS.CONSULTAR,'
       'AMOSTRAS.CONSULTAR,AMOSTRAS.INCLUIR,AMOSTRAS.ALTERAR,AMOSTRAS.IMPRIMIR,ETIQUETAS.CONSULTAR,ETIQUETAS.IMPRIMIR,'
       'MOVIMENTACAO.CONSULTAR,MOVIMENTACAO.INCLUIR,MOVIMENTACAO.ALTERAR,MOVIMENTACAO.IMPRIMIR');
  CALL PR_CRIAR_PERFIL(p_id_empresa, 'ANALISTA', 'Lançamento de resultados',
       'AMOSTRAS.CONSULTAR,RESULTADOS.CONSULTAR,RESULTADOS.INCLUIR,RESULTADOS.ALTERAR,RESULTADOS.IMPRIMIR');
  CALL PR_CRIAR_PERFIL(p_id_empresa, 'RESPONSÁVEL TÉCNICO', 'Revisão de resultados e emissão de boletins',
       'AMOSTRAS.CONSULTAR,RESULTADOS.CONSULTAR,RESULTADOS.IMPRIMIR,RESULTADOS.REVISAR,RESULTADOS.DESFAZER_REVISAO,BOLETINS.*');
  CALL PR_CRIAR_PERFIL(p_id_empresa, 'COMERCIAL', 'Clientes, produtos, propostas, pedidos e relatórios',
       'CLIENTES.*,PRODUTOS.*,PROPOSTAS.*,PEDIDOS_VENDA.*,RELATORIOS.*');
END$$

DELIMITER ;
