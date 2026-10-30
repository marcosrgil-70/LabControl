<#
.SYNOPSIS
  Instala ou atualiza o LabControl v2 num servidor Windows (chamado pelo setup.exe; também roda sozinho).

.DESCRIPTION
  Pode ser repetido: o que já está pronto é conferido e mantido.
   1. IIS (com o console e os cmdlets de administração);
   2. Visual C++ Redistributable (MySQL) e .NET 10 Hosting Bundle (ASP.NET Core no IIS), se faltarem — os instaladores
      vêm de -PastaPrerequisitos (baixados pelo setup) e precisam da assinatura digital da Microsoft;
   3. MySQL 8.0 como serviço "MySQL80", ouvindo só em 127.0.0.1 (se ainda não houver um MySQL);
   4. banco (padrão LabControlDB): se existir, é mantido (atualização). Se não existir: com -ArquivoBackup, restaura o
      backup (usuários e senhas do backup); sem backup, cria zerado pelos scripts com a empresa LC_EMPRESA e o
      ADMINISTRADOR / Admin123 sem troca (decisão do usuário, 29/10/2026) — o legado vem depois pela T07, no mesmo banco;
   5. usuários do MySQL "labcontrol" (app) e "labcontrol_backup" com permissões mínimas e senhas aleatórias;
   6. pastas (boletins, chaves, backup) com acesso restrito;
   7. arquivos do sistema, pool e site do IIS em HTTPS com certificado autoassinado (exportado para as estações);
   8. regra do firewall, tarefa diária de backup (22h) e teste de abertura da tela de login.

  LC_SENHA_ROOT (root do MySQL: a nova, se o MySQL for instalado agora; a atual, se já existir) e LC_EMPRESA (nome da
  empresa, banco novo) vêm por variável de ambiente — nunca pela linha de comando nem pelo log. As senhas internas
  (app e backup) são geradas aqui e não aparecem em lugar nenhum.

  Código de saída: 0 ok; 3010 ok, mas o Windows precisa reiniciar; 1 erro (ver o log em <dados>\logs).
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$PastaApp,
  [Parameter(Mandatory)] [string]$PastaDados,
  [string]$PastaBoletins = '',
  [string]$PastaPrerequisitos = '',
  [string]$Banco = 'LabControlDB',
  [string]$NomeServidor = $env:COMPUTERNAME,
  [int]$PortaHttps = 443,
  [string]$ArquivoBackup = '',
  [switch]$RestaurarChaves,
  [string]$HoraBackup = '22:00'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $aqui 'Funcoes.ps1')

$Site = 'LabControl'
$Pool = 'LabControl'
$IdentidadePool = "IIS AppPool\$Pool"
$ServicoMySql = 'MySQL80'
$pacote = Join-Path $PastaApp 'pacote'
$prereq = if ($PastaPrerequisitos) { $PastaPrerequisitos } else { Join-Path $pacote 'prerequisitos' }
$pastaSite = Join-Path $PastaApp 'site'
if (-not $PastaBoletins) { $PastaBoletins = Join-Path $PastaDados 'boletins' }
$pastas = @{
  Boletins = $PastaBoletins
  Chaves   = Join-Path $PastaDados 'chaves'
  Backup   = Join-Path $PastaDados 'backup'
  Config   = Join-Path $PastaDados 'config'
  Logs     = Join-Path $PastaDados 'logs'
  Cert     = Join-Path $PastaDados 'certificado'
}
$boletinsJaExistia = Test-Path $PastaBoletins
$reiniciar = $false
$bancoNovo = $false

