-- =====================================================================
-- LabControl v2 — dados de DESENVOLVIMENTO (nunca em produção/homologação)
-- Rodado por: .\Scripts\v2\recriar.ps1 -Dev   (depois de 01..05, no banco recriado)
-- Sem USE: o banco é escolhido na linha de comando.
--
-- Cria a empresa 001 (inicializada por SP_INICIALIZAR_EMPRESA) e o usuário ADMINISTRADOR (ADMIN_GLOBAL,
-- acesso a tudo) com a senha Admin123, sem troca obrigatória, vinculado ao profissional "USUÁRIO ESPECIAL DO
-- SISTEMA" (regra de desenvolvimento/testes, CLAUDE.md). Em produção os usuários vêm da migração (M-09) ou da T02.
-- =====================================================================

SET NAMES utf8mb4;

INSERT INTO EMPRESAS (COD_EMPRESA, RAZAO_SOCIAL, NOME_FANTASIA)
VALUES ('001', 'LABORATÓRIO (DESENVOLVIMENTO)', 'LABORATÓRIO DEV');
CALL SP_INICIALIZAR_EMPRESA(LAST_INSERT_ID());

-- hash PBKDF2 (PasswordHasher do ASP.NET Core) de "Admin123"
INSERT INTO USUARIOS (LOGIN, NOME, SENHA_HASH, ADMIN_GLOBAL, TROCAR_SENHA)
VALUES ('ADMINISTRADOR', 'ADMINISTRADOR', 'AQAAAAIAAYagAAAAEKSYHGIRWGJroqPcvrxd1AS7/qSn+L03osdsb8vprPS1ZxpCMkxU51Uu+SAB/RyyPQ==', 1, 0);

-- Profissional "USUÁRIO ESPECIAL DO SISTEMA" vinculado ao ADMINISTRADOR em cada empresa (pedido do usuário,
-- 23/10/2026): documentos que registram o profissional do usuário (proposta, amostra…) também podem ser gravados
-- por ele. Idempotente: cria só o que faltar (também serve para acertar um banco de desenvolvimento existente).
INSERT INTO ENTIDADES (ID_EMPRESAS, CATEGORIA, NOME, TIPO_FUNCIONARIO)
SELECT E.ID_EMPRESAS, 'F', 'USUÁRIO ESPECIAL DO SISTEMA', 1 FROM EMPRESAS E
 WHERE NOT EXISTS (SELECT 1 FROM ENTIDADES X WHERE X.ID_EMPRESAS = E.ID_EMPRESAS AND X.NOME = 'USUÁRIO ESPECIAL DO SISTEMA');
UPDATE ENTIDADES SET TIPO_FUNCIONARIO = 1, INATIVO = 0 WHERE NOME = 'USUÁRIO ESPECIAL DO SISTEMA';
INSERT INTO ENTIDADES_FUNCIONARIOS (ID_ENTIDADES, ID_EMPRESAS, EXECUTA_ANALISE)
SELECT X.ID_ENTIDADES, X.ID_EMPRESAS, 0 FROM ENTIDADES X
 WHERE X.NOME = 'USUÁRIO ESPECIAL DO SISTEMA'
   AND NOT EXISTS (SELECT 1 FROM ENTIDADES_FUNCIONARIOS F WHERE F.ID_ENTIDADES = X.ID_ENTIDADES);
INSERT INTO USUARIOS_EMPRESAS (ID_USUARIOS, ID_EMPRESAS, ID_ENTIDADES_FUNC, PADRAO, ATIVO)
SELECT U.ID_USUARIOS, X.ID_EMPRESAS, X.ID_ENTIDADES, 1, 1
  FROM USUARIOS U JOIN ENTIDADES X ON X.NOME = 'USUÁRIO ESPECIAL DO SISTEMA'
 WHERE U.LOGIN = 'ADMINISTRADOR'
ON DUPLICATE KEY UPDATE ID_ENTIDADES_FUNC = VALUES(ID_ENTIDADES_FUNC), ATIVO = 1;
