<#
.SYNOPSIS
  Carrega o legado (Firebird) no banco de trabalho LabControlDB: APAGA o banco, recria pelos scripts como o instalador
  deixa e migra o legado nele pela T07 (M-01…M-18).

.DESCRIPTION
  O LabControlDB é o único banco permanente do desenvolvimento (decisão do usuário, 30/10/2026): programar, rodar o
  sistema e testar à mão com os dados reais. Os testes automáticos usam bancos temporários (lc_teste_*), criados e
  apagados a cada "dotnet test".
  Leva ~11 min com a cópia do legado de 29/10/2026. No fim: login ADMINISTRADOR / Admin123 (sem troca); os outros
  usuários migrados entram com a senha provisória Mudar@2026 e trocam no 1º acesso.
  O legado guardava o selo da acreditação e o rodapé do boletim dentro do relatório: -Selo, -NrAcreditacao e -Rodape
  gravam na empresa migrada (o mesmo que a tela Empresa faria).
  Para só acrescentar uma mudança dos scripts sem perder os dados, use o atualizar.ps1.

.EXAMPLE
  .\Scripts\v2\carregar-legado.ps1
  .\Scripts\v2\carregar-legado.ps1 -Selo .\exemplos\Boletins\Logo_RelINMETRO.bmp -NrAcreditacao "CRL 1170" -Rodape "FORM 0069 - R2|Emitente: Gerência da Qualidade"
#>
[CmdletBinding()]
param(
  [string]$Fdb = 'C:\Project\Laboratorio\DBMAIS.FDB',
  [string]$Banco = 'LabControlDB',
  [string]$Selo = '',
  [string]$NrAcreditacao = '',
  [string]$Rodape = '',
  [switch]$Forcar
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path (Split-Path $PSScriptRoot)
if (-not (Test-Path $Fdb)) { Write-Host "ERRO: legado não encontrado: $Fdb" -ForegroundColor Red; exit 1 }
if ($Selo -and -not (Test-Path $Selo)) { Write-Host "ERRO: selo não encontrado: $Selo" -ForegroundColor Red; exit 1 }
if (-not $Forcar) {
  $r = Read-Host "O banco '$Banco' será APAGADO e carregado de novo com o legado ($Fdb). Digite o nome do banco para confirmar"
  if ($r -ne $Banco) { Write-Host 'Cancelado.' -ForegroundColor Yellow; exit 1 }
}
$env:LABCONTROL_CARREGAR_FDB = (Resolve-Path $Fdb).Path
$env:LABCONTROL_CARREGAR_BANCO = $Banco
$env:LABCONTROL_CARREGAR_SELO = if ($Selo) { (Resolve-Path $Selo).Path } else { '' }
$env:LABCONTROL_CARREGAR_ACREDITACAO = $NrAcreditacao
$env:LABCONTROL_CARREGAR_RODAPE = $Rodape
try {
  Write-Host "Carregando $Fdb em $Banco (cerca de 11 min)..." -ForegroundColor Cyan
  & dotnet test (Join-Path $raiz 'tests\LabControl.Tests') --filter 'FullyQualifiedName~FerramentasBancoTests.Carrega_o_legado_no_banco_de_trabalho' `
      --logger 'console;verbosity=detailed'
  if ($LASTEXITCODE -ne 0) { Write-Host "ERRO: a carga falhou (veja acima)." -ForegroundColor Red; exit 1 }
  Write-Host "Banco $Banco carregado. Login: ADMINISTRADOR / Admin123." -ForegroundColor Green
} finally {
  'LABCONTROL_CARREGAR_FDB', 'LABCONTROL_CARREGAR_BANCO', 'LABCONTROL_CARREGAR_SELO', 'LABCONTROL_CARREGAR_ACREDITACAO', 'LABCONTROL_CARREGAR_RODAPE' |
    ForEach-Object { Remove-Item "env:$_" -ErrorAction SilentlyContinue }
}