New-Item -ItemType Directory -Force -Path $PastaDados, $pastas.Logs | Out-Null
$log = Join-Path $pastas.Logs ("instalacao-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))
Start-Transcript -Path $log | Out-Null

try {
  Etapa "LabControl v2 — instalação em $env:COMPUTERNAME"
  Info "Sistema: $PastaApp   Boletins: $PastaBoletins   Dados: $PastaDados   Endereço: https://${NomeServidor}:$PortaHttps/"

  # --- 0. conferências -------------------------------------------------------------------------------------
  $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
  if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'execute como administrador.' }
  if ([Environment]::OSVersion.Version.Build -lt 14393) { throw 'requer Windows Server 2016 / Windows 10 ou mais novo.' }
  if (-not [Environment]::Is64BitOperatingSystem) { throw 'requer Windows de 64 bits.' }
  $senhaRoot = $env:LC_SENHA_ROOT
  if (-not $senhaRoot) { throw 'senha do root do MySQL não informada (LC_SENHA_ROOT).' }
  if ($ArquivoBackup -and -not (Test-Path $ArquivoBackup)) { throw "backup não encontrado: $ArquivoBackup" }
  if ($NomeServidor -notmatch '^[A-Za-z0-9.-]+$') { throw "nome do servidor inválido: $NomeServidor" }
  if ($Banco -notmatch '^[A-Za-z][A-Za-z0-9_]{1,63}$' -or $Banco -eq 'labcontrol') { throw "nome de banco inválido: $Banco" }
  $bancoSql = $Banco.ToLowerInvariant()   # o MySQL no Windows guarda os nomes em minúsculas (lower_case_table_names = 1)
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

  # --- 1. IIS ----------------------------------------------------------------------------------------------
  Etapa '1/9 IIS'
  # nomes do DISM (valem no Windows Server e no Windows 10/11)
  $recursosIis = 'IIS-WebServerRole', 'IIS-WebServer', 'IIS-DefaultDocument', 'IIS-StaticContent', 'IIS-HttpErrors',
                 'IIS-HttpLogging', 'IIS-RequestMonitor', 'IIS-RequestFiltering', 'IIS-HttpCompressionStatic',
                 'IIS-ManagementConsole', 'IIS-ManagementScriptingTools'
  # IIS já ativo (inclusive à mão, em "Recursos do Windows") com o módulo de scripts que o setup usa: não mexe
  $iisPronto = { [int](Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\InetStp' -ErrorAction SilentlyContinue).MajorVersion -ge 10 -and
                 (Get-Module -ListAvailable WebAdministration) }
  if (& $iisPronto) {
    Info 'IIS já ativado neste Windows.'
  } else { try {
    if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
      $r = Install-WindowsFeature Web-Server, Web-Default-Doc, Web-Static-Content, Web-Http-Errors, Web-Http-Logging,
                                  Web-Request-Monitor, Web-Filtering, Web-Stat-Compression, Web-Mgmt-Console, Web-Scripting-Tools -ErrorAction Stop
      if ($r.RestartNeeded -eq 'Yes') { $reiniciar = $true }
    } else {
      # Windows 10/11
      $r = Enable-WindowsOptionalFeature -Online -All -NoRestart -FeatureName $recursosIis -ErrorAction Stop
      if ($r.RestartNeeded) { $reiniciar = $true }
    }
  } catch {
    # o cmdlet às vezes falha ("Não foi possível ler a chave do Registro de configuração") e o dism.exe funciona
    Aviso "Ativação do IIS pelo PowerShell falhou ($($_.Exception.Message.Trim())). Tentando pelo dism.exe..."
    $argumentos = @('/Online', '/Enable-Feature', '/All', '/NoRestart', '/Quiet') + ($recursosIis | ForEach-Object { "/FeatureName:$_" })
    $saidaDism = & "$env:SystemRoot\System32\dism.exe" @argumentos 2>&1 | Out-String
    switch ($LASTEXITCODE) {
      0 { }
      3010 { $reiniciar = $true }
      default {
        $pendente = (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending') -or
                    (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired')
        $dica = if ($pendente) { 'O Windows tem um REINÍCIO PENDENTE (atualização): reinicie o computador e execute o setup de novo.' }
                else { 'Reinicie o computador, espere o Windows Update terminar e execute o setup de novo.' }
        throw ("o Windows não conseguiu ativar o IIS (dism.exe código $LASTEXITCODE). $dica`n" +
               'Se persistir, ative à mão em "Ativar ou desativar recursos do Windows": "Serviços de Informações da Internet" e, dentro de ' +
               '"Ferramentas de Gerenciamento da Web", também "Scripts e Ferramentas de Gerenciamento do IIS"; depois execute o setup de novo.' +
               "`n$($saidaDism.Trim())`nDetalhes: $env:SystemRoot\Logs\DISM\dism.log")
      }
    }
  } }
  if (-not (& $iisPronto)) {
    throw 'o IIS não ficou pronto (falta o IIS ou "Scripts e Ferramentas de Gerenciamento do IIS"). Reinicie o computador e execute o setup de novo.'
  }
  Ok 'IIS pronto.'

  # --- 2. VC++ e .NET Hosting Bundle -----------------------------------------------------------------------
  Etapa '2/9 Visual C++ e .NET 10 Hosting Bundle'
  # instalador baixado: só com a assinatura digital válida da Microsoft (o VC++ vem de um link sem hash fixo)
  function Instalador([string]$filtro, [string]$nome, [string]$modo = '/install') {
    $exe = Get-ChildItem $prereq -Filter $filtro -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exe) { throw "$nome não está instalado e o instalador não foi encontrado em $prereq (execute o setup de novo, com internet ou com a pasta prerequisitos)." }
    $assinatura = Get-AuthenticodeSignature $exe.FullName
    if ($assinatura.Status -ne 'Valid' -or $assinatura.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
      throw "$($exe.Name): assinatura digital inválida ($($assinatura.Status)). Arquivo recusado."
    }
    $p = Start-Process $exe.FullName -ArgumentList $modo, '/quiet', '/norestart' -Wait -PassThru
    switch ($p.ExitCode) {
      0 { Ok "$nome instalado." }
      1638 { Ok "${nome}: já havia uma versão igual ou mais nova." }
      3010 { Ok "$nome instalado (o Windows pede reinício)."; $script:reiniciar = $true }
      default { throw "$nome terminou com o código $($p.ExitCode)." }
    }
  }
  $vc = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64' -ErrorAction SilentlyContinue
  if ($vc -and $vc.Installed -eq 1) { Ok "Visual C++ $($vc.Version) já instalado." }
  else { Instalador 'vc_redist.x64.exe' 'Visual C++ 2015-2022 (x64)' }
  $ancm = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
  $runtime = Get-ChildItem (Join-Path $env:ProgramFiles 'dotnet\shared\Microsoft.AspNetCore.App') -Directory -Filter '10.*' -ErrorAction SilentlyContinue
  $hosting = Get-ChildItem $prereq -Filter 'dotnet-hosting-*.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
  $versaoPacote = if ($hosting) { [version]($hosting.Name -replace '^dotnet-hosting-([\d.]+)-win\.exe$', '$1') } else { [version]'10.0.0' }
  if ((Test-Path $ancm) -and ($runtime | Where-Object { [version]$_.Name -ge $versaoPacote })) {
    Ok ".NET $(($runtime | Sort-Object { [version]$_.Name } | Select-Object -Last 1).Name) e o módulo do IIS já instalados."
  } else {
    # .NET instalado antes do IIS fica sem o módulo do IIS: nesse caso, reparar
    $modo = if ($runtime | Where-Object { [version]$_.Name -eq $versaoPacote }) { '/repair' } else { '/install' }
    Instalador 'dotnet-hosting-*.exe' ".NET Hosting Bundle $versaoPacote" $modo
    # o módulo do ASP.NET Core só entra no IIS depois de reiniciar os serviços web
    & net.exe stop was /y | Out-Null
    & net.exe start w3svc | Out-Null
  }

  # --- 3. MySQL --------------------------------------------------------------------------------------------
  Etapa '3/9 MySQL 8.0'
  $servico = Get-CimInstance Win32_Service | Where-Object { $_.Name -like 'MySQL*' -and $_.PathName -match 'mysqld' } | Select-Object -First 1
  if ($servico) {
    $mysqlBin = Split-Path (($servico.PathName -replace '^"([^"]+)".*$', '$1') -replace '^(\S+mysqld(\.exe)?).*$', '$1')
    Info "MySQL já instalado: serviço $($servico.Name) em $mysqlBin."
    if ($servico.State -ne 'Running') { Start-Service $servico.Name }
  } else {
    $base = Join-Path $env:ProgramFiles 'MySQL\MySQL Server 8.0'
    $dadosMySql = Join-Path $env:ProgramData 'MySQL\MySQL Server 8.0'
    $zip = Get-ChildItem $prereq -Filter 'mysql-8.0.*-winx64.zip' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $zip) { throw "MySQL não está instalado e o arquivo do MySQL não foi encontrado em $prereq (execute o setup de novo, com internet ou com a pasta prerequisitos)." }
    if (Test-Path (Join-Path $dadosMySql 'Data')) { throw "já existe uma pasta de dados do MySQL em $dadosMySql\Data sem o serviço: confira antes de instalar." }
    Info "Extraindo $($zip.Name) para $base (sem os símbolos de depuração)..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
    try {
      foreach ($e in $z.Entries) {
        if (-not $e.Name -or $e.FullName -match '\.pdb$|/lib/plugin/debug/|/bin/debug/') { continue }
        $rel = $e.FullName.Substring($e.FullName.IndexOf('/') + 1)
        $destino = Join-Path $base $rel
        New-Item -ItemType Directory -Force -Path (Split-Path $destino) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $destino, $true)
      }
    } finally { $z.Dispose() }
    $mysqlBin = Join-Path $base 'bin'
    New-Item -ItemType Directory -Force -Path $dadosMySql | Out-Null
    $ini = Join-Path $dadosMySql 'my.ini'
    # só local (o app está no mesmo servidor); utf8mb4 como no desenvolvimento; sem log binário (sem réplica)
    $conteudoIni = @"
[mysqld]
basedir="$($base -replace '\\', '/')"
datadir="$((Join-Path $dadosMySql 'Data') -replace '\\', '/')"
port=3306
bind-address=127.0.0.1
character-set-server=utf8mb4
collation-server=utf8mb4_0900_ai_ci
skip-log-bin
max_allowed_packet=64M
innodb_buffer_pool_size=512M

[client]
port=3306
default-character-set=utf8mb4
"@
    [IO.File]::WriteAllText($ini, $conteudoIni, $utf8SemBom)
    Info 'Inicializando a pasta de dados...'
    $p = Start-Process (Join-Path $mysqlBin 'mysqld.exe') -ArgumentList "--defaults-file=`"$ini`"", '--initialize-insecure', '--console' -Wait -PassThru -NoNewWindow
    if ($p.ExitCode -ne 0) { throw "mysqld --initialize terminou com o código $($p.ExitCode)." }
    $p = Start-Process (Join-Path $mysqlBin 'mysqld.exe') -ArgumentList '--install', $ServicoMySql, "--defaults-file=`"$ini`"" -Wait -PassThru -NoNewWindow
    if ($p.ExitCode -ne 0) { throw "mysqld --install terminou com o código $($p.ExitCode)." }
    & sc.exe failure $ServicoMySql reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Start-Service $ServicoMySql
    # root nasce sem senha (só local, por segundos): define a senha digitada no assistente
    for ($i = 0; ; $i++) {
      try { MySql (Join-Path $mysqlBin 'mysql.exe') '' "ALTER USER 'root'@'localhost' IDENTIFIED BY $(SqlTexto $senhaRoot);" | Out-Null; break }
      catch { if ($i -ge 30) { throw }; Start-Sleep 2 }
    }
    Ok "MySQL instalado: serviço $ServicoMySql, só em 127.0.0.1:3306."
  }
  $mysql = Join-Path $mysqlBin 'mysql.exe'
  for ($i = 0; ; $i++) {
    try { $versao = (MySql $mysql $senhaRoot 'SELECT VERSION();').Trim(); break }
    catch { if ($i -ge 30 -or $_.Exception.Message -match 'Access denied') { throw "não foi possível entrar no MySQL como root: $($_.Exception.Message)" }; Start-Sleep 2 }
  }
  Ok "Conectado ao MySQL $versao."

  # --- 4. banco --------------------------------------------------------------------------------------------
  Etapa "4/9 Banco $Banco"
  $existe = (MySql $mysql $senhaRoot "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = $(SqlTexto $bancoSql);").Trim() -eq '1'
  New-Item -ItemType Directory -Force -Path @($pastas.Values) | Out-Null   # @(): sem ele, a coleção vira um texto só
  if ($existe) {
    if ($ArquivoBackup) { throw "o banco $Banco já existe e o instalador nunca o sobrescreve. Para restaurar um backup, apague o banco antes (depois de um backup!) ou escolha 'banco novo' (que mantém o existente)." }
    Ok "Banco $Banco existente mantido (atualização do sistema)."
  } elseif ($ArquivoBackup) {
    RestaurarBanco -Mysql $mysql -SenhaRoot $senhaRoot -Zip $ArquivoBackup -Banco $bancoSql -PastaBoletins $pastas.Boletins `
                   -PastaChaves $pastas.Chaves -RestaurarChaves:$RestaurarChaves -Recriar (Join-Path $aqui 'recriar.ps1') -PastaTemp $PastaDados
  } else {
    $empresa = "$env:LC_EMPRESA".Trim()
    if ($empresa.Length -lt 3) { throw 'nome da empresa não informado (LC_EMPRESA).' }
    Info "Criando o banco $Banco pelos scripts (alguns minutos)..."
    $antes = $env:MYSQL_PWD
    try {
      $env:MYSQL_PWD = $senhaRoot
      & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $aqui 'recriar.ps1') -Banco $bancoSql -Empresa $empresa `
          -MySql $mysql -Usuario root -Forcar
      if ($LASTEXITCODE -ne 0) { throw "recriar.ps1 terminou com o código $LASTEXITCODE." }
    } finally { $env:MYSQL_PWD = $antes }
    $bancoNovo = $true
    Ok "Banco $Banco criado: empresa 001 ($($empresa.ToUpperInvariant())) e usuário ADMINISTRADOR / Admin123."
  }

  # --- 5. usuários do MySQL --------------------------------------------------------------------------------
  Etapa '5/9 Usuários do MySQL'
  # senhas novas a cada execução: ninguém precisa conhecê-las (app lê do web.config; backup do arquivo cifrado)
  $senhaApp = SenhaAleatoria
  $senhaBackup = SenhaAleatoria
  MySql $mysql $senhaRoot @"
