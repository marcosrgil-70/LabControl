# Funções comuns do instalador do LabControl v2 (Instalar.ps1 e Desinstalar.ps1). PowerShell 5.1 (Windows Server).

$utf8SemBom = New-Object Text.UTF8Encoding($false)

function Etapa([string]$texto) {
  $linha = "[{0:HH:mm:ss}] {1}" -f (Get-Date), $texto
  Write-Host ''
  Write-Host $linha -ForegroundColor Cyan
}
function Info([string]$texto) { Write-Host "  $texto" }
function Aviso([string]$texto) { Write-Host "  AVISO: $texto" -ForegroundColor Yellow }
function Ok([string]$texto) { Write-Host "  $texto" -ForegroundColor Green }

<#
  Hash de senha no formato do ASP.NET Core Identity (PasswordHasher, versão 3 — o mesmo do ContaService.GerarHash):
  0x01 | PRF (HMACSHA512 = 2) | iterações (100.000) | tamanho do sal (16) | sal | subchave PBKDF2 (32 bytes), em Base64.
#>
function HashSenha([string]$senha) {
  $sal = New-Object byte[] 16
  $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
  try { $rng.GetBytes($sal) } finally { $rng.Dispose() }
  $iteracoes = 100000
  $kdf = New-Object Security.Cryptography.Rfc2898DeriveBytes($senha, $sal, $iteracoes, [Security.Cryptography.HashAlgorithmName]::SHA512)
  try { $sub = $kdf.GetBytes(32) } finally { $kdf.Dispose() }
  $saida = New-Object byte[] (13 + $sal.Length + $sub.Length)
  $saida[0] = 1
  $be = { param([int]$v, [int]$pos) $saida[$pos] = [byte](($v -shr 24) -band 255); $saida[$pos + 1] = [byte](($v -shr 16) -band 255)
          $saida[$pos + 2] = [byte](($v -shr 8) -band 255); $saida[$pos + 3] = [byte]($v -band 255) }
  & $be 2 1
  & $be $iteracoes 5
  & $be $sal.Length 9
  [Array]::Copy($sal, 0, $saida, 13, $sal.Length)
  [Array]::Copy($sub, 0, $saida, 13 + $sal.Length, $sub.Length)
  return [Convert]::ToBase64String($saida)
}

# Senha aleatória para os usuários internos do MySQL (app e backup): letras e números, sem caracteres que
# atrapalhem a connection string (; = ' ")
function SenhaAleatoria([int]$tamanho = 32) {
  $alfabeto = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789'
  $bytes = New-Object byte[] $tamanho
  $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
  try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
  return -join ($bytes | ForEach-Object { $alfabeto[$_ % $alfabeto.Length] })
}

# Cifra/decifra com o DPAPI da máquina (só esta máquina decifra; o arquivo fica com acesso só de administradores/SYSTEM)
function ProtegerDpapi([string]$texto) {
  Add-Type -AssemblyName System.Security
  $b = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($texto), $null,
         [Security.Cryptography.DataProtectionScope]::LocalMachine)
  return [Convert]::ToBase64String($b)
}

# Pasta só para Administradores e SYSTEM (+ uma identidade opcional com Modificar), sem herança
function RestringirPasta([string]$pasta, [string]$identidadeExtra = '') {
  $acl = New-Object Security.AccessControl.DirectorySecurity
  $acl.SetAccessRuleProtection($true, $false)
  $heranca = [Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit'
  $prop = [Security.AccessControl.PropagationFlags]::None
  foreach ($sid in 'S-1-5-32-544', 'S-1-5-18') {   # Administradores, SYSTEM (por SID: o nome muda com o idioma)
    $conta = (New-Object Security.Principal.SecurityIdentifier($sid)).Translate([Security.Principal.NTAccount])
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($conta, 'FullControl', $heranca, $prop, 'Allow')))
  }
  if ($identidadeExtra) {
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($identidadeExtra, 'Modify', $heranca, $prop, 'Allow')))
  }
  Set-Acl -Path $pasta -AclObject $acl
}

