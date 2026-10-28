<#
.SYNOPSIS
  Recria do zero um banco LabControl v2 a partir de Scripts/v2/01..06 e,
  opcionalmente, roda o roteiro de teste do doc 12 §7.

.DESCRIPTION
  1. Apaga o banco informado (pede confirmação se ele já existir);
  2. executa 01_schema -> 02_funcoes_procedures -> 03_triggers -> 04_views -> 05_seed -> 06_cidades,
     trocando "labcontrol_v2" pelo nome do banco, e PARA no primeiro erro;
  3. confere as contagens de objetos (59 tabelas, 10 views, 8 funções,
     26 procedures, 48 triggers, 109 permissões, 5.571 cidades);
  4. com -Dev: roda dev/*.sql (empresa 001 + usuário ADMINISTRADOR / senha
     Admin123, só para desenvolvimento);
  5. com -Teste: cria um banco temporário labcontrol_teste_<id>, roda
     teste/roteiro_12_7.sql e apaga o banco no final (a não ser que -Manter).

  Senha: MYSQL_PWD, se definida; senão a do user-secret ConnectionStrings:LabControl
  (fora do repositório, a mesma do app e dos testes) quando o usuário é o mesmo;
  senão é pedida no console. Nunca fica em arquivo versionado.

.EXAMPLE
  .\Scripts\v2\recriar.ps1                       # recria labcontrol_v2
  .\Scripts\v2\recriar.ps1 -Dev                  # recria labcontrol_v2 com empresa 001 + usuário ADMIN
  .\Scripts\v2\recriar.ps1 -Teste                # banco temporário + roteiro 12 §7
  .\Scripts\v2\recriar.ps1 -Banco labcontrol_v2_hml -Forcar
#>
[CmdletBinding()]
param(
  [string]$Banco = 'labcontrol_v2',
  [string]$Servidor = '127.0.0.1',
  [int]$Porta = 3306,
  [string]$Usuario = 'root',
  [string]$MySql = '',
  [switch]$Teste,
  [switch]$Dev,
  [switch]$Manter,
  [switch]$Forcar
)

$ErrorActionPreference = 'Stop'
$pasta = $PSScriptRoot
$scripts = '01_schema', '02_funcoes_procedures', '03_triggers', '04_views', '05_seed', '06_cidades'
$esperado = [ordered]@{ TABELAS = 59; VIEWS = 10; FUNCOES = 8; PROCEDURES = 27; TRIGGERS = 48; PERMISSOES = 109; CIDADES = 5571 }

function Falhar([string]$msg) { Write-Host "ERRO: $msg" -ForegroundColor Red; exit 1 }

# --- banco ---------------------------------------------------------------
if ($Teste) { $Banco = 'labcontrol_teste_' + [guid]::NewGuid().ToString('N').Substring(0, 8) }
if ($Banco -notmatch '^[A-Za-z0-9_]+$') { Falhar "nome de banco inválido: $Banco" }
if ($Banco -in 'labcontrol', 'mysql', 'sys', 'information_schema', 'performance_schema') {
  Falhar "o banco '$Banco' não pode ser recriado por este script (labcontrol = v1)."
}

# --- cliente mysql -------------------------------------------------------
if (-not $MySql) {
  $cmd = Get-Command mysql.exe -ErrorAction SilentlyContinue
  $MySql = if ($cmd) { $cmd.Source } else { 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe' }
}
if (-not (Test-Path $MySql)) { Falhar "mysql.exe não encontrado ($MySql). Informe -MySql <caminho>." }

# --- senha ---------------------------------------------------------------
$senhaAntes = $env:MYSQL_PWD
# 1º MYSQL_PWD; 2º a senha do user-secret ConnectionStrings:LabControl (a mesma do app e dos testes,
# fora do repositório) quando o usuário bate; 3º pergunta no console
if (-not $env:MYSQL_PWD) {
  $csproj = Join-Path $pasta '..\..\LabControl.csproj'
  if ((Test-Path $csproj) -and ((Get-Content $csproj -Raw) -match '<UserSecretsId>([^<]+)</UserSecretsId>')) {
    $segredos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$($Matches[1])\secrets.json"
    if (Test-Path $segredos) {
      $cs = (Get-Content $segredos -Raw | ConvertFrom-Json).'ConnectionStrings:LabControl'
      if ($cs -and $cs -match '(?i)(^|;)\s*(User|Uid|User Id)\s*=\s*([^;]+)' -and $Matches[3].Trim() -eq $Usuario -and
          $cs -match '(?i)(^|;)\s*(Password|Pwd)\s*=\s*([^;]*)') {
        $env:MYSQL_PWD = $Matches[3]
        Write-Host "Senha lida do user-secret ConnectionStrings:LabControl." -ForegroundColor DarkGray
      }
    }
  }
}
if (-not $env:MYSQL_PWD) {
  $seg = Read-Host "Senha do usuário '$Usuario' em ${Servidor}:$Porta" -AsSecureString
  $env:MYSQL_PWD = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
}

$base = @("-u$Usuario", "-h$Servidor", "-P$Porta", '--default-character-set=utf8mb4')
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('labcontrol_recriar_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
$utf8 = New-Object Text.UTF8Encoding($false)

# Lê um arquivo de saída do mysql. O Start-Process -Wait pode voltar antes de o handle do redirecionamento
# ser liberado ("usado por outro processo", pendência 19): abre compartilhado e tenta de novo por até 10 s.
function LerSaida([string]$caminho) {
  for ($i = 1; ; $i++) {
    try {
      $fs = New-Object IO.FileStream($caminho, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
      try { return (New-Object IO.StreamReader($fs, $utf8)).ReadToEnd() } finally { $fs.Dispose() }
    } catch [IO.IOException] {
      if ($i -ge 50) { throw }
      Start-Sleep -Milliseconds 200
    }
  }
}

# Executa SQL (texto ou arquivo) no mysql; devolve a saída ou para no erro.
function Sql([string]$texto, [string]$arquivo, [string[]]$extra = @()) {
  if (-not $arquivo) {
    $arquivo = Join-Path $tmp 'cmd.sql'
    [IO.File]::WriteAllText($arquivo, $texto, $utf8)
  }
  $saida = Join-Path $tmp 'saida.txt'
  $erro = Join-Path $tmp 'erro.txt'
  $p = Start-Process -FilePath $MySql -ArgumentList ($base + $extra) -RedirectStandardInput $arquivo `
         -RedirectStandardOutput $saida -RedirectStandardError $erro -NoNewWindow -Wait -PassThru
  $out = LerSaida $saida
  $err = ((LerSaida $erro) -split "`r?`n" | Where-Object { $_ -and $_ -notmatch 'Using a password' }) -join "`n"
  if ($p.ExitCode -ne 0) { throw "mysql (código $($p.ExitCode)): $err`n$out" }
  return $out
}

$sucesso = $false
try {
  Write-Host "Servidor: ${Servidor}:$Porta  Banco: $Banco" -ForegroundColor Cyan
  [void](Sql 'SELECT 1;')

  $existe = (Sql "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = '$Banco';" '' @('-N')).Trim()
  if ($existe -eq '1' -and -not $Forcar -and -not $Teste) {
    $r = Read-Host "O banco '$Banco' já existe e será APAGADO com todos os dados. Digite o nome do banco para confirmar"
    if ($r -ne $Banco) { Falhar 'cancelado pelo usuário.' }
  }
  [void](Sql "DROP DATABASE IF EXISTS ``$Banco``;")

  foreach ($s in $scripts) {
    $origem = Join-Path $pasta "$s.sql"
    $destino = Join-Path $tmp "$s.sql"
    $texto = [IO.File]::ReadAllText($origem, $utf8) -replace '\blabcontrol_v2\b', $Banco
    [IO.File]::WriteAllText($destino, $texto, $utf8)
    $t = [Diagnostics.Stopwatch]::StartNew()
    try { [void](Sql '' $destino) } catch { Write-Host ("  {0,-26} ERRO" -f "$s.sql") -ForegroundColor Red; throw "$s.sql: $($_.Exception.Message)" }
    Write-Host ("  {0,-26} ok ({1:n0}s)" -f "$s.sql", $t.Elapsed.TotalSeconds) -ForegroundColor Green
  }

  $contagem = Sql @"
SELECT
 (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$Banco' AND TABLE_TYPE = 'BASE TABLE'),
 (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$Banco' AND TABLE_TYPE = 'VIEW'),
 (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$Banco' AND ROUTINE_TYPE = 'FUNCTION'),
 (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$Banco' AND ROUTINE_TYPE = 'PROCEDURE'),
 (SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = '$Banco'),
 (SELECT COUNT(*) FROM ``$Banco``.PERMISSOES),
 (SELECT COUNT(*) FROM ``$Banco``.CIDADES);
"@ '' @('-N')
  $valores = $contagem.Trim() -split '\s+'
  $i = 0; $divergente = $false
  foreach ($k in $esperado.Keys) {
    $ok = [int]$valores[$i] -eq $esperado[$k]
    if (-not $ok) { $divergente = $true }
    Write-Host ("  {0,-12} {1,4}  (esperado {2})" -f $k, $valores[$i], $esperado[$k]) -ForegroundColor $(if ($ok) { 'Gray' } else { 'Yellow' })
    $i++
  }
  if ($divergente) {
    Write-Host 'AVISO: contagens diferentes do esperado. Se o schema mudou de propósito, atualize $esperado neste script e o doc 11.' -ForegroundColor Yellow
  }

  if ($Dev) {
    foreach ($arq in Get-ChildItem (Join-Path $pasta 'dev') -Filter '*.sql' | Sort-Object Name) {
      try { [void](Sql '' $arq.FullName @("-D$Banco")) } catch { throw "dev/$($arq.Name): $($_.Exception.Message)" }
      Write-Host ("  dev/{0,-22} ok" -f $arq.Name) -ForegroundColor Green
    }
    Write-Host '  login de desenvolvimento: ADMINISTRADOR / Admin123 (acesso a tudo, sem troca de senha)' -ForegroundColor Cyan
  }

  if ($Teste) {
    try {
      $res = Sql '' (Join-Path $pasta 'teste\roteiro_12_7.sql') @("-D$Banco", '--table')
      Write-Host $res
      Write-Host '  roteiro 12 §7: ok' -ForegroundColor Green
    } catch {
      Write-Host '  roteiro 12 §7: FALHA' -ForegroundColor Red
      throw $_.Exception.Message
    }
  }

  $sucesso = -not $divergente
  if ($sucesso) { Write-Host "Banco '$Banco' recriado com sucesso." -ForegroundColor Green }
}
catch {
  Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
  if ($Teste -and -not $Manter) {
    try { [void](Sql "DROP DATABASE IF EXISTS ``$Banco``;"); Write-Host "Banco temporário '$Banco' removido." }
    catch { Write-Host "Não foi possível remover '$Banco': $($_.Exception.Message)" -ForegroundColor Yellow }
  }
  $env:MYSQL_PWD = $senhaAntes
  Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
if (-not $sucesso) { exit 1 }
exit 0
