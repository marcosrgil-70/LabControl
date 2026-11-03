<#
.SYNOPSIS
  Gera o Manual do Usuário (Manual.pdf, na raiz do repositório) a partir de docs/manual/manual.html.

.DESCRIPTION
  1. Sobe o sistema num banco temporário com um laboratório fictício (nenhum dado real) e tira as imagens das telas
     em docs/manual/img (ferramenta ManualTests, Edge sem janela) — ~4 min, a maior parte criando o banco temporário;
  2. imprime o manual.html em A4 pelo Edge, com o número da página no rodapé.
  -SoPdf: só refaz o PDF (texto mudou, telas não) — mais rápido, mas ainda cria o banco temporário.
  Não rode com o "dotnet run" aberto (o build fica travado).

.EXAMPLE
  .\docs\manual\gerar-manual.ps1
  .\docs\manual\gerar-manual.ps1 -SoPdf
#>
[CmdletBinding()]
param([switch]$SoPdf)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path (Split-Path $PSScriptRoot)
$env:LABCONTROL_MANUAL = $PSScriptRoot
$env:LABCONTROL_MANUAL_SO_PDF = if ($SoPdf) { '1' } else { '' }
try {
  & dotnet test (Join-Path $raiz 'tests\LabControl.Tests') --filter 'FullyQualifiedName~ManualTests' --logger 'console;verbosity=detailed'
  if ($LASTEXITCODE -ne 0) { Write-Host 'ERRO: a geração do manual falhou (veja acima).' -ForegroundColor Red; exit 1 }
  Write-Host "Manual gerado: $(Join-Path $raiz 'Manual.pdf')" -ForegroundColor Green
} finally {
  Remove-Item env:LABCONTROL_MANUAL, env:LABCONTROL_MANUAL_SO_PDF -ErrorAction SilentlyContinue
}