CREATE USER IF NOT EXISTS 'labcontrol'@'localhost' IDENTIFIED BY $(SqlTexto $senhaApp);
ALTER USER 'labcontrol'@'localhost' IDENTIFIED BY $(SqlTexto $senhaApp);
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE, CREATE TEMPORARY TABLES ON ``$bancoSql``.* TO 'labcontrol'@'localhost';
CREATE USER IF NOT EXISTS 'labcontrol_backup'@'localhost' IDENTIFIED BY $(SqlTexto $senhaBackup);
ALTER USER 'labcontrol_backup'@'localhost' IDENTIFIED BY $(SqlTexto $senhaBackup);
GRANT SELECT, SHOW VIEW, TRIGGER, EVENT, LOCK TABLES, SHOW_ROUTINE ON *.* TO 'labcontrol_backup'@'localhost';
"@ | Out-Null
  Ok "labcontrol (app: leitura/gravação e procedures em $Banco) e labcontrol_backup (só leitura) prontos."

  # --- 6. pool do IIS e pastas -----------------------------------------------------------------------------
  Etapa '6/9 Pool do IIS e pastas'
  Import-Module WebAdministration
  if (-not (Test-Path "IIS:\AppPools\$Pool")) { New-WebAppPool -Name $Pool | Out-Null }
  $caminhoPool = "IIS:\AppPools\$Pool"
  Set-ItemProperty $caminhoPool -Name managedRuntimeVersion -Value ''
  Set-ItemProperty $caminhoPool -Name startMode -Value 'AlwaysRunning'
  # a sessão vale só para a execução do app (pendência 16): sem reciclagem periódica nem desligar por ociosidade,
  # senão os usuários cairiam no login no meio do dia
  Set-ItemProperty $caminhoPool -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
  Set-ItemProperty $caminhoPool -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)
  if ((Get-WebAppPoolState -Name $Pool).Value -eq 'Started') { Stop-WebAppPool -Name $Pool; Start-Sleep 3 }

  RestringirPasta $PastaDados
  if ($boletinsJaExistia -and -not $PastaBoletins.StartsWith($PastaDados, [StringComparison]::OrdinalIgnoreCase)) {
    # pasta escolhida que já existia (ex.: um disco compartilhado): só acrescenta a gravação do pool, sem tirar acessos
    $acl = Get-Acl $pastas.Boletins
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($IdentidadePool, 'Modify', 'ContainerInherit, ObjectInherit', 'None', 'Allow')))
    Set-Acl $pastas.Boletins $acl
  } else { RestringirPasta $pastas.Boletins $IdentidadePool }
  RestringirPasta $pastas.Chaves $IdentidadePool
  foreach ($p in 'Backup', 'Config', 'Logs', 'Cert') { RestringirPasta $pastas[$p] }
  Ok "Pastas prontas: boletins em $($pastas.Boletins); chaves, backups e logs em $PastaDados (acesso restrito)."

  # --- 7. arquivos do sistema ------------------------------------------------------------------------------
  Etapa '7/9 Arquivos do sistema'
  New-Item -ItemType Directory -Force -Path $pastaSite | Out-Null
  & robocopy.exe (Join-Path $pacote 'app') $pastaSite /MIR /XF appsettings.Production.json /XD storage logs /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy terminou com o código $LASTEXITCODE." }
  RestringirPasta $pastaSite
  $acl = Get-Acl $pastaSite
  $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($IdentidadePool, 'ReadAndExecute', 'ContainerInherit, ObjectInherit', 'None', 'Allow')))
  Set-Acl $pastaSite $acl

  # configuração de produção sem senha (as pastas); a connection string vai no web.config, legível só pelo pool
  $config = [ordered]@{
    AllowedHosts   = '*'
    Boletins       = @{ Pasta = $pastas.Boletins }
    DataProtection = @{ PastaChaves = $pastas.Chaves }
  }
  [IO.File]::WriteAllText((Join-Path $pastaSite 'appsettings.Production.json'), ($config | ConvertTo-Json), $utf8SemBom)
  $webConfig = Join-Path $pastaSite 'web.config'
  [xml]$xml = Get-Content $webConfig -Raw
  $anc = $xml.SelectSingleNode('//aspNetCore')
  if (-not $anc) { throw 'web.config publicado sem o elemento aspNetCore.' }
  $vars = $anc.SelectSingleNode('environmentVariables')
  if ($vars) { [void]$anc.RemoveChild($vars) }
  $vars = $xml.CreateElement('environmentVariables')
  foreach ($par in @(
      @('ASPNETCORE_ENVIRONMENT', 'Production'),
      @('ASPNETCORE_HTTPS_PORT', "$PortaHttps"),
      @('ConnectionStrings__LabControl', "Server=127.0.0.1;Port=3306;Database=$Banco;User=labcontrol;Password=$senhaApp"))) {
    $v = $xml.CreateElement('environmentVariable'); $v.SetAttribute('name', $par[0]); $v.SetAttribute('value', $par[1]); [void]$vars.AppendChild($v)
  }
  [void]$anc.AppendChild($vars)
  $xml.Save($webConfig)
  Ok "Sistema copiado para $pastaSite."

  # --- 8. HTTPS, site e firewall ---------------------------------------------------------------------------
  Etapa '8/9 Certificado, site e firewall'
  $fqdn = try { [Net.Dns]::GetHostEntry($NomeServidor).HostName } catch { $NomeServidor }
  $nomes = @($NomeServidor, $fqdn, 'localhost') | Select-Object -Unique
  $cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object {
    $_.FriendlyName -eq 'LabControl' -and $_.NotAfter -gt (Get-Date).AddDays(60) -and
    -not (Compare-Object @($_.DnsNameList.Unicode) $nomes)
  } | Sort-Object NotAfter -Descending | Select-Object -First 1
  if (-not $cert) {
    $cert = New-SelfSignedCertificate -DnsName $nomes -CertStoreLocation Cert:\LocalMachine\My -FriendlyName 'LabControl' `
              -NotAfter (Get-Date).AddYears(10) -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy NonExportable `
              -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.1')
    Info "Certificado autoassinado criado para $($nomes -join ', ') (válido até $($cert.NotAfter.ToString('dd/MM/yyyy')))."
  } else { Info "Certificado existente mantido (válido até $($cert.NotAfter.ToString('dd/MM/yyyy')))." }
  $cer = Join-Path $pastas.Cert 'LabControl.cer'
  Export-Certificate -Cert $cert -FilePath $cer -Force | Out-Null
  if (-not (Get-ChildItem Cert:\LocalMachine\Root | Where-Object Thumbprint -eq $cert.Thumbprint)) {
    Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
  }
  # instrução para as estações: confiar no certificado uma vez (sem isso o navegador avisa "não seguro")
  $estacao = @"
