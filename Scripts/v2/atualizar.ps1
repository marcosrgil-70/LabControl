<#
.SYNOPSIS
  Atualiza um banco LabControl v2 que já existe com o que falta em Scripts/v2, sem apagar dados.

.DESCRIPTION
  1. Cria um banco de referência temporário pelo recriar.ps1 (os mesmos Scripts/v2/01..06);
  2. compara tabela por tabela (SHOW CREATE TABLE) e, no banco informado:
     - cria as tabelas que faltam;
     - acrescenta as colunas que faltam e ajusta as colunas com definição diferente (MODIFY: para no erro se os
       dados não couberem — nada é truncado);
     - troca índices, UNIQUE, CHECK e chaves estrangeiras diferentes ou que não existem mais nos scripts;
     - só AVISA (não apaga) tabelas e colunas que existem no banco e não nos scripts, e chave primária diferente;
  3. recria todas as views, funções, procedures e triggers pelos scripts 02, 03 e 04;
  4. reaplica o 05_seed (INSERT IGNORE: permissões novas etc.) e roda SP_INICIALIZAR_EMPRESA em cada empresa
     (idempotente: dá ao perfil ADMINISTRADOR as permissões novas e cria o que faltar);
  5. confere as contagens de objetos com o banco de referência e apaga o banco de referência.

  -CopiaAntes <arquivo.sql>: antes de mudar algo, grava uma cópia do banco (mysqldump) nesse arquivo.
  -SoMostrar: só lista o que seria feito (o banco não é alterado).
  Senha: MYSQL_PWD, se definida; senão a do user-secret ConnectionStrings:LabControl (como o recriar.ps1);
  senão é pedida no console.

.EXAMPLE
  .\Scripts\v2\atualizar.ps1 -Banco labcontrol_migracao -SoMostrar
  .\Scripts\v2\atualizar.ps1 -Banco labcontrol_migracao
  .\Scripts\v2\atualizar.ps1 -Banco LabControlDB -CopiaAntes D:\LabControl\backup\antes-da-atualizacao.sql   # instalador
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Banco,
  [string]$Servidor = '127.0.0.1',
  [int]$Porta = 3306,
  [string]$Usuario = 'root',
  [string]$MySql = '',
  [string]$CopiaAntes = '',
  [switch]$SoMostrar
)

$ErrorActionPreference = 'Stop'
$pasta = $PSScriptRoot

function Falhar([string]$msg) { Write-Host "ERRO: $msg" -ForegroundColor Red; exit 1 }

if ($Banco -notmatch '^[A-Za-z0-9_]+$' -or $Banco.Length -gt 50) { Falhar "nome de banco inválido: $Banco" }
if ($Banco -in 'labcontrol', 'mysql', 'sys', 'information_schema', 'performance_schema') { Falhar "o banco '$Banco' não pode ser atualizado por este script." }

# --- cliente mysql -------------------------------------------------------
if (-not $MySql) {
  $cmd = Get-Command mysql.exe -ErrorAction SilentlyContinue
  $MySql = if ($cmd) { $cmd.Source } else { 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe' }
}
if (-not (Test-Path $MySql)) { Falhar "mysql.exe não encontrado ($MySql). Informe -MySql <caminho>." }

# --- senha (a mesma regra do recriar.ps1) -----------------------------------
$senhaAntes = $env:MYSQL_PWD
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
  $env:MYSQL_PWD = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
}

