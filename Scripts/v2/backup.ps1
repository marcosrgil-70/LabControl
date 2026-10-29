<#
.SYNOPSIS
  Backup do LabControl v2: banco MySQL + pasta dos boletins + chaves do Data Protection, num único .zip.

.DESCRIPTION
  1. mysqldump do banco (--single-transaction --routines --triggers --events --hex-blob), sem travar o uso;
  2. confere que o dump terminou ("-- Dump completed");
  3. junta no zip: o dump (banco.sql), a pasta Boletins:Pasta (boletins/) e a pasta DataProtection:PastaChaves
     (chaves/) — o banco guarda só o caminho e o SHA-256 dos PDFs, e sem as chaves a senha do e-mail da T05
     precisa ser digitada de novo (pendência 27);
  4. grava no zip um manifesto (manifesto.txt) com data, banco, SHA-256 do dump e quantidade de arquivos;
  5. com -Conferir: restaura o dump num banco temporário labcontrol_conf_<id>, compara a quantidade de linhas de
     cada tabela com o banco de origem e apaga o temporário;
  6. apaga do destino os backups com mais de -ManterDias dias (só os arquivos <banco>_*.zip deste script).
  Termina com código 1 em qualquer erro (para o Agendador de Tarefas avisar).

  Senha, nesta ordem: -LoginPath (credencial guardada pelo mysql_config_editor no perfil do usuário que roda o
  script — o jeito recomendado para a tarefa agendada); MYSQL_PWD; a do user-secret ConnectionStrings:LabControl
  (desenvolvimento); pergunta no console. Nunca fica em arquivo versionado nem no zip.

  O zip tem a senha do e-mail cifrada E as chaves que a decifram: guarde-o num lugar com acesso restrito.

.EXAMPLE
  .\Scripts\v2\backup.ps1 -Destino D:\Backup\LabControl -PastaApp C:\inetpub\LabControl -LoginPath labcontrol_backup
  .\Scripts\v2\backup.ps1 -Destino C:\Project\backup -Banco labcontrol_migracao -Conferir
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Destino,
  [string]$Banco = 'labcontrol_v2',
  # pasta do app publicado: Boletins:Pasta e DataProtection:PastaChaves relativas são resolvidas a partir dela
  # (padrão: a raiz do repositório; resolvida abaixo, porque no -File do PowerShell 5.1 o $PSScriptRoot vem vazio aqui)
  [string]$PastaApp = '',
  [string]$PastaBoletins = '',
  [string]$PastaChaves = '',
  [string]$Servidor = '127.0.0.1',
  [int]$Porta = 3306,
  [string]$Usuario = 'root',
  [string]$LoginPath = '',
  [string]$MySqlBin = '',
  [int]$ManterDias = 30,
  [switch]$Conferir
)

$ErrorActionPreference = 'Stop'
$senhaAntes = $env:MYSQL_PWD
function Falhar([string]$msg) { Write-Host "ERRO: $msg" -ForegroundColor Red; exit 1 }
if (-not $PastaApp) { $PastaApp = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }

if ($Banco -notmatch '^[A-Za-z0-9_]+$') { Falhar "nome de banco inválido: $Banco" }

# --- clientes mysql ------------------------------------------------------
if (-not $MySqlBin) {
  $cmd = Get-Command mysqldump.exe -ErrorAction SilentlyContinue
  $MySqlBin = if ($cmd) { Split-Path $cmd.Source } else { 'C:\Program Files\MySQL\MySQL Server 8.0\bin' }
}
$dumpExe = Join-Path $MySqlBin 'mysqldump.exe'
$mysqlExe = Join-Path $MySqlBin 'mysql.exe'
foreach ($e in $dumpExe, $mysqlExe) { if (-not (Test-Path $e)) { Falhar "$e não encontrado. Informe -MySqlBin <pasta>." } }

# --- pastas do app (as mesmas regras do Program.cs: relativa ao ContentRoot ou absoluta) ---------------------
function Configuracao([string]$secao, [string]$chave, [string]$padrao) {
  $valor = $null
  foreach ($arq in 'appsettings.json', 'appsettings.Production.json') {
    $p = Join-Path $PastaApp $arq
    if (Test-Path $p) {
      $json = Get-Content $p -Raw -Encoding UTF8 | ConvertFrom-Json
      if ($json.$secao -and $json.$secao.$chave) { $valor = $json.$secao.$chave }
    }
  }
  if (-not $valor) { $valor = $padrao }
  if ([IO.Path]::IsPathRooted($valor)) { return $valor }
  return [IO.Path]::GetFullPath((Join-Path $PastaApp $valor))
}
if (-not $PastaBoletins) { $PastaBoletins = Configuracao 'Boletins' 'Pasta' 'storage/boletins' }
if (-not $PastaChaves) { $PastaChaves = Configuracao 'DataProtection' 'PastaChaves' 'storage/chaves' }

