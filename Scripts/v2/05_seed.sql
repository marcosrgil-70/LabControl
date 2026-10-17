-- =====================================================================
-- LabControl v2 — Carga inicial (catálogo de permissões + Brasil/UFs)
-- Empresas, usuários e cadastros vêm da MIGRAÇÃO (docs/banco/13-MIGRACAO-DADOS.md)
-- ou do cadastro de nova empresa (que chama SP_INICIALIZAR_EMPRESA).
-- =====================================================================

USE labcontrol_v2;

-- ---------------------------------------------------------------------
-- Brasil + 27 UFs (códigos IBGE). Cidades: importar a tabela IBGE completa
-- (ver docs/banco/13-MIGRACAO-DADOS.md, passo M-02).
-- ---------------------------------------------------------------------
INSERT IGNORE INTO PAISES (ID_PAISES, NOME, SIGLA, CODIGO_BACEN) VALUES (1, 'BRASIL', 'BR', '1058');

INSERT IGNORE INTO ESTADOS (ID_PAISES, NOME, SIGLA, CODIGO_IBGE) VALUES
 (1,'RONDÔNIA','RO','11'),(1,'ACRE','AC','12'),(1,'AMAZONAS','AM','13'),(1,'RORAIMA','RR','14'),
 (1,'PARÁ','PA','15'),(1,'AMAPÁ','AP','16'),(1,'TOCANTINS','TO','17'),(1,'MARANHÃO','MA','21'),
 (1,'PIAUÍ','PI','22'),(1,'CEARÁ','CE','23'),(1,'RIO GRANDE DO NORTE','RN','24'),(1,'PARAÍBA','PB','25'),
 (1,'PERNAMBUCO','PE','26'),(1,'ALAGOAS','AL','27'),(1,'SERGIPE','SE','28'),(1,'BAHIA','BA','29'),
 (1,'MINAS GERAIS','MG','31'),(1,'ESPÍRITO SANTO','ES','32'),(1,'RIO DE JANEIRO','RJ','33'),
 (1,'SÃO PAULO','SP','35'),(1,'PARANÁ','PR','41'),(1,'SANTA CATARINA','SC','42'),
 (1,'RIO GRANDE DO SUL','RS','43'),(1,'MATO GROSSO DO SUL','MS','50'),(1,'MATO GROSSO','MT','51'),
 (1,'GOIÁS','GO','52'),(1,'DISTRITO FEDERAL','DF','53');