@echo off
rem LabControl: confia no certificado do servidor $NomeServidor (executar como administrador em cada estação)
certutil -addstore -f Root "%~dp0LabControl.cer"
pause
"@
  [IO.File]::WriteAllText((Join-Path $pastas.Cert 'instalar-certificado-na-estacao.cmd'), $estacao, (New-Object Text.ASCIIEncoding))

  # o "Default Web Site" de uma instalação nova do IIS ocupa a porta 80: parado, se for só a página padrão
  $padrao = Get-Website | Where-Object Name -eq 'Default Web Site'
  if ($padrao -and $padrao.State -eq 'Started') {
    $conteudo = Get-ChildItem ([Environment]::ExpandEnvironmentVariables($padrao.PhysicalPath)) -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notmatch '^iisstart\.(htm|png)$' }
    if (-not $conteudo) { Stop-Website 'Default Web Site'; Set-ItemProperty 'IIS:\Sites\Default Web Site' serverAutoStart False; Info 'Site padrão do IIS parado (só tinha a página inicial).' }
  }
  if (-not (Get-Website | Where-Object Name -eq $Site)) {
    New-Website -Name $Site -PhysicalPath $pastaSite -ApplicationPool $Pool -Port $PortaHttps -Ssl | Out-Null
  } else {
    Set-ItemProperty "IIS:\Sites\$Site" -Name physicalPath -Value $pastaSite
    Set-ItemProperty "IIS:\Sites\$Site" -Name applicationPool -Value $Pool
  }
  if (-not (Get-WebBinding -Name $Site -Protocol https -Port $PortaHttps)) {
    Get-WebBinding -Name $Site -Protocol https | Remove-WebBinding
    New-WebBinding -Name $Site -Protocol https -Port $PortaHttps | Out-Null
  }
  $sslPath = "IIS:\SslBindings\0.0.0.0!$PortaHttps"
  if (Test-Path $sslPath) {
    $atual = Get-Item $sslPath
    if ($atual.Thumbprint -ne $cert.Thumbprint) { Remove-Item $sslPath; (Get-WebBinding -Name $Site -Protocol https -Port $PortaHttps).AddSslCertificate($cert.Thumbprint, 'My') }
  } else { (Get-WebBinding -Name $Site -Protocol https -Port $PortaHttps).AddSslCertificate($cert.Thumbprint, 'My') }
  # http (80) só redireciona para o https (UseHttpsRedirection + ASPNETCORE_HTTPS_PORT), se a porta estiver livre
  $ocupa80 = Get-Website | Where-Object { $_.Name -ne $Site -and $_.State -eq 'Started' } | ForEach-Object { Get-WebBinding -Name $_.Name -Protocol http -Port 80 }
  if (-not (Get-WebBinding -Name $Site -Protocol http -Port 80)) {
    if ($ocupa80) { Aviso 'porta 80 em uso por outro site: o endereço http:// não redireciona (use https://).' }
    else { New-WebBinding -Name $Site -Protocol http -Port 80 | Out-Null }
  }
  foreach ($regra in @(@('LabControl (HTTPS)', $PortaHttps), @('LabControl (HTTP)', 80))) {
    Get-NetFirewallRule -DisplayName $regra[0] -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $regra[0] -Direction Inbound -Protocol TCP -LocalPort $regra[1] -Action Allow -Profile Any | Out-Null
  }
  Start-WebAppPool -Name $Pool
  if ((Get-Website -Name $Site).State -ne 'Started') { Start-Website -Name $Site }
  Ok "Site $Site em https://${NomeServidor}:$PortaHttps/ (porta liberada no firewall)."

  # --- 9. backup agendado e teste --------------------------------------------------------------------------
  Etapa '9/9 Backup diário e teste'
  $arquivoSenha = Join-Path $pastas.Config 'backup.senha'
  [IO.File]::WriteAllText($arquivoSenha, (ProtegerDpapi $senhaBackup), $utf8SemBom)
  $scriptBackup = Join-Path $aqui 'backup.ps1'
  $logBackup = Join-Path $pastas.Logs 'backup-ultimo.log'
  $comando = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptBackup`" -Destino `"$($pastas.Backup)`" -PastaApp `"$pastaSite`" " +
             "-Banco $bancoSql -Usuario labcontrol_backup -ArquivoSenha `"$arquivoSenha`" -MySqlBin `"$mysqlBin`" > `"$logBackup`" 2>&1"
  $acao = New-ScheduledTaskAction -Execute 'cmd.exe' -Argument "/c $comando"
  $gatilho = New-ScheduledTaskTrigger -Daily -At $HoraBackup
  $conta = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
  $opcoes = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2)
  Register-ScheduledTask -TaskName 'LabControl - Backup' -Action $acao -Trigger $gatilho -Principal $conta -Settings $opcoes -Force | Out-Null
  Ok "Tarefa 'LabControl - Backup' todo dia às $HoraBackup → $($pastas.Backup) (30 dias; log em $logBackup)."

  $url = "https://localhost:$PortaHttps/Conta/Login"
  for ($i = 0; ; $i++) {
    try { $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 30; if ($r.StatusCode -eq 200) { break } }
    catch { if ($i -ge 12) { throw "a tela de login não abriu em $url : $($_.Exception.Message). Veja o Visualizador de Eventos (Aplicativo, 'IIS AspNetCore Module V2')." } }
    Start-Sleep 5
  }
  Ok "Tela de login respondendo em $url."

  $leia = @"