$base = @("-u$Usuario", "-h$Servidor", "-P$Porta", '--default-character-set=utf8mb4')
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('labcontrol_atualizar_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
$utf8 = New-Object Text.UTF8Encoding($false)

# Lê um arquivo de saída do mysql (o handle do redirecionamento pode demorar a ser liberado — pendência 19).
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

function Tabelas([string]$db) {
  @((Sql "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$db' AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;" '' @('-N')) -split "`r?`n" | Where-Object { $_ })
}

# SHOW CREATE TABLE de todas as tabelas numa chamada só → { TABELA = texto }
function Definicoes([string]$db, [string[]]$tabelas) {
  $r = @{}
  if (-not $tabelas.Count) { return $r }
  $texto = Sql (($tabelas | ForEach-Object { "SHOW CREATE TABLE ``$_``;" }) -join "`n") '' @("-D$db", '-N', '-B', '--raw')
  foreach ($m in [regex]::Matches($texto, '(?ms)^(\w+)\t(CREATE TABLE .*?\n\) ENGINE=[^\n]*)')) { $r[$m.Groups[1].Value] = $m.Groups[2].Value }
  return $r
}

# Separa o CREATE TABLE em colunas (na ordem), índices e restrições: { Colunas = [ordered]@{nome=linha}; ... }
function Partes([string]$create) {
  $p = @{ Colunas = [ordered]@{}; Primaria = $null; Indices = @{}; Restricoes = @{}; Estrangeiras = @{} }
  foreach ($l in ($create -split "`n" | Select-Object -Skip 1)) {
    $l = $l.Trim().TrimEnd(',')
    if ($l -match '^`([^`]+)` ') { $p.Colunas[$Matches[1]] = $l }
    elseif ($l -match '^PRIMARY KEY ') { $p.Primaria = $l }
    elseif ($l -match '^(UNIQUE |FULLTEXT |SPATIAL )?KEY `([^`]+)`') { $p.Indices[$Matches[2]] = $l }
    elseif ($l -match '^CONSTRAINT `([^`]+)` FOREIGN KEY') { $p.Estrangeiras[$Matches[1]] = $l }
    elseif ($l -match '^CONSTRAINT `([^`]+)` CHECK') { $p.Restricoes[$Matches[1]] = $l }
  }
  return $p
}

$ref = ($Banco + '_ref_' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$sucesso = $false
try {
  Write-Host "Servidor: ${Servidor}:$Porta  Banco: $Banco$(if ($SoMostrar) { '  (só mostrar)' })" -ForegroundColor Cyan
  $existe = (Sql "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$Banco' AND TABLE_NAME IN ('EMPRESAS', 'PERMISSOES');" '' @('-N')).Trim()
  if ($existe -ne '2') { throw "o banco '$Banco' não existe ou não é um banco do LabControl v2." }

  # 1. referência pelos scripts
  Write-Host "Criando o banco de referência $ref pelos scripts..." -ForegroundColor DarkGray
  $ErrorActionPreference = 'Continue'   # saída do processo filho não interrompe; o código de saída decide
  $saidaRef = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $pasta 'recriar.ps1') -Banco $ref -Forcar `
                -MySql $MySql -Usuario $Usuario -Servidor $Servidor -Porta $Porta 2>&1
  $ErrorActionPreference = 'Stop'
  if ($LASTEXITCODE -ne 0) { throw "recriar.ps1 (referência) falhou:`n$($saidaRef -join "`n")" }

  # 2. tabelas
  $tabRef = Tabelas $ref
  $tabBanco = Tabelas $Banco
  $defRef = Definicoes $ref $tabRef
  $defBanco = Definicoes $Banco @($tabBanco | Where-Object { $_ -in $tabRef })
  $criar = New-Object Collections.Generic.List[string]     # CREATE TABLE
  $antes = New-Object Collections.Generic.List[string]     # tira chaves estrangeiras/índices/CHECK antigos
  $colunas = New-Object Collections.Generic.List[string]   # ADD/MODIFY COLUMN
  $depois = New-Object Collections.Generic.List[string]    # índices, CHECK
  $estrangeiras = New-Object Collections.Generic.List[string]
  $avisos = New-Object Collections.Generic.List[string]

  foreach ($t in $tabRef) {
    if ($t -notin $tabBanco) { $criar.Add($defRef[$t] + ';'); continue }
    $r = Partes $defRef[$t]; $b = Partes $defBanco[$t]
    foreach ($k in $b.Estrangeiras.Keys) { if ($r.Estrangeiras[$k] -ne $b.Estrangeiras[$k]) { $antes.Add("ALTER TABLE ``$t`` DROP FOREIGN KEY ``$k``;") } }
    foreach ($k in $b.Restricoes.Keys) { if ($r.Restricoes[$k] -ne $b.Restricoes[$k]) { $antes.Add("ALTER TABLE ``$t`` DROP CHECK ``$k``;") } }
    foreach ($k in $b.Indices.Keys) {
      # índice criado sozinho pelo MySQL para uma chave estrangeira que continua: não mexer
      if ($r.Indices[$k] -ne $b.Indices[$k] -and -not ($null -eq $r.Indices[$k] -and $r.Estrangeiras.ContainsKey($k))) { $antes.Add("ALTER TABLE ``$t`` DROP INDEX ``$k``;") }
    }
    $anterior = $null
    foreach ($c in $r.Colunas.Keys) {
      $posicao = if ($anterior) { " AFTER ``$anterior``" } else { ' FIRST' }
      if (-not $b.Colunas.Contains($c)) { $colunas.Add("ALTER TABLE ``$t`` ADD COLUMN $($r.Colunas[$c])$posicao;") }
      elseif ($b.Colunas[$c] -ne $r.Colunas[$c]) { $colunas.Add("ALTER TABLE ``$t`` MODIFY COLUMN $($r.Colunas[$c]);") }
      $anterior = $c
    }
    foreach ($c in $b.Colunas.Keys) { if (-not $r.Colunas.Contains($c)) { $avisos.Add("coluna $t.$c existe no banco e não nos scripts (mantida)") } }
    if ($r.Primaria -ne $b.Primaria) { $avisos.Add("chave primária de $t diferente dos scripts ($($b.Primaria) → $($r.Primaria)): ajuste à mão") }
    foreach ($k in $r.Indices.Keys) { if ($r.Indices[$k] -ne $b.Indices[$k]) { $depois.Add("ALTER TABLE ``$t`` ADD $($r.Indices[$k]);") } }
    foreach ($k in $r.Restricoes.Keys) { if ($r.Restricoes[$k] -ne $b.Restricoes[$k]) { $depois.Add("ALTER TABLE ``$t`` ADD $($r.Restricoes[$k]);") } }
    foreach ($k in $r.Estrangeiras.Keys) { if ($r.Estrangeiras[$k] -ne $b.Estrangeiras[$k]) { $estrangeiras.Add("ALTER TABLE ``$t`` ADD $($r.Estrangeiras[$k]);") } }
  }
  foreach ($t in $tabBanco) { if ($t -notin $tabRef) { $avisos.Add("tabela $t existe no banco e não nos scripts (mantida)") } }

  $mudancas = @($criar) + @($antes) + @($colunas) + @($depois) + @($estrangeiras)
  Write-Host ("Tabelas: {0} nova(s), {1} alteração(ões) de estrutura." -f $criar.Count, ($mudancas.Count - $criar.Count)) -ForegroundColor Cyan
  foreach ($m in $mudancas) { Write-Host "  $(($m -split "`n")[0])" }
  foreach ($a in $avisos) { Write-Host "  AVISO: $a" -ForegroundColor Yellow }

  if ($SoMostrar) {
    Write-Host 'Também seriam recriadas as views, funções, procedures e triggers, reaplicado o 05_seed e inicializadas as empresas.' -ForegroundColor Cyan
    $sucesso = $true
  } else {

    if ($CopiaAntes) {
      Write-Host "Cópia do banco antes da atualização: $CopiaAntes" -ForegroundColor DarkGray
      $dump = Join-Path (Split-Path $MySql) 'mysqldump.exe'
      if (-not (Test-Path $dump)) { throw "mysqldump.exe não encontrado ao lado do mysql.exe." }
      New-Item -ItemType Directory -Force -Path (Split-Path $CopiaAntes) | Out-Null
      $p = Start-Process -FilePath $dump -ArgumentList ($base + @('--single-transaction', '--routines', '--triggers', '--events', '--databases', $Banco, "--result-file=`"$CopiaAntes`"")) `
             -RedirectStandardError (Join-Path $tmp 'dump.txt') -NoNewWindow -Wait -PassThru
      if ($p.ExitCode -ne 0) { throw "mysqldump (código $($p.ExitCode)): $(LerSaida (Join-Path $tmp 'dump.txt'))" }
    }

    if ($mudancas.Count) {
      # criação e colunas sem conferir as chaves estrangeiras (tabelas novas podem se referir umas às outras);
      # as chaves estrangeiras novas no fim, conferindo os dados
      $script = "SET FOREIGN_KEY_CHECKS = 0;`n" + ((@($criar) + @($antes) + @($colunas) + @($depois)) -join "`n") + "`nSET FOREIGN_KEY_CHECKS = 1;`n" + ($estrangeiras -join "`n")
      try { [void](Sql $script '' @("-D$Banco")) } catch { throw "estrutura das tabelas: $($_.Exception.Message)" }
      Write-Host '  tabelas atualizadas' -ForegroundColor Green
    }

    # 3. views, funções, procedures e triggers: apagadas e recriadas pelos scripts
    $objetos = Sql @"
  SELECT CONCAT('DROP VIEW IF EXISTS ``', TABLE_NAME, '``;') FROM information_schema.VIEWS WHERE TABLE_SCHEMA = '$Banco'
  UNION ALL SELECT CONCAT('DROP ', ROUTINE_TYPE, ' IF EXISTS ``', ROUTINE_NAME, '``;') FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$Banco'
  UNION ALL SELECT CONCAT('DROP TRIGGER IF EXISTS ``', TRIGGER_NAME, '``;') FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = '$Banco';
"@ '' @('-N')
    if ($objetos.Trim()) { [void](Sql $objetos '' @("-D$Banco")) }
    foreach ($s in '02_funcoes_procedures', '03_triggers', '04_views', '05_seed') {
      $destino = Join-Path $tmp "$s.sql"
      [IO.File]::WriteAllText($destino, ([IO.File]::ReadAllText((Join-Path $pasta "$s.sql"), $utf8) -replace '\blabcontrol_v2\b', $Banco), $utf8)
      try { [void](Sql '' $destino) } catch { throw "$s.sql: $($_.Exception.Message)" }
      Write-Host ("  {0,-26} ok" -f "$s.sql") -ForegroundColor Green
    }

    # 4. empresas: permissões novas no perfil ADMINISTRADOR e o que mais a inicialização criar
    $empresas = @((Sql 'SELECT ID_EMPRESAS FROM EMPRESAS ORDER BY ID_EMPRESAS;' '' @("-D$Banco", '-N')) -split "`r?`n" | Where-Object { $_ })
    if ($empresas.Count) { [void](Sql (($empresas | ForEach-Object { "CALL SP_INICIALIZAR_EMPRESA($_);" }) -join "`n") '' @("-D$Banco")) }
    Write-Host "  $($empresas.Count) empresa(s) inicializada(s)" -ForegroundColor Green

    # 5. conferência com a referência
    $contar = {
      param($db)
      (Sql @"
  SELECT (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$db' AND TABLE_TYPE = 'BASE TABLE'),
         (SELECT COUNT(*) FROM information_schema.VIEWS WHERE TABLE_SCHEMA = '$db'),
         (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$db'),
         (SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = '$db'),
         (SELECT COUNT(*) FROM ``$db``.PERMISSOES);
"@ '' @('-N')).Trim() -split '\s+'
    }
    $cr = & $contar $ref; $cb = & $contar $Banco
    $nomes = 'tabelas', 'views', 'rotinas', 'triggers', 'permissões'
    $dif = @(for ($i = 0; $i -lt 5; $i++) { if ([int]$cb[$i] -lt [int]$cr[$i] -or ($i -gt 0 -and $i -lt 4 -and $cb[$i] -ne $cr[$i])) { "$($nomes[$i]) $($cb[$i]) (scripts: $($cr[$i]))" } })
    if ($dif.Count) { throw "depois da atualização o banco ainda difere dos scripts: $($dif -join ', ')." }
    Write-Host ("Banco '{0}' atualizado: {1} tabelas, {2} views, {3} rotinas, {4} triggers, {5} permissões." -f $Banco, $cb[0], $cb[1], $cb[2], $cb[3], $cb[4]) -ForegroundColor Green
    $sucesso = $true
  }
}
catch {
  Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
  if ($CopiaAntes -and (Test-Path $CopiaAntes)) { Write-Host "A cópia de antes da atualização está em $CopiaAntes." -ForegroundColor Yellow }
}
finally {
  try { [void](Sql "DROP DATABASE IF EXISTS ``$ref``;") } catch { Write-Host "Não foi possível apagar '$ref': $($_.Exception.Message)" -ForegroundColor Yellow }
  $env:MYSQL_PWD = $senhaAntes
  Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
if (-not $sucesso) { exit 1 }
exit 0
