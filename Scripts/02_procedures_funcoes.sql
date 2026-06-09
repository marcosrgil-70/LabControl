-- ============================================================
-- LabControl - Stored Procedures e Functions MySQL
-- ============================================================

USE labcontrol;

DELIMITER $$

-- ============================================================
-- FUNÇÃO: Próximo código de amostra por tipo e ano
-- Uso: SELECT FN_PROXIMO_COD_AMOSTRA(1, 2024);
-- ============================================================
DROP FUNCTION IF EXISTS FN_PROXIMO_COD_AMOSTRA$$
CREATE FUNCTION FN_PROXIMO_COD_AMOSTRA(
    p_id_amostras_tipo INT,
    p_ano INT
)
RETURNS INT
DETERMINISTIC
READS SQL DATA
BEGIN
    DECLARE v_proximo INT;

    SELECT COALESCE(MAX(COD_AMOSTRA), 0) + 1
      INTO v_proximo
      FROM LAB_HIST_AMOSTRAS
     WHERE ID_AMOSTRAS_TIPO = p_id_amostras_tipo
       AND ANO_AMOSTRA = p_ano;

    RETURN v_proximo;
END$$

-- ============================================================
-- FUNÇÃO: Próximo código de proposta por ano
-- Uso: SELECT FN_PROXIMO_COD_PROPOSTA(2024);
-- ============================================================
DROP FUNCTION IF EXISTS FN_PROXIMO_COD_PROPOSTA$$
CREATE FUNCTION FN_PROXIMO_COD_PROPOSTA(p_ano INT)
RETURNS INT
DETERMINISTIC
READS SQL DATA
BEGIN
    DECLARE v_proximo INT;

    SELECT COALESCE(MAX(COD_PROPOSTA), 0) + 1
      INTO v_proximo
      FROM LAB_PROPOSTAS
     WHERE ANO_PROPOSTA = p_ano;

    RETURN v_proximo;
END$$

-- ============================================================
-- FUNÇÃO: Código formatado da amostra (ex: Q001FQ2024)
-- ============================================================
DROP FUNCTION IF EXISTS FN_CODIGO_AMOSTRA$$
CREATE FUNCTION FN_CODIGO_AMOSTRA(
    p_id_amostras_tipo INT,
    p_cod_amostra INT,
    p_id_analises_tipo INT,
    p_ano INT
)
RETURNS VARCHAR(30)
DETERMINISTIC
READS SQL DATA
BEGIN
    DECLARE v_sigla_amostra VARCHAR(5);
    DECLARE v_sigla_analise VARCHAR(5);

    SELECT LEFT(DESCRICAO, 2) INTO v_sigla_amostra
      FROM AMOSTRAS_TIPO WHERE ID_AMOSTRAS_TIPO = p_id_amostras_tipo;

    SELECT LEFT(DESCRICAO, 2) INTO v_sigla_analise
      FROM ANALISES_TIPO WHERE ID_ANALISES_TIPO = p_id_analises_tipo;

    RETURN CONCAT(
        COALESCE(v_sigla_amostra, '?'),
        LPAD(p_cod_amostra, 3, '0'),
        COALESCE(v_sigla_analise, '?'),
        p_ano
    );
END$$

