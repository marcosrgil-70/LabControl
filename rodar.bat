@echo off
title LabControl
echo.
echo  ============================================
echo   LabControl v2 - Sistema de Laboratorio
echo  ============================================
echo.
echo  Banco de dados: localhost:3306 / labcontrol_v2
echo  Conexao: user-secrets ConnectionStrings:LabControl
echo    (ver Scripts\v2\README.md)
echo.
echo  Acesse: http://localhost:5180
echo.
echo  Pressione Ctrl+C para encerrar o sistema.
echo  ============================================
echo.
cd /d "%~dp0"
dotnet run --launch-profile http
pause