# Executa o mysql.exe com o SQL de um arquivo ou texto; devolve a saída; lança erro com a mensagem do MySQL.
function MySql([string]$exe, [string]$senha, [string]$sql = '', [string]$arquivo = '', [string]$banco = '', [string]$usuario = 'root') {
  $tmp = [IO.Path]::GetTempFileName()
  $err = [IO.Path]::GetTempFileName()
  $entrada = $arquivo
  if (-not $entrada) { $entrada = [IO.Path]::GetTempFileName(); [IO.File]::WriteAllText($entrada, $sql, $utf8SemBom) }
  $antes = $env:MYSQL_PWD
  try {
    $env:MYSQL_PWD = $senha
    $argumentos = @("-u$usuario", '-h127.0.0.1', '-P3306', '--default-character-set=utf8mb4', '-N', '-B')
    if ($banco) { $argumentos += $banco }
    $p = Start-Process -FilePath $exe -ArgumentList $argumentos -RedirectStandardInput $entrada -RedirectStandardOutput $tmp `
           -RedirectStandardError $err -NoNewWindow -Wait -PassThru
    $saida = [IO.File]::ReadAllText($tmp, $utf8SemBom)
    $erro = ((Get-Content $err -Encoding UTF8) | Where-Object { $_ -and $_ -notmatch 'Using a password' }) -join "`n"
    if ($p.ExitCode -ne 0) { throw "mysql: $erro" }
    return $saida
  } finally {
    $env:MYSQL_PWD = $antes
    Remove-Item $tmp, $err -ErrorAction SilentlyContinue
    if (-not $arquivo) { Remove-Item $entrada -ErrorAction SilentlyContinue }
  }
}