-- ---------------------------------------------------------------------
-- Catálogo de permissões (tela.ação). Ações padrão: CONSULTAR, INCLUIR,
-- ALTERAR, EXCLUIR, IMPRIMIR + ações especiais de cada tela.
-- ---------------------------------------------------------------------
INSERT IGNORE INTO PERMISSOES (CODIGO, MODULO, TELA, ACAO, DESCRICAO, ORDEM)
SELECT CONCAT(T.TELA, '.', A.ACAO), T.MODULO, T.TELA, A.ACAO, CONCAT(T.NOME, ' — ', A.NOME), T.ORDEM * 10 + A.ORDEM
  FROM (
        SELECT 'SISTEMA' MODULO, 'EMPRESAS' TELA, 'Empresas' NOME, 1 ORDEM UNION ALL
        SELECT 'SISTEMA', 'PARAMETROS_EMPRESA', 'Parâmetros da Empresa', 2 UNION ALL
        SELECT 'SISTEMA', 'USUARIOS', 'Usuários', 3 UNION ALL
        SELECT 'SISTEMA', 'PERFIS', 'Perfis de Acesso', 4 UNION ALL
        SELECT 'SISTEMA', 'AUDITORIA', 'Auditoria', 5 UNION ALL
        SELECT 'CADASTROS', 'AUXILIARES', 'Cadastros Auxiliares', 10 UNION ALL
        SELECT 'CADASTROS', 'GEOGRAFIA', 'Países/Estados/Cidades', 11 UNION ALL
        SELECT 'CADASTROS', 'CLIENTES', 'Clientes/Fornecedores', 12 UNION ALL
        SELECT 'CADASTROS', 'PROFISSIONAIS', 'Profissionais', 13 UNION ALL
        SELECT 'CADASTROS', 'PRODUTOS', 'Produtos', 14 UNION ALL
        SELECT 'CADASTROS', 'PARAMETROS_ANALISE', 'Parâmetros de Análise', 15 UNION ALL
        SELECT 'LABORATORIO', 'PROPOSTAS', 'Propostas', 20 UNION ALL
        SELECT 'LABORATORIO', 'PEDIDOS_VENDA', 'Pedidos de Venda', 21 UNION ALL
        SELECT 'LABORATORIO', 'AMOSTRAS', 'Recebimento de Amostras', 22 UNION ALL
        SELECT 'LABORATORIO', 'MOVIMENTACAO', 'Movimentação de Amostras', 23 UNION ALL
        SELECT 'LABORATORIO', 'LOCALIZACAO', 'Localização de Amostras', 24 UNION ALL
        SELECT 'LABORATORIO', 'ETIQUETAS', 'Etiquetas de Amostras', 25 UNION ALL
        SELECT 'LABORATORIO', 'RESULTADOS', 'Resultados', 26 UNION ALL
        SELECT 'LABORATORIO', 'BOLETINS', 'Boletins', 27 UNION ALL
        SELECT 'LABORATORIO', 'RELATORIO_ENSAIO', 'Relatório de Ensaio', 28 UNION ALL
        SELECT 'RELATORIOS', 'RELATORIOS', 'Relatórios Gerenciais', 30
       ) T
  JOIN (
        SELECT 'CONSULTAR' ACAO, 'Consultar' NOME, 1 ORDEM UNION ALL
        SELECT 'INCLUIR', 'Incluir', 2 UNION ALL
        SELECT 'ALTERAR', 'Alterar', 3 UNION ALL
        SELECT 'EXCLUIR', 'Excluir', 4 UNION ALL
        SELECT 'IMPRIMIR', 'Imprimir', 5
       ) A
 WHERE NOT (T.TELA = 'RELATORIOS' AND A.ACAO IN ('INCLUIR','ALTERAR','EXCLUIR'))
   AND NOT (T.TELA = 'AUDITORIA' AND A.ACAO IN ('INCLUIR','ALTERAR','EXCLUIR'));

INSERT IGNORE INTO PERMISSOES (CODIGO, MODULO, TELA, ACAO, DESCRICAO, ORDEM) VALUES
 ('PROPOSTAS.REVISAR',            'LABORATORIO', 'PROPOSTAS',       'REVISAR',   'Propostas — Gerar revisão', 206),
 ('PEDIDOS_VENDA.GERAR',          'LABORATORIO', 'PEDIDOS_VENDA',   'GERAR',     'Pedidos de Venda — Gerar a partir da proposta', 216),
 ('PEDIDOS_VENDA.CANCELAR',       'LABORATORIO', 'PEDIDOS_VENDA',   'CANCELAR',  'Pedidos de Venda — Cancelar', 217),
 ('AMOSTRAS.REVISAR',             'LABORATORIO', 'AMOSTRAS',        'REVISAR',   'Amostras — Gerar revisão', 226),
 ('AMOSTRAS.ENTREGA_RETROATIVA',  'LABORATORIO', 'AMOSTRAS',        'RETROATIVA','Amostras — Data de entrega além do limite de dias', 227),
 ('RESULTADOS.REVISAR',           'LABORATORIO', 'RESULTADOS',      'REVISAR',   'Resultados — Gerar revisão', 266),
 ('RESULTADOS.DESFAZER_REVISAO',  'LABORATORIO', 'RESULTADOS',      'DESFAZER',  'Resultados — Desfazer última revisão', 267),
 ('BOLETINS.EMITIR',              'LABORATORIO', 'BOLETINS',        'EMITIR',    'Boletins — Emitir (congelar)', 276),
 ('BOLETINS.REVISAR',             'LABORATORIO', 'BOLETINS',        'REVISAR',   'Boletins — Gerar revisão', 277),
 ('BOLETINS.DESFAZER_REVISAO',    'LABORATORIO', 'BOLETINS',        'DESFAZER',  'Boletins — Desfazer última revisão', 278);
