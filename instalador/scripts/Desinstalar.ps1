<#
.SYNOPSIS
  Remove o LabControl v2 do IIS (chamado pela desinstalação do setup.exe).

.DESCRIPTION
  Remove o site e o pool do IIS, a tarefa de backup e as regras do firewall.
  NÃO remove: o MySQL, o banco (LabControlDB), a pasta de dados (boletins, chaves, backups), o certificado,
  o IIS e o .NET — os dados do laboratório nunca são apagados por aqui.
#>
$ErrorActionPreference = 'Continue'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $aqui 'Funcoes.ps1')

Etapa 'LabControl v2 — desinstalação'
try {
  Import-Module WebAdministration -ErrorAction Stop
  if (Get-Website | Where-Object Name -eq 'LabControl') { Remove-Website -Name 'LabControl'; Ok 'Site removido.' }
  if (Test-Path 'IIS:\AppPools\LabControl') { Remove-WebAppPool -Name 'LabControl'; Ok 'Pool removido.' }
} catch { Aviso "IIS: $($_.Exception.Message)" }
if (Get-ScheduledTask -TaskName 'LabControl - Backup' -ErrorAction SilentlyContinue) {
  Unregister-ScheduledTask -TaskName 'LabControl - Backup' -Confirm:$false; Ok 'Tarefa de backup removida.'
}
Get-NetFirewallRule -DisplayName 'LabControl (HTTP*' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Info 'Mantidos: MySQL, banco (LabControlDB), pastas dos boletins e de dados (boletins, chaves, backups) e certificado.'
exit 0