# Texto SQL entre aspas simples (só para valores gerados pelo instalador ou digitados no assistente)
function SqlTexto([string]$v) { return "'" + $v.Replace('\', '\\').Replace("'", "''") + "'" }

<#
  Restaura o banco de um backup do Scripts/v2/backup.ps1 (banco.sql + boletins/ + chaves/) com o nome $Banco, confere
  os objetos com as contagens do recriar.ps1 e dá ao ADMINISTRADOR a senha provisória com troca obrigatória.
  Se algo falhar depois de criar o banco, o banco é apagado (o instalador pode ser executado de novo).
#>
function RestaurarBanco([string]$Mysql, [string]$SenhaRoot, [string]$Zip, [string]$Banco, [string]$PastaBoletins,
                        [string]$PastaChaves, [switch]$RestaurarChaves, [string]$SenhaAdmin, [string]$Recriar, [string]$PastaTemp) {
  try {
    Info "Restaurando $Zip..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [IO.Compression.ZipFile]::OpenRead($Zip)
    $dump = Join-Path $PastaTemp 'restauracao.sql'
    try {
      $entrada = $z.GetEntry('banco.sql')
      if (-not $entrada) { throw 'o arquivo não é um backup do LabControl (falta banco.sql).' }
      $leitor = New-Object IO.StreamReader($entrada.Open(), $utf8SemBom)
      try { $texto = $leitor.ReadToEnd() } finally { $leitor.Dispose() }
      if ($texto -notmatch '(?m)^CREATE DATABASE /\*!32312 IF NOT EXISTS\*/ `([A-Za-z0-9_]+)`') { throw 'banco.sql sem o CREATE DATABASE do mysqldump.' }
      $origem = $Matches[1]
      Info "Banco de origem no backup: $origem → $Banco"
      $texto = $texto -replace "(?m)^(CREATE DATABASE /\*!32312 IF NOT EXISTS\*/ )``$origem``", "`$1``$Banco``" `
                      -replace "(?m)^USE ``$origem``;", "USE ``$Banco``;"
      [IO.File]::WriteAllText($dump, $texto, $utf8SemBom)
      Remove-Variable texto
      MySql $Mysql $SenhaRoot -arquivo $dump | Out-Null

      # boletins emitidos (o banco guarda só o caminho e o SHA-256); arquivos já existentes não são trocados
      $n = 0
      foreach ($e in $z.Entries | Where-Object { $_.Name -and $_.FullName -like 'boletins/*' }) {
        $destino = Join-Path $PastaBoletins ($e.FullName.Substring(9) -replace '/', '\')
        if (Test-Path $destino) { continue }
        New-Item -ItemType Directory -Force -Path (Split-Path $destino) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $destino); $n++
      }
      Info "$n boletim(ns) restaurado(s)."
      # chaves: só na recuperação deste mesmo servidor (um backup de outra máquina traria chaves de fora)
      if ($RestaurarChaves -and -not (Get-ChildItem $PastaChaves -File -ErrorAction SilentlyContinue)) {
        $n = 0
        foreach ($e in $z.Entries | Where-Object { $_.Name -and $_.FullName -like 'chaves/*' }) {
          [IO.Compression.ZipFileExtensions]::ExtractToFile($e, (Join-Path $PastaChaves $e.Name)); $n++
        }
        Info "$n chave(s) do Data Protection restaurada(s)."
      }
    } finally { $z.Dispose(); Remove-Item $dump -ErrorAction SilentlyContinue }

    # confere os objetos com as contagens do recriar.ps1 (a mesma fonte do schema)
    $esperado = @{ TABELAS = 59; VIEWS = 10; FUNCOES = 8; PROCEDURES = 27; TRIGGERS = 48 }
    if ((Test-Path $Recriar) -and ((Get-Content $Recriar -Raw) -match '\$esperado = \[ordered\]@\{([^}]+)\}')) {
      foreach ($par in $Matches[1] -split ';') { $k, $v = $par -split '='; if ($k.Trim() -in $esperado.Keys) { $esperado[$k.Trim()] = [int]$v.Trim() } }
    }
    $contagem = (MySql $Mysql $SenhaRoot @"
    SELECT (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = '$Banco' AND TABLE_TYPE = 'BASE TABLE'),
       (SELECT COUNT(*) FROM information_schema.VIEWS WHERE TABLE_SCHEMA = '$Banco'),
       (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$Banco' AND ROUTINE_TYPE = 'FUNCTION'),
       (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = '$Banco' AND ROUTINE_TYPE = 'PROCEDURE'),
       (SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = '$Banco');
"@).Trim() -split "`t"
    $achado = @{ TABELAS = [int]$contagem[0]; VIEWS = [int]$contagem[1]; FUNCOES = [int]$contagem[2]; PROCEDURES = [int]$contagem[3]; TRIGGERS = [int]$contagem[4] }
    $dif = @($esperado.Keys | Where-Object { $esperado[$_] -ne $achado[$_] } | ForEach-Object { "$_ $($achado[$_]) (esperado $($esperado[$_]))" })
    if ($dif.Count) { throw "banco restaurado diferente do schema desta versão: $($dif -join ', ')." }
    Ok ("Banco restaurado: {0} tabelas, {1} views, {2} funções, {3} procedures, {4} triggers." -f $contagem)

    # nunca o ADMINISTRADOR/Admin123 em produção (CLAUDE.md): senha provisória do assistente, troca no 1º acesso
    $linhas = MySql $Mysql $SenhaRoot "UPDATE USUARIOS SET SENHA_HASH = $(SqlTexto (HashSenha $SenhaAdmin)), TROCAR_SENHA = 1, TENTATIVAS_FALHAS = 0, BLOQUEADO_ATE = NULL WHERE LOGIN = 'ADMINISTRADOR'; SELECT ROW_COUNT();" -banco $Banco
    if ($linhas.Trim() -ne '1') { Aviso 'o banco restaurado não tem o usuário ADMINISTRADOR.' } else { Ok 'ADMINISTRADOR com a senha provisória do assistente (troca no 1º acesso).' }
    $semTroca = (MySql $Mysql $SenhaRoot "SELECT GROUP_CONCAT(LOGIN) FROM USUARIOS WHERE TROCAR_SENHA = 0 AND ATIVO = 1;" -banco $Banco).Trim()
    if ($semTroca -and $semTroca -ne 'NULL') { Info "Usuários que já trocaram a senha (mantida): $semTroca" }
  } catch {
    try { MySql $Mysql $SenhaRoot "DROP DATABASE IF EXISTS ``$Banco``;" | Out-Null; Aviso "banco $Banco apagado (restauração incompleta)." } catch { }
    throw
  }
}