LabControl v2 — instalado em $(Get-Date -Format 'dd/MM/yyyy HH:mm') em $env:COMPUTERNAME

Endereço:   https://${NomeServidor}:$PortaHttps/
Sistema:    $pastaSite
Dados:      $PastaDados
  boletins  $($pastas.Boletins)   (PDFs emitidos — entram no backup)
  chaves    $($pastas.Chaves)     (cifram a senha do e-mail — entram no backup)
  backup    $($pastas.Backup)     (tarefa "LabControl - Backup", todo dia às $HoraBackup, 30 dias)
MySQL:      serviço MySQL80 (ou o já existente), só local; banco $Banco
            usuários: root (senha definida no assistente — guarde-a), labcontrol e labcontrol_backup (senhas internas)

Acesso: usuário ADMINISTRADOR, senha Admin123 (banco novo) — ou os usuários e senhas do backup restaurado.
Sistema antigo: entre como ADMINISTRADOR e use Sistema › Migração com o banco destino  (o próprio
banco): a empresa e o ADMINISTRADOR da instalação são substituídos pelos dados do legado. O Firebird do sistema
antigo precisa estar rodando NESTE servidor (localhost:3050) — informe o caminho do .FDB na tela.
Estações: copiar a pasta $($pastas.Cert) e executar instalar-certificado-na-estacao.cmd como administrador
(uma vez por estação), senão o navegador avisa "não seguro".
Copie os backups para fora do servidor (outro disco, nuvem): eles têm os dados dos clientes.
Log desta instalação: $log
"@
  [IO.File]::WriteAllText((Join-Path $PastaDados 'LEIA-ME.txt'), $leia, (New-Object Text.UTF8Encoding($true)))
  Etapa 'Instalação concluída.'
  Write-Host $leia
  if ($reiniciar) { Aviso 'reinicie o Windows para concluir a instalação dos componentes.' }
  $codigo = if ($reiniciar) { 3010 } else { 0 }
} catch {
  Write-Host ''
  Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
  Write-Host "Log: $log" -ForegroundColor Red
  $codigo = 1
} finally {
  Stop-Transcript | Out-Null
}
exit $codigo
