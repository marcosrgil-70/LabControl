<#
.SYNOPSIS
  Gera o instalador do LabControl v2: instalador\saida\LabControl-Setup-<versão>.exe.

.DESCRIPTION
  1. confere (ou baixa) os pré-requisitos offline de prerequisitos.json, pelo SHA-256;
  2. publica o app (dotnet publish -c Release) em instalador\saida\app;
  3. confere a sintaxe dos scripts PowerShell do pacote;
  4. compila o LabControl.iss com o Inno Setup 6 (ISCC.exe).
  Os pré-requisitos e a saída ficam fora do git (.gitignore).

.EXAMPLE
  .\instalador\construir.ps1
  .\instalador\construir.ps1 -Versao 2.0.1
#>
[CmdletBinding()]
param(
  [string]$Versao = '',
  [string]$Iscc = ''
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$raiz = Split-Path -Parent $aqui
$saida = Join-Path $aqui 'saida'
$prereq = Join-Path $aqui 'prerequisitos'

# versão: 2.0.<nº de commits> (o mesmo número também na versão do arquivo)
$commits = [int](& git -C $raiz rev-list --count HEAD)
if (-not $Versao) { $Versao = "2.0.$commits" }
$numerica = "$Versao.0" -replace '^(\d+\.\d+\.\d+)\.0\.0$', '$1.0'
if ($numerica -notmatch '^\d+\.\d+\.\d+\.\d+$') { $numerica = "2.0.$commits.0" }
Write-Host "LabControl $Versao" -ForegroundColor Cyan

# 1. pré-requisitos
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Force -Path $prereq | Out-Null
foreach ($p in (Get-Content (Join-Path $aqui 'prerequisitos.json') -Raw | ConvertFrom-Json)) {
  $arquivo = Join-Path $prereq $p.arquivo
  if (-not (Test-Path $arquivo)) {
    Write-Host "  baixando $($p.arquivo)..."
    Invoke-WebRequest $p.url -OutFile $arquivo -UseBasicParsing
  }
  $hash = (Get-FileHash $arquivo -Algorithm SHA256).Hash
  if ($hash -ne $p.sha256) { Remove-Item $arquivo; throw "$($p.arquivo): SHA-256 diferente do esperado ($hash). Arquivo apagado; confira a URL/versão em prerequisitos.json." }
  Write-Host "  ok $($p.arquivo)" -ForegroundColor Green
}
# só os pré-requisitos da lista vão no pacote
$lista = (Get-Content (Join-Path $aqui 'prerequisitos.json') -Raw | ConvertFrom-Json).arquivo
Get-ChildItem $prereq -File | Where-Object Name -notin $lista | ForEach-Object { throw "arquivo fora de prerequisitos.json em $prereq : $($_.Name)" }

# 2. publicação
$app = Join-Path $saida 'app'
if (Test-Path $app) { Remove-Item $app -Recurse -Force }
& dotnet publish (Join-Path $raiz 'LabControl.csproj') -c Release -o $app --nologo -p:Version=$Versao -p:FileVersion=$numerica
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou.' }
foreach ($proibido in 'appsettings.Development.json', 'appsettings.Production.json') {
  Remove-Item (Join-Path $app $proibido) -ErrorAction SilentlyContinue
}
if (Test-Path (Join-Path $app 'storage')) { Remove-Item (Join-Path $app 'storage') -Recurse -Force }
if (-not (Test-Path (Join-Path $app 'web.config'))) { throw 'a publicação não gerou o web.config.' }

# 3. sintaxe dos scripts
foreach ($s in (Get-ChildItem (Join-Path $aqui 'scripts') -Filter *.ps1) + (Get-Item (Join-Path $raiz 'Scripts\v2\backup.ps1'), (Join-Path $raiz 'Scripts\v2\recriar.ps1'))) {
  $erros = $null
  [void][Management.Automation.Language.Parser]::ParseFile($s.FullName, [ref]$null, [ref]$erros)
  if ($erros) { throw "$($s.Name): $($erros[0].Message) (linha $($erros[0].Extent.StartLineNumber))" }
  # PowerShell 5.1 lê sem BOM como ANSI: os acentos das mensagens precisam do BOM
  $b = [IO.File]::ReadAllBytes($s.FullName)
  if (-not ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)) { throw "$($s.Name) sem BOM UTF-8." }
}
Write-Host '  scripts ok' -ForegroundColor Green

# 4. Inno Setup
if (-not $Iscc) {
  $Iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
          Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $Iscc) { throw 'Inno Setup 6 não encontrado (winget install JRSoftware.InnoSetup) — ou informe -Iscc.' }
& $Iscc /Q "/DVersao=$Versao" "/DVersaoNumerica=$numerica" "/DSaida=$saida" (Join-Path $aqui 'LabControl.iss')
if ($LASTEXITCODE -ne 0) { throw 'ISCC falhou.' }
$exe = Join-Path $saida "LabControl-Setup-$Versao.exe"
Write-Host ("Instalador: {0} ({1:n0} MB)" -f $exe, ((Get-Item $exe).Length / 1MB)) -ForegroundColor Green
Write-Host ("SHA-256: {0}" -f (Get-FileHash $exe -Algorithm SHA256).Hash)
