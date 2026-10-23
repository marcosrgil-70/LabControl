-- =====================================================================
-- LabControl v2 — dados de DESENVOLVIMENTO (nunca em produção/homologação)
-- Rodado por: .\Scripts\v2\recriar.ps1 -Dev   (depois de 01..05, no banco recriado)
-- Sem USE: o banco é escolhido na linha de comando.
--
-- Cria a empresa 001 (inicializada por SP_INICIALIZAR_EMPRESA) e o usuário
-- ADMINISTRADOR (ADMIN_GLOBAL, acesso a tudo) com a senha Admin123, sem troca
-- obrigatória (regra de desenvolvimento/testes, CLAUDE.md). Em produção os usuários vêm da migração (M-09) ou da T02.
-- =====================================================================

SET NAMES utf8mb4;

INSERT INTO EMPRESAS (COD_EMPRESA, RAZAO_SOCIAL, NOME_FANTASIA)
VALUES ('001', 'LABORATÓRIO (DESENVOLVIMENTO)', 'LABORATÓRIO DEV');
CALL SP_INICIALIZAR_EMPRESA(LAST_INSERT_ID());

-- hash PBKDF2 (PasswordHasher do ASP.NET Core) de "Admin123"
INSERT INTO USUARIOS (LOGIN, NOME, SENHA_HASH, ADMIN_GLOBAL, TROCAR_SENHA)
VALUES ('ADMINISTRADOR', 'ADMINISTRADOR', 'AQAAAAIAAYagAAAAEKSYHGIRWGJroqPcvrxd1AS7/qSn+L03osdsb8vprPS1ZxpCMkxU51Uu+SAB/RyyPQ==', 1, 0);
