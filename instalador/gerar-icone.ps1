<#
.SYNOPSIS
  Gera o ícone do LabControl: frasco de laboratório (o bi-flask da barra do sistema) no azul da barra (#1a2f4e).
.DESCRIPTION
  Grava instalador\LabControl.ico (setup, atalho da área de trabalho, "Adicionar ou remover programas") e
  wwwroot\favicon.ico (aba do navegador), em 16, 24, 32, 48, 64, 128 e 256 px. Só precisa rodar de novo se o desenho mudar.
#>
param([string[]]$Saida = @())
if (-not $Saida) { $Saida = (Join-Path $PSScriptRoot 'LabControl.ico'), (Join-Path (Split-Path $PSScriptRoot) 'wwwroot\favicon.ico') }
Add-Type -AssemblyName System.Drawing
$azul = [Drawing.ColorTranslator]::FromHtml('#1a2f4e')
$liquido = [Drawing.ColorTranslator]::FromHtml('#3fc1b0')

function Desenhar([int]$t) {
  $bmp = New-Object Drawing.Bitmap $t, $t, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.Clear([Drawing.Color]::Transparent)
  $k = $t / 256.0
  function P([double]$x, [double]$y) { New-Object Drawing.PointF ([float]($x * $k)), ([float]($y * $k)) }

  # fundo: quadrado arredondado
  $r = 56 * $k; $m = 4 * $k; $l = $t - 2 * $m
  $fundo = New-Object Drawing.Drawing2D.GraphicsPath
  $fundo.AddArc($m, $m, 2 * $r, 2 * $r, 180, 90); $fundo.AddArc($m + $l - 2 * $r, $m, 2 * $r, 2 * $r, 270, 90)
  $fundo.AddArc($m + $l - 2 * $r, $m + $l - 2 * $r, 2 * $r, 2 * $r, 0, 90); $fundo.AddArc($m, $m + $l - 2 * $r, 2 * $r, 2 * $r, 90, 90)
  $fundo.CloseFigure()
  $g.FillPath((New-Object Drawing.SolidBrush $azul), $fundo)

  # frasco (Erlenmeyer): gargalo e corpo
  $corpo = New-Object Drawing.Drawing2D.GraphicsPath
  $corpo.AddLines([Drawing.PointF[]]@((P 106 52), (P 106 108), (P 56 200), (P 64 214), (P 192 214), (P 200 200), (P 150 108), (P 150 52)))
  $corpo.CloseFigure()
  # líquido: parte de baixo do corpo
  $g.SetClip($corpo)
  $g.FillRectangle((New-Object Drawing.SolidBrush $liquido), [float]0, [float](150 * $k), [float]$t, [float]$t)
  if ($t -ge 48) {
    $bolha = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(200, 255, 255, 255))
    $g.FillEllipse($bolha, [float](104 * $k), [float](170 * $k), [float](16 * $k), [float](16 * $k))
    $g.FillEllipse($bolha, [float](134 * $k), [float](186 * $k), [float](11 * $k), [float](11 * $k))
  }
  $g.ResetClip()
  $caneta = New-Object Drawing.Pen ([Drawing.Color]::White), ([float][Math]::Max(1.6, 14 * $k))
  $caneta.LineJoin = 'Round'; $caneta.StartCap = 'Round'; $caneta.EndCap = 'Round'
  $g.DrawPath($caneta, $corpo)
  # borda do gargalo
  $g.DrawLine($caneta, (P 92 50), (P 164 50))
  $g.Dispose()
  $bmp
}

$tamanhos = 16, 24, 32, 48, 64, 128, 256
$imagens = foreach ($t in $tamanhos) {
  $ms = New-Object IO.MemoryStream
  (Desenhar $t).Save($ms, [Drawing.Imaging.ImageFormat]::Png)
  , $ms.ToArray()
}
# .ico com as imagens em PNG (aceito pelo Windows Vista em diante e pelo Inno Setup)
$ico = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $ico
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$tamanhos.Count)
$deslocamento = 6 + 16 * $tamanhos.Count
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
  $t = $tamanhos[$i]; $dados = $imagens[$i]
  $w.Write([byte]($t % 256)); $w.Write([byte]($t % 256)); $w.Write([byte]0); $w.Write([byte]0)
  $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$dados.Length); $w.Write([uint32]$deslocamento)
  $deslocamento += $dados.Length
}
foreach ($dados in $imagens) { $w.Write($dados) }
$w.Flush()
foreach ($s in $Saida) { [IO.File]::WriteAllBytes($s, $ico.ToArray()); "gravado $s ($($ico.Length) bytes)" }
