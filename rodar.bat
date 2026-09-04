@echo off
title LabControl
echo.
echo  ============================================
echo   LabControl - Sistema de Laboratorio
echo  ============================================
echo.
echo  Banco de dados: localhost:3306 / labcontrol
echo  Usuario MySQL:  root / root
echo.
echo  Acesse: http://localhost:5180
echo  Login:  ADMINISTRADOR / administrador
echo.
echo  Pressione Ctrl+C para encerrar o sistema.
echo  ============================================
echo.
cd /d "%~dp0"
dotnet run --launch-profile http
pause