# --- senha ---------------------------------------------------------------
if ($LoginPath) {
  $base = @("--login-path=$LoginPath")   # tem de ser a 1ª opção do mysql/mysqldump
} else {
  if (-not $env:MYSQL_PWD) {
    $csproj = Join-Path $PSScriptRoot '..\..\LabControl.csproj'
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
  $base = @("-u$Usuario")
}
$base += @("-h$Servidor", "-P$Porta", '--default-character-set=utf8mb4')

$tmp = Join-Path ([IO.Path]::GetTempPath()) ('labcontrol_backup_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
$utf8 = New-Object Text.UTF8Encoding($false)

# Executa um cliente com entrada/saída em arquivo; para no erro (o aviso de senha na linha de comando não conta).
function Rodar([string]$exe, [string[]]$argumentos, [string]$entrada, [string]$saida) {
  $erro = Join-Path $tmp 'erro.txt'
  $opcoes = @{ FilePath = $exe; ArgumentList = $argumentos; RedirectStandardOutput = $saida; RedirectStandardError = $erro
               NoNewWindow = $true; Wait = $true; PassThru = $true }
  if ($entrada) { $opcoes.RedirectStandardInput = $entrada }
  $p = Start-Process @opcoes
  $err = ((Get-Content $erro -Encoding UTF8) | Where-Object { $_ -and $_ -notmatch 'Using a password' }) -join "`n"
  if ($p.ExitCode -ne 0) { throw "$(Split-Path $exe -Leaf) (código $($p.ExitCode)): $err" }
}
function Sql([string]$banco, [string]$texto) {
  $cmd = Join-Path $tmp 'cmd.sql'
  [IO.File]::WriteAllText($cmd, $texto, $utf8)
  $saida = Join-Path $tmp 'saida.txt'
  Rodar $mysqlExe ($base + @('-N', '-B', $banco)) $cmd $saida
  return [IO.File]::ReadAllText($saida, $utf8)
}
# linhas de cada tabela (COUNT exato, não a estimativa do information_schema)
function Contagens([string]$banco) {
  $tabelas = (Sql $banco "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$banco' AND TABLE_TYPE = 'BASE TABLE' ORDER BY 1;") -split "`r?`n" | Where-Object { $_ }
  $sql = ($tabelas | ForEach-Object { "SELECT '$_', COUNT(*) FROM ``$_``" }) -join "`nUNION ALL "
  $r = [ordered]@{}
  foreach ($l in (Sql $banco "$sql;") -split "`r?`n" | Where-Object { $_ }) { $c = $l -split "`t"; $r[$c[0]] = [long]$c[1] }
  return $r
}

$sucesso = $false
try {
  if (-not (Test-Path $Destino)) { New-Item -ItemType Directory -Path $Destino | Out-Null }
  $carimbo = Get-Date -Format 'yyyyMMdd-HHmm'
  $zip = Join-Path $Destino "${Banco}_$carimbo.zip"
  Write-Host "Banco: $Banco em ${Servidor}:$Porta" -ForegroundColor Cyan
  Write-Host "Boletins: $PastaBoletins" -ForegroundColor Cyan
  Write-Host "Chaves:   $PastaChaves" -ForegroundColor Cyan

  # 1-2. dump
  $dump = Join-Path $tmp 'banco.sql'
  $t = [Diagnostics.Stopwatch]::StartNew()
  Rodar $dumpExe ($base + @('--single-transaction', '--routines', '--triggers', '--events', '--hex-blob',
                            '--set-gtid-purged=OFF', '--no-tablespaces', '--databases', $Banco)) '' $dump
  $fim = Get-Content $dump -Tail 1 -Encoding UTF8
  if ($fim -notmatch '^-- Dump completed') { throw "o dump não terminou (última linha: '$fim')." }
  $hash = (Get-FileHash $dump -Algorithm SHA256).Hash.ToLowerInvariant()
  Write-Host ("  dump ok: {0:n1} MB em {1:n0}s" -f ((Get-Item $dump).Length / 1MB), $t.Elapsed.TotalSeconds) -ForegroundColor Green

  # 5. conferência opcional: restaura num banco temporário e compara as linhas de cada tabela
  if ($Conferir) {
    $conf = 'labcontrol_conf_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    $copia = Join-Path $tmp 'conf.sql'
    # o dump tem CREATE DATABASE/USE do banco de origem: troca só essas duas linhas
    $texto = [IO.File]::ReadAllText($dump, $utf8)
    $texto = $texto -replace "(?m)^(CREATE DATABASE /\*!32312 IF NOT EXISTS\*/ )``$Banco``", "`$1``$conf``" `
                    -replace "(?m)^USE ``$Banco``;", "USE ``$conf``;"
    [IO.File]::WriteAllText($copia, $texto, $utf8)
    Remove-Variable texto
    try {
      $t.Restart()
      Rodar $mysqlExe $base $copia (Join-Path $tmp 'saida.txt')
      $origem = Contagens $Banco
      $restaurado = Contagens $conf
      $dif = @($origem.Keys | Where-Object { $origem[$_] -ne $restaurado[$_] })
      if ($dif.Count) { throw "conferência: linhas diferentes em $($dif -join ', ') (o banco mudou durante o backup?)." }
      Write-Host ("  conferência ok: {0} tabelas, {1:n0} linhas restauradas em {2:n0}s" -f $origem.Count,
                  ($origem.Values | Measure-Object -Sum).Sum, $t.Elapsed.TotalSeconds) -ForegroundColor Green
    } finally {
      [void](Sql 'mysql' "DROP DATABASE IF EXISTS ``$conf``;")
      Remove-Item $copia -ErrorAction SilentlyContinue
    }
  }

  # 3-4. zip (sem cópia intermediária das pastas)
  Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
  $parcial = "$zip.parcial"
  $arquivo = [IO.Compression.ZipFile]::Open($parcial, [IO.Compression.ZipArchiveMode]::Create)
  $qtd = @{}
  try {
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivo, $dump, 'banco.sql', [IO.Compression.CompressionLevel]::Optimal)
    foreach ($par in @(@('boletins', $PastaBoletins), @('chaves', $PastaChaves))) {
      $nome, $pasta = $par
      $qtd[$nome] = 0
      if (-not (Test-Path $pasta)) { Write-Host "  aviso: pasta $pasta não existe (nada de $nome no backup)." -ForegroundColor Yellow; continue }
      $raiz = (Resolve-Path $pasta).Path.TrimEnd('\') + '\'
      foreach ($f in Get-ChildItem $pasta -Recurse -File) {
        $rel = $f.FullName.Substring($raiz.Length) -replace '\\', '/'
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivo, $f.FullName, "$nome/$rel", [IO.Compression.CompressionLevel]::Optimal)
        $qtd[$nome]++
      }
    }
    $manifesto = @(
      "LabControl v2 — backup de $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss') em $env:COMPUTERNAME",
      "banco: $Banco (${Servidor}:$Porta)",
      "banco.sql SHA-256: $hash",
      "boletins: $($qtd.boletins) arquivo(s) de $PastaBoletins",
      "chaves: $($qtd.chaves) arquivo(s) de $PastaChaves",
      "conferido por restauração: $(if ($Conferir) { 'sim' } else { 'não' })"
    ) -join "`r`n"
    $entrada = $arquivo.CreateEntry('manifesto.txt')
    $w = New-Object IO.StreamWriter($entrada.Open(), $utf8)
    try { $w.Write($manifesto) } finally { $w.Dispose() }
  } finally { $arquivo.Dispose() }
  Move-Item $parcial $zip
  if ($qtd.chaves -eq 0) { Write-Host '  aviso: nenhuma chave do Data Protection no backup (pendência 27).' -ForegroundColor Yellow }
  Write-Host ("  zip ok: {0} ({1:n1} MB; {2} boletim(ns), {3} chave(s))" -f $zip, ((Get-Item $zip).Length / 1MB), $qtd.boletins, $qtd.chaves) -ForegroundColor Green

  # 6. retenção
  if ($ManterDias -gt 0) {
    $limite = (Get-Date).AddDays(-$ManterDias)
    foreach ($velho in Get-ChildItem $Destino -Filter "${Banco}_*.zip" | Where-Object { $_.LastWriteTime -lt $limite }) {
      Remove-Item $velho.FullName
      Write-Host "  apagado (mais de $ManterDias dias): $($velho.Name)" -ForegroundColor DarkGray
    }
  }
  $sucesso = $true
} catch {
  Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
} finally {
  Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
  if (Test-Path "$zip.parcial") { Remove-Item "$zip.parcial" -Force }
  $env:MYSQL_PWD = $senhaAntes
}
if (-not $sucesso) { exit 1 }
Write-Host 'Backup concluído.' -ForegroundColor Green
exit 0