-- ============================================================
-- PROCEDURE: Registrar nova amostra (com entrada automática)
-- ============================================================
DROP PROCEDURE IF EXISTS SP_REGISTRAR_AMOSTRA$$
CREATE PROCEDURE SP_REGISTRAR_AMOSTRA(
    IN  p_id_amostras_tipo          INT,
    IN  p_id_analises_tipo          INT,
    IN  p_ano_amostra               INT,
    IN  p_id_entidades              INT,
    IN  p_nome_contato              VARCHAR(100),
    IN  p_id_lab_propostas          INT,
    IN  p_dt_entrega                DATETIME,
    IN  p_local_recebimento         VARCHAR(100),
    IN  p_id_embalagens_tipos       INT,
    IN  p_qtde_embalagens           INT,
    IN  p_id_produtos               INT,
    IN  p_nr_lote                   VARCHAR(50),
    IN  p_dt_fabricacao             DATE,
    IN  p_dt_validade               DATE,
    IN  p_id_amostras_status        INT,
    IN  p_id_entidades_func_dig     INT,
    IN  p_id_empresas               INT,
    OUT p_id_amostra                INT,
    OUT p_cod_amostra               INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- Gerar código sequencial
    SET p_cod_amostra = FN_PROXIMO_COD_AMOSTRA(p_id_amostras_tipo, p_ano_amostra);

    -- Inserir amostra
    INSERT INTO LAB_HIST_AMOSTRAS (
        ID_AMOSTRAS_TIPO, COD_AMOSTRA, ID_ANALISES_TIPO, ANO_AMOSTRA,
        ID_ENTIDADES, NOME_CONTATO, ID_LAB_PROPOSTAS, DT_ENTREGA,
        LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE,
        ID_PRODUTOS, NR_LOTE, DT_FABRICACAO, DT_VALIDADE,
        ID_AMOSTRAS_STATUS, ID_ENTIDADES_FUNC_DIG, ID_EMPRESAS
    ) VALUES (
        p_id_amostras_tipo, p_cod_amostra, p_id_analises_tipo, p_ano_amostra,
        p_id_entidades, p_nome_contato, p_id_lab_propostas, p_dt_entrega,
        p_local_recebimento, p_id_embalagens_tipos, p_qtde_embalagens,
        p_id_produtos, p_nr_lote, p_dt_fabricacao, p_dt_validade,
        p_id_amostras_status, p_id_entidades_func_dig, p_id_empresas
    );

    SET p_id_amostra = LAST_INSERT_ID();

    -- Criar movimentação de entrada automática
    INSERT INTO LAB_MOV_AMOSTRAS (
        ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS,
        DATA_MOV, QTDE, E_S, JUSTIFICATIVA
    ) VALUES (
        p_id_empresas, p_id_amostra,
        NOW(), COALESCE(p_qtde_embalagens, 1), 'E', 'Entrada inicial de amostra'
    );

    -- Inicializar registro de saldo
    INSERT INTO LAB_HIST_AMOSTRAS_SALDO (
        ID_LAB_HIST_AMOSTRAS, ID_EMPRESAS, SALDO_ATUAL, DATA_ATUALIZACAO
    ) VALUES (
        p_id_amostra, p_id_empresas, COALESCE(p_qtde_embalagens, 1), NOW()
    );

    -- Localização inicial
    INSERT INTO LAB_LOCAL_AMOSTRAS (
        ID_LAB_HIST_AMOSTRAS, ID_EMPRESAS, LOCAL
    ) VALUES (
        p_id_amostra, p_id_empresas, 'Recepção'
    );

    COMMIT;
END$$

-- ============================================================
-- PROCEDURE: Registrar movimentação de amostra
-- ============================================================
DROP PROCEDURE IF EXISTS SP_MOVIMENTAR_AMOSTRA$$
CREATE PROCEDURE SP_MOVIMENTAR_AMOSTRA(
    IN p_id_lab_hist_amostras   INT,
    IN p_id_empresas            INT,
    IN p_id_entidades_func      INT,
    IN p_qtde                   DECIMAL(15,4),
    IN p_entrada_saida          CHAR(1),    -- E ou S
    IN p_justificativa          VARCHAR(200)
)
BEGIN
    DECLARE v_saldo_atual DECIMAL(15,4);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- Verificar saldo disponível para saída
    IF p_entrada_saida = 'S' THEN
        SELECT SALDO_ATUAL INTO v_saldo_atual
          FROM LAB_HIST_AMOSTRAS_SALDO
         WHERE ID_LAB_HIST_AMOSTRAS = p_id_lab_hist_amostras;

        IF v_saldo_atual < p_qtde THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Saldo insuficiente para realizar a saída.';
        END IF;
    END IF;

    -- Registrar movimentação
    INSERT INTO LAB_MOV_AMOSTRAS (
        ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ID_ENTIDADES_FUNC,
        DATA_MOV, QTDE, E_S, JUSTIFICATIVA
    ) VALUES (
        p_id_empresas, p_id_lab_hist_amostras, p_id_entidades_func,
        NOW(), p_qtde, p_entrada_saida, p_justificativa
    );

    -- O trigger TRG_MOV_AMOSTRAS_AI atualiza o saldo automaticamente

    COMMIT;
END$$

-- ============================================================
-- PROCEDURE: Criar proposta
-- ============================================================
DROP PROCEDURE IF EXISTS SP_CRIAR_PROPOSTA$$
CREATE PROCEDURE SP_CRIAR_PROPOSTA(
    IN  p_id_entidades          INT,
    IN  p_id_empresas           INT,
    IN  p_dt_validade           DATE,
    IN  p_id_status             INT,
    IN  p_id_condicoes_pagtos   INT,
    IN  p_id_moedas             INT,
    IN  p_id_entidades_func     INT,
    OUT p_id_proposta           INT,
    OUT p_cod_proposta          INT
)
BEGIN
    DECLARE v_ano INT DEFAULT YEAR(NOW());

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    SET p_cod_proposta = FN_PROXIMO_COD_PROPOSTA(v_ano);

    INSERT INTO LAB_PROPOSTAS (
        ID_ENTIDADES, ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, REV_PROPOSTA,
        DT_SOLICITACAO, DT_VALIDADE, ID_LAB_PROPOSTAS_STATUS,
        VR_TOTAL_PROPOSTA, ID_ENTIDADES_FUNC, ID_CONDICOES_PAGTOS, ID_MOEDAS
    ) VALUES (
        p_id_entidades, p_id_empresas, p_cod_proposta, v_ano, 0,
        NOW(), p_dt_validade, p_id_status,
        0, p_id_entidades_func, p_id_condicoes_pagtos, p_id_moedas
    );

    SET p_id_proposta = LAST_INSERT_ID();
    COMMIT;
END$$

-- ============================================================
-- PROCEDURE: Gerar revisão de proposta
-- ============================================================
DROP PROCEDURE IF EXISTS SP_REVISAR_PROPOSTA$$
CREATE PROCEDURE SP_REVISAR_PROPOSTA(
    IN  p_id_proposta_original  INT,
    OUT p_id_nova_proposta      INT
)
BEGIN
    DECLARE v_id_entidades          INT;
    DECLARE v_id_empresas           INT;
    DECLARE v_cod_proposta          INT;
    DECLARE v_ano_proposta          INT;
    DECLARE v_rev_atual             INT;
    DECLARE v_id_status             INT;
    DECLARE v_id_condicoes_pagtos   INT;
    DECLARE v_id_moedas             INT;
    DECLARE v_id_entidades_func     INT;
    DECLARE v_dt_validade           DATE;

    SELECT ID_ENTIDADES, ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA,
           REV_PROPOSTA, ID_LAB_PROPOSTAS_STATUS, ID_CONDICOES_PAGTOS,
           ID_MOEDAS, ID_ENTIDADES_FUNC, DT_VALIDADE
      INTO v_id_entidades, v_id_empresas, v_cod_proposta, v_ano_proposta,
           v_rev_atual, v_id_status, v_id_condicoes_pagtos,
           v_id_moedas, v_id_entidades_func, v_dt_validade
      FROM LAB_PROPOSTAS
     WHERE ID_LAB_PROPOSTAS = p_id_proposta_original;

    INSERT INTO LAB_PROPOSTAS (
        ID_ENTIDADES, ID_EMPRESAS, COD_PROPOSTA, ANO_PROPOSTA, REV_PROPOSTA,
        DT_SOLICITACAO, DT_VALIDADE, ID_LAB_PROPOSTAS_STATUS,
        VR_TOTAL_PROPOSTA, ID_CONDICOES_PAGTOS, ID_MOEDAS, ID_ENTIDADES_FUNC
    ) VALUES (
        v_id_entidades, v_id_empresas, v_cod_proposta, v_ano_proposta,
        v_rev_atual + 1, NOW(), v_dt_validade, v_id_status,
        0, v_id_condicoes_pagtos, v_id_moedas, v_id_entidades_func
    );

    SET p_id_nova_proposta = LAST_INSERT_ID();

    -- Copiar itens da proposta original
    INSERT INTO LAB_PROPOSTAS_ANALISES (
        ID_LAB_PROPOSTAS, ID_PRODUTOS, ID_ANALISES_METODOS, ID_ANALISES_TIPO,
        ID_LAB_PARAMETROS_ANALISES, ID_IDIOMAS, QTDE_AMOSTRAS,
        VR_UNITARIO, VR_DESCONTO, VR_TOTAL, ID_PRAZOS
    )
    SELECT
        p_id_nova_proposta, ID_PRODUTOS, ID_ANALISES_METODOS, ID_ANALISES_TIPO,
        ID_LAB_PARAMETROS_ANALISES, ID_IDIOMAS, QTDE_AMOSTRAS,
        VR_UNITARIO, VR_DESCONTO, VR_TOTAL, ID_PRAZOS
      FROM LAB_PROPOSTAS_ANALISES
     WHERE ID_LAB_PROPOSTAS = p_id_proposta_original;
END$$

-- ============================================================
-- PROCEDURE: Recalcular total da proposta
-- ============================================================
DROP PROCEDURE IF EXISTS SP_RECALCULAR_PROPOSTA$$
CREATE PROCEDURE SP_RECALCULAR_PROPOSTA(IN p_id_proposta INT)
BEGIN
    DECLARE v_subtotal      DECIMAL(15,4);
    DECLARE v_porc_desc     DECIMAL(5,2);
    DECLARE v_desconto      DECIMAL(15,4);

    SELECT COALESCE(SUM(VR_TOTAL), 0)
      INTO v_subtotal
      FROM LAB_PROPOSTAS_ANALISES
     WHERE ID_LAB_PROPOSTAS = p_id_proposta;

    SELECT COALESCE(PORC_DESCONTO, 0)
      INTO v_porc_desc
      FROM LAB_PROPOSTAS
     WHERE ID_LAB_PROPOSTAS = p_id_proposta;

    SET v_desconto = v_subtotal * (v_porc_desc / 100);

    UPDATE LAB_PROPOSTAS
       SET VR_TOTAL_PROPOSTA = v_subtotal - v_desconto,
           VR_DESCONTO = v_desconto
     WHERE ID_LAB_PROPOSTAS = p_id_proposta;
END$$

-- ============================================================
-- VIEW: Resumo das amostras (para listagem principal)
-- ============================================================
DROP VIEW IF EXISTS VW_AMOSTRAS_RESUMO$$
CREATE VIEW VW_AMOSTRAS_RESUMO AS
SELECT
    a.ID_LAB_HIST_AMOSTRAS,
    a.ID_EMPRESAS,
    a.ANO_AMOSTRA,
    a.COD_AMOSTRA,
    at.DESCRICAO              AS TIPO_AMOSTRA,
    ant.DESCRICAO             AS TIPO_ANALISE,
    CONCAT(at2.DESCRICAO_ABREV, LPAD(a.COD_AMOSTRA,3,'0'),
           ant2.DESCRICAO_ABREV, a.ANO_AMOSTRA) AS COD_FORMATADO,
    e.NOME                    AS CLIENTE,
    CASE e.CATEGORIA
        WHEN 'F' THEN epf.CPF
        WHEN 'J' THEN epj.CNPJ
    END                       AS DOCUMENTO,
    p.DESCRICAO               AS PRODUTO,
    a.NR_LOTE,
    a.DT_ENTREGA,
    s.DESCRICAO               AS STATUS,
    s.COR                     AS COR_STATUS,
    sal.SALDO_ATUAL
FROM LAB_HIST_AMOSTRAS a
LEFT JOIN AMOSTRAS_TIPO   at   ON at.ID_AMOSTRAS_TIPO   = a.ID_AMOSTRAS_TIPO
LEFT JOIN ANALISES_TIPO   ant  ON ant.ID_ANALISES_TIPO   = a.ID_ANALISES_TIPO
LEFT JOIN ENTIDADES       e    ON e.ID_ENTIDADES         = a.ID_ENTIDADES
LEFT JOIN ENTIDADES_PF    epf  ON epf.ID_ENTIDADES       = e.ID_ENTIDADES
LEFT JOIN ENTIDADES_PJ    epj  ON epj.ID_ENTIDADES       = e.ID_ENTIDADES
LEFT JOIN PRODUTOS        p    ON p.ID_PRODUTOS          = a.ID_PRODUTOS
LEFT JOIN AMOSTRAS_STATUS s    ON s.ID_AMOSTRAS_STATUS   = a.ID_AMOSTRAS_STATUS
LEFT JOIN LAB_HIST_AMOSTRAS_SALDO sal ON sal.ID_LAB_HIST_AMOSTRAS = a.ID_LAB_HIST_AMOSTRAS
-- Sub-selects para abreviações (fallback simples)
LEFT JOIN (SELECT ID_AMOSTRAS_TIPO, LEFT(DESCRICAO,2) AS DESCRICAO_ABREV FROM AMOSTRAS_TIPO) at2
       ON at2.ID_AMOSTRAS_TIPO = a.ID_AMOSTRAS_TIPO
LEFT JOIN (SELECT ID_ANALISES_TIPO, LEFT(DESCRICAO,2) AS DESCRICAO_ABREV FROM ANALISES_TIPO) ant2
       ON ant2.ID_ANALISES_TIPO = a.ID_ANALISES_TIPO$$

-- ============================================================
-- VIEW: Resumo das propostas
-- ============================================================
DROP VIEW IF EXISTS VW_PROPOSTAS_RESUMO$$
CREATE VIEW VW_PROPOSTAS_RESUMO AS
SELECT
    p.ID_LAB_PROPOSTAS,
    p.ID_EMPRESAS,
    CONCAT(LPAD(p.COD_PROPOSTA,3,'0'), '/', p.ANO_PROPOSTA, '-R', p.REV_PROPOSTA) AS COD_FORMATADO,
    p.COD_PROPOSTA,
    p.ANO_PROPOSTA,
    p.REV_PROPOSTA,
    p.DT_SOLICITACAO,
    p.DT_VALIDADE,
    e.NOME              AS CLIENTE,
    ps.DESCRICAO        AS STATUS,
    ps.COR              AS COR_STATUS,
    m.SIGLA             AS MOEDA,
    p.VR_TOTAL_PROPOSTA,
    p.PORC_DESCONTO,
    COUNT(pa.ID_LAB_PROPOSTAS_ANALISES) AS QTD_ITENS
FROM LAB_PROPOSTAS p
LEFT JOIN ENTIDADES          e  ON e.ID_ENTIDADES           = p.ID_ENTIDADES
LEFT JOIN LAB_PROPOSTAS_STATUS ps ON ps.ID_LAB_PROPOSTAS_STATUS = p.ID_LAB_PROPOSTAS_STATUS
LEFT JOIN MOEDAS             m  ON m.ID_MOEDAS              = p.ID_MOEDAS
LEFT JOIN LAB_PROPOSTAS_ANALISES pa ON pa.ID_LAB_PROPOSTAS = p.ID_LAB_PROPOSTAS
GROUP BY p.ID_LAB_PROPOSTAS$$

-- ============================================================
-- VIEW: Movimentação detalhada de amostras
-- ============================================================
DROP VIEW IF EXISTS VW_MOV_AMOSTRAS$$
CREATE VIEW VW_MOV_AMOSTRAS AS
SELECT
    m.ID_LAB_MOV_AMOSTRAS,
    m.ID_LAB_HIST_AMOSTRAS,
    m.DATA_MOV,
    m.QTDE,
    m.E_S,
    CASE m.E_S WHEN 'E' THEN 'Entrada' ELSE 'Saída' END AS TIPO_MOV,
    m.JUSTIFICATIVA,
    m.AMOSTRA_COMPLEMENTAR,
    e.NOME AS FUNCIONARIO,
    sal.SALDO_ATUAL
FROM LAB_MOV_AMOSTRAS m
LEFT JOIN ENTIDADES e ON e.ID_ENTIDADES = m.ID_ENTIDADES_FUNC
LEFT JOIN LAB_HIST_AMOSTRAS_SALDO sal
       ON sal.ID_LAB_HIST_AMOSTRAS = m.ID_LAB_HIST_AMOSTRAS$$

-- ============================================================
-- PROCEDURE: Relatório de amostras por período
-- ============================================================
DROP PROCEDURE IF EXISTS SP_REL_AMOSTRAS_PERIODO$$
CREATE PROCEDURE SP_REL_AMOSTRAS_PERIODO(
    IN p_id_empresas    INT,
    IN p_dt_inicio      DATE,
    IN p_dt_fim         DATE,
    IN p_id_status      INT   -- NULL = todos
)
BEGIN
    SELECT
        a.ID_LAB_HIST_AMOSTRAS,
        FN_CODIGO_AMOSTRA(a.ID_AMOSTRAS_TIPO, a.COD_AMOSTRA, a.ID_ANALISES_TIPO, a.ANO_AMOSTRA) AS CODIGO,
        e.NOME          AS CLIENTE,
        at.DESCRICAO    AS TIPO_AMOSTRA,
        ant.DESCRICAO   AS TIPO_ANALISE,
        p.DESCRICAO     AS PRODUTO,
        a.NR_LOTE,
        a.DT_ENTREGA,
        s.DESCRICAO     AS STATUS,
        sal.SALDO_ATUAL
    FROM LAB_HIST_AMOSTRAS a
    LEFT JOIN AMOSTRAS_TIPO   at  ON at.ID_AMOSTRAS_TIPO  = a.ID_AMOSTRAS_TIPO
    LEFT JOIN ANALISES_TIPO   ant ON ant.ID_ANALISES_TIPO  = a.ID_ANALISES_TIPO
    LEFT JOIN ENTIDADES       e   ON e.ID_ENTIDADES        = a.ID_ENTIDADES
    LEFT JOIN PRODUTOS        p   ON p.ID_PRODUTOS         = a.ID_PRODUTOS
    LEFT JOIN AMOSTRAS_STATUS s   ON s.ID_AMOSTRAS_STATUS  = a.ID_AMOSTRAS_STATUS
    LEFT JOIN LAB_HIST_AMOSTRAS_SALDO sal ON sal.ID_LAB_HIST_AMOSTRAS = a.ID_LAB_HIST_AMOSTRAS
    WHERE a.ID_EMPRESAS = p_id_empresas
      AND DATE(a.DT_ENTREGA) BETWEEN p_dt_inicio AND p_dt_fim
      AND (p_id_status IS NULL OR a.ID_AMOSTRAS_STATUS = p_id_status)
    ORDER BY a.DT_ENTREGA DESC;
END$$

DELIMITER ;
