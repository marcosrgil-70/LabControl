<#
.SYNOPSIS
  Gera o instalador do LabControl v2: instalador\saida\LabControl-Setup-<versão>.exe.

.DESCRIPTION
  1. publica o app (dotnet publish -c Release) em instalador\saida\app;
  2. confere a sintaxe e o BOM dos scripts PowerShell e do .iss;
  3. compila o LabControl.iss com o Inno Setup 6, passando os pré-requisitos de prerequisitos.json (nome, versão, URL,
     URL alternativa e SHA-256): o setup confere o que o servidor já tem e baixa só o que faltar;
  4. com -Offline: baixa/confere os pré-requisitos e copia para instalador\saida\prerequisitos — levar essa pasta ao
     lado do setup num servidor sem internet (o setup usa os arquivos de lá em vez de baixar).
  A saída e o cache dos pré-requisitos ficam fora do git (.gitignore).

.EXAMPLE
  .\instalador\construir.ps1
  .\instalador\construir.ps1 -Offline
  .\instalador\construir.ps1 -Versao 2.1.0
#>
[CmdletBinding()]
param(
  [string]$Versao = '',
  [string]$Iscc = '',
  [switch]$Offline
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$raiz = Split-Path -Parent $aqui
$saida = Join-Path $aqui 'saida'
$cache = Join-Path $aqui 'prerequisitos'
$prereqs = Get-Content (Join-Path $aqui 'prerequisitos.json') -Raw -Encoding UTF8 | ConvertFrom-Json

# versão: 2.0.<nº de commits> (o mesmo número também na versão do arquivo)
$commits = [int](& git -C $raiz rev-list --count HEAD)
if (-not $Versao) { $Versao = "2.0.$commits" }
$numerica = "$Versao.0" -replace '^(\d+\.\d+\.\d+)\.0\.0$', '$1.0'
if ($numerica -notmatch '^\d+\.\d+\.\d+\.\d+$') { $numerica = "2.0.$commits.0" }
Write-Host "LabControl $Versao" -ForegroundColor Cyan

# 1. publicação
$app = Join-Path $saida 'app'
if (Test-Path $app) { Remove-Item $app -Recurse -Force }
& dotnet publish (Join-Path $raiz 'LabControl.csproj') -c Release -o $app --nologo -p:Version=$Versao -p:FileVersion=$numerica
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou.' }
foreach ($proibido in 'appsettings.Development.json', 'appsettings.Production.json') {
  Remove-Item (Join-Path $app $proibido) -ErrorAction SilentlyContinue
}
if (Test-Path (Join-Path $app 'storage')) { Remove-Item (Join-Path $app 'storage') -Recurse -Force }
if (-not (Test-Path (Join-Path $app 'web.config'))) { throw 'a publicação não gerou o web.config.' }

# 2. sintaxe e BOM (PowerShell 5.1 e o ISCC leem arquivo sem BOM como ANSI: os acentos estragariam)
function TemBom([string]$caminho) { $b = [IO.File]::ReadAllBytes($caminho); return $b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF }
foreach ($s in (Get-ChildItem (Join-Path $aqui 'scripts') -Filter *.ps1) + (Get-Item (Join-Path $raiz 'Scripts\v2\backup.ps1'), (Join-Path $raiz 'Scripts\v2\recriar.ps1'), (Join-Path $raiz 'Scripts\v2\atualizar.ps1'))) {
  $erros = $null
  [void][Management.Automation.Language.Parser]::ParseFile($s.FullName, [ref]$null, [ref]$erros)
  if ($erros) { throw "$($s.Name): $($erros[0].Message) (linha $($erros[0].Extent.StartLineNumber))" }
  if (-not (TemBom $s.FullName)) { throw "$($s.Name) sem BOM UTF-8." }
}
if (-not (TemBom (Join-Path $aqui 'LabControl.iss'))) { throw 'LabControl.iss sem BOM UTF-8.' }
Write-Host '  scripts ok' -ForegroundColor Green

# 4. kit offline (opcional)
$kit = Join-Path $saida 'prerequisitos'
if (Test-Path $kit) { Remove-Item $kit -Recurse -Force }
if ($Offline) {
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  New-Item -ItemType Directory -Force -Path $cache, $kit | Out-Null
  foreach ($p in $prereqs) {
    $arquivo = Join-Path $cache $p.arquivo
    if (-not (Test-Path $arquivo)) {
      Write-Host "  baixando $($p.arquivo)..."
      try { Invoke-WebRequest $p.url -OutFile $arquivo -UseBasicParsing }
      catch { if (-not $p.url_alternativa) { throw }; Invoke-WebRequest $p.url_alternativa -OutFile $arquivo -UseBasicParsing }
    }
    $hash = (Get-FileHash $arquivo -Algorithm SHA256).Hash
    if ($hash -ne $p.sha256) { Remove-Item $arquivo; throw "$($p.arquivo): SHA-256 diferente do esperado ($hash). Arquivo apagado; atualize prerequisitos.json." }
    Copy-Item $arquivo $kit
    Write-Host "  ok $($p.arquivo)" -ForegroundColor Green
  }
}

# 3. Inno Setup
if (-not $Iscc) {
  $Iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
          Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $Iscc) { throw 'Inno Setup 6 não encontrado (winget install JRSoftware.InnoSetup) — ou informe -Iscc.' }
$defines = @("/DVersao=$Versao", "/DVersaoNumerica=$numerica", "/DSaida=$saida")
foreach ($p in $prereqs) {
  # o VC++ vem de um link que sempre aponta para a versão atual: no setup, sem hash (o Instalar.ps1 confere a assinatura)
  $sha = if ($p.sem_hash_no_setup) { '' } else { $p.sha256.ToLowerInvariant() }
  $defines += "/D$($p.id)Arquivo=$($p.arquivo)", "/D$($p.id)Url=$($p.url)", "/D$($p.id)Url2=$($p.url_alternativa)",
              "/D$($p.id)Sha=$sha", "/D$($p.id)Versao=$($p.versao)"
}
& $Iscc /Q @defines (Join-Path $aqui 'LabControl.iss')
if ($LASTEXITCODE -ne 0) { throw 'ISCC falhou.' }
$exe = Join-Path $saida "LabControl-Setup-$Versao.exe"
Write-Host ("Instalador: {0} ({1:n0} MB)" -f $exe, ((Get-Item $exe).Length / 1MB)) -ForegroundColor Green
Write-Host ("SHA-256: {0}" -f (Get-FileHash $exe -Algorithm SHA256).Hash)
if ($Offline) { Write-Host "Kit sem internet: copie $kit para ao lado do setup." -ForegroundColor Green }
