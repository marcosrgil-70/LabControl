; LabControl v2 — instalador para o servidor Windows do laboratório (Inno Setup 6.1+).
; Gerado por instalador\construir.ps1, que publica o app e passa os pré-requisitos de prerequisitos.json (/D...).
; O assistente pede a empresa, as pastas, confere o que já existe no servidor (IIS, VC++, .NET, MySQL), baixa o que
; faltar (ou usa a pasta "prerequisitos" ao lado do setup, sem internet), pede a senha do root do MySQL e o banco:
; novo (LabControlDB, empresa + ADMINISTRADOR / Admin123) ou restaurado de um backup .zip. Quem instala de fato é
; o scripts\Instalar.ps1 (também serve para atualizar o sistema).

#ifndef Versao
  #define Versao "2.0.0"
#endif
#ifndef VersaoNumerica
  #define VersaoNumerica "2.0.0.0"
#endif
#ifndef Saida
  #define Saida "saida"
#endif
#define Banco "LabControlDB"

[Setup]
AppId={{35DB6820-7E4C-47DC-ADFF-962A158DA447}
AppName=LabControl
AppVersion={#Versao}
AppVerName=LabControl {#Versao}
AppPublisher=LabControl
VersionInfoVersion={#VersaoNumerica}
DefaultDirName=C:\LabControl
DisableProgramGroupPage=yes
DisableDirPage=no
UsePreviousAppDir=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.14393
OutputDir={#Saida}
OutputBaseFilename=LabControl-Setup-{#Versao}
Compression=lzma2/normal
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=no
UninstallDisplayName=LabControl {#Versao}
SetupLogging=yes

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
; sistema publicado (dotnet publish -c Release) — o Instalar.ps1 copia para {app}\site com o pool parado
Source: "{#Saida}\app\*"; DestDir: "{app}\pacote\app"; Flags: recursesubdirs createallsubdirs ignoreversion
; scripts de instalação, backup e desinstalação + scripts do banco (banco novo pelo recriar.ps1 -Empresa)
Source: "scripts\*.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\backup.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\recriar.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\0*.sql"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\inicial.sql"; DestDir: "{app}\scripts"; Flags: ignoreversion

[INI]
Filename: "{app}\LabControl.url"; Section: "InternetShortcut"; Key: "URL"; String: "{code:Endereco}"; Flags: uninsdeletesection

[Icons]
Name: "{commondesktop}\LabControl"; Filename: "{app}\LabControl.url"

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\Desinstalar.ps1"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoverIIS"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\site"
Type: filesandordirs; Name: "{app}\pacote"

[Code]
function SetEnvironmentVariable(lpName: string; lpValue: string): BOOL;
  external 'SetEnvironmentVariableW@kernel32.dll stdcall';

const
  QtdComponentes = 4;   // 0 IIS, 1 VC++, 2 .NET Hosting, 3 MySQL

var
  PaginaEmpresa: TInputQueryWizardPage;
  PaginaPastas: TInputDirWizardPage;
  PaginaComponentes: TWizardPage;
  PaginaMySql: TInputQueryWizardPage;
  PaginaBanco: TInputOptionWizardPage;
  PaginaBackup: TInputFileWizardPage;
  PaginaServidor: TInputQueryWizardPage;
  PaginaDownload: TDownloadWizardPage;
  Rotulos: array[0..3] of TNewStaticText;
  Baixar: array[0..3] of TNewCheckBox;
  Instalado: array[0..3] of Boolean;
  ServicoMySql: string;
  ResultadoInstalacao: Integer;

function ObterPrerequisitos(): Boolean; forward;

// ---------------------------------------------------------------------------------------------- detecção

function VersaoMaiorOuIgual(A, B: string): Boolean;
var
  I, X, Y: Integer;
  PA, PB: string;
begin
  // compara "10.0.14" com "10.0.9" número a número
  Result := True;
  for I := 1 to 4 do
  begin
    PA := A; PB := B;
    if Pos('.', A) > 0 then begin PA := Copy(A, 1, Pos('.', A) - 1); A := Copy(A, Pos('.', A) + 1, 99); end else A := '';
    if Pos('.', B) > 0 then begin PB := Copy(B, 1, Pos('.', B) - 1); B := Copy(B, Pos('.', B) + 1, 99); end else B := '';
    X := StrToIntDef(PA, 0); Y := StrToIntDef(PB, 0);
    if X > Y then Exit;
    if X < Y then begin Result := False; Exit; end;
  end;
end;

function TemIIS(): Boolean;
var
  V: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\InetStp', 'MajorVersion', V) and (V >= 10);
end;

function TemVc(): Boolean;
var
  V: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', V) and (V = 1);
end;

function TemHosting(): Boolean;
var
  Busca: TFindRec;
begin
  // runtime do ASP.NET Core 10 (igual ou mais novo que o do pacote) + módulo do IIS
  Result := False;
  if not FileExists(ExpandConstant('{commonpf64}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll')) then Exit;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.AspNetCore.App\10.*'), Busca) then
  try
    repeat
      if VersaoMaiorOuIgual(Busca.Name, '{#HostingVersao}') then Result := True;
    until Result or not FindNext(Busca);
  finally
    FindClose(Busca);
  end;
end;

function AcharMySql(): string;
var
  Nomes: TArrayOfString;
  I: Integer;
  Caminho: string;
begin
  // qualquer serviço MySQL* cujo executável seja o mysqld
  Result := '';
  if RegGetSubkeyNames(HKLM, 'SYSTEM\CurrentControlSet\Services', Nomes) then
    for I := 0 to GetArrayLength(Nomes) - 1 do
      if (Pos('MYSQL', Uppercase(Nomes[I])) = 1) and
         RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Services\' + Nomes[I], 'ImagePath', Caminho) and
         (Pos('MYSQLD', Uppercase(Caminho)) > 0) then
      begin
        Result := Nomes[I];
        Exit;
      end;
end;

procedure Detectar();
begin
  Instalado[0] := TemIIS();
  Instalado[1] := TemVc();
  Instalado[2] := TemHosting();
  ServicoMySql := AcharMySql();
  Instalado[3] := ServicoMySql <> '';
end;

// ---------------------------------------------------------------------------------------------- páginas

function Endereco(Param: string): string;
var
  Porta: string;
begin
  Porta := Trim(PaginaServidor.Values[1]);
  if Porta = '443' then
    Result := 'https://' + Trim(PaginaServidor.Values[0]) + '/'
  else
    Result := 'https://' + Trim(PaginaServidor.Values[0]) + ':' + Porta + '/';
end;

function BancoNovo(): Boolean;
begin
  Result := PaginaBanco.Values[0];
end;

function NomeComponente(I: Integer): string;
begin
  case I of
    0: Result := 'IIS (servidor web do Windows)';
    1: Result := 'Visual C++ 2015-2022 (x64)';
    2: Result := '.NET 10 Hosting Bundle ({#HostingVersao} ou mais novo)';
  else
    Result := 'MySQL 8.0';
  end;
end;

procedure AtualizarComponentes();
var
  I: Integer;
begin
  for I := 0 to QtdComponentes - 1 do
  begin
    if Instalado[I] then
    begin
      if I = 3 then Rotulos[I].Caption := '✔ ' + NomeComponente(I) + ' — já instalado (serviço ' + ServicoMySql + ')'
      else Rotulos[I].Caption := '✔ ' + NomeComponente(I) + ' — já instalado';
      Baixar[I].Visible := False;
    end
    else
    begin
      Rotulos[I].Caption := '✘ ' + NomeComponente(I) + ' — não encontrado';
      Baixar[I].Visible := True;
    end;
  end;
end;

procedure InitializeWizard();
var
  I, Topo: Integer;
  Nota: TNewStaticText;
begin
  Detectar();

  PaginaEmpresa := CreateInputQueryPage(wpWelcome, 'Empresa', 'Empresa usuária do sistema',
    'Nome (razão social) do laboratório que vai usar o LabControl. Num banco novo, ela é a empresa 001; ' +
    'ao migrar o sistema antigo (Sistema › Migração), os dados da empresa vêm do legado.');
  PaginaEmpresa.Add('Nome da empresa:', False);
  PaginaEmpresa.Values[0] := GetPreviousData('Empresa', '');

  PaginaPastas := CreateInputDirPage(wpSelectDir, 'Pastas de dados',
    'Onde ficam os boletins emitidos e os dados do sistema?',
    'Os boletins (PDFs emitidos) e os dados (backups diários, chaves de criptografia, logs e certificado) devem ficar fora ' +
    'da pasta do sistema — de preferência em outro disco. Entram no backup e nunca são apagados pela desinstalação.', False, '');
  PaginaPastas.Add('Pasta dos boletins:');
  PaginaPastas.Add('Pasta dos dados (backups, chaves, logs):');
  PaginaPastas.Values[0] := GetPreviousData('PastaBoletins', 'C:\LabControlDados\Boletins');
  PaginaPastas.Values[1] := GetPreviousData('PastaDados', 'C:\LabControlDados');

  PaginaComponentes := CreateCustomPage(PaginaPastas.ID, 'Componentes necessários',
    'O que o LabControl precisa neste servidor');
  Topo := 0;
  for I := 0 to QtdComponentes - 1 do
  begin
    Rotulos[I] := TNewStaticText.Create(PaginaComponentes);
    Rotulos[I].Parent := PaginaComponentes.Surface;
    Rotulos[I].Top := Topo;
    Rotulos[I].Font.Style := [fsBold];
    Baixar[I] := TNewCheckBox.Create(PaginaComponentes);
    Baixar[I].Parent := PaginaComponentes.Surface;
    Baixar[I].Top := Topo + ScaleY(18);
    Baixar[I].Left := ScaleX(16);
    Baixar[I].Width := PaginaComponentes.SurfaceWidth - ScaleX(16);
    Baixar[I].Checked := True;
    case I of
      0: Baixar[I].Caption := 'Ativar o IIS (recurso do Windows, sem download)';
      1: Baixar[I].Caption := 'Baixar e instalar o Visual C++ (~25 MB)';
      2: Baixar[I].Caption := 'Baixar e instalar o .NET {#HostingVersao} Hosting Bundle (~110 MB)';
    else
      Baixar[I].Caption := 'Baixar e instalar o MySQL {#MySqlVersao} (~240 MB; serviço MySQL80, só neste servidor)';
    end;
    Topo := Topo + ScaleY(46);
  end;
  Nota := TNewStaticText.Create(PaginaComponentes);
  Nota.Parent := PaginaComponentes.Surface;
  Nota.Top := Topo + ScaleY(4);
  Nota.Width := PaginaComponentes.SurfaceWidth;
  Nota.WordWrap := True;
  Nota.AutoSize := False;
  Nota.Height := ScaleY(48);
  Nota.Caption := 'Sem internet neste servidor: copie os instaladores para uma pasta "prerequisitos" ao lado do setup ' +
    '(gerada pelo construir.ps1) — o setup usa os arquivos de lá em vez de baixar.';
  AtualizarComponentes();

  PaginaMySql := CreateInputQueryPage(PaginaComponentes.ID, 'Banco de dados MySQL',
    'Senha do administrador (root) do MySQL',
    'Se o MySQL for instalado agora, esta será a senha nova do root (guarde-a em lugar seguro). Se ele já existir, ' +
    'informe a senha atual. O sistema e o backup usam usuários próprios, com senhas internas geradas pelo instalador.');
  PaginaMySql.Add('Senha do root:', True);
  PaginaMySql.Add('Confirme a senha:', True);

  PaginaBanco := CreateInputOptionPage(PaginaMySql.ID, 'Banco do sistema',
    'Banco novo ou backup?',
    'Se o banco {#Banco} já existir neste servidor (atualização), ele é mantido nas duas opções.', True, False);
  PaginaBanco.Add('Criar o banco {#Banco} zerado (empresa digitada + usuário ADMINISTRADOR / senha Admin123). ' +
    'Os dados do sistema antigo são trazidos depois, pelo próprio sistema: Sistema › Migração.');
  PaginaBanco.Add('Restaurar um backup (.zip) gerado pelo backup do LabControl');
  if GetPreviousData('Banco', 'novo') = 'backup' then PaginaBanco.SelectedValueIndex := 1 else PaginaBanco.SelectedValueIndex := 0;

  PaginaBackup := CreateInputFilePage(PaginaBanco.ID, 'Backup do banco',
    'Arquivo de backup (.zip)',
    'Escolha o .zip gerado pelo backup do LabControl (Scripts\v2\backup.ps1 ou a tarefa diária). Os usuários e senhas ' +
    'são os do backup. Se o banco {#Banco} já existir, a instalação para sem mexer nele.');
  PaginaBackup.Add('Arquivo de backup:', 'Backup do LabControl (*.zip)|*.zip', '.zip');

  PaginaServidor := CreateInputQueryPage(PaginaBackup.ID, 'Endereço do sistema',
    'Como as estações vão acessar o LabControl?',
    'O endereço será https://<nome do servidor>/ (com certificado autoassinado para esse nome).');
  PaginaServidor.Add('Nome do servidor na rede:', False);
  PaginaServidor.Add('Porta HTTPS:', False);
  PaginaServidor.Values[0] := GetPreviousData('NomeServidor', GetComputerNameString());
  PaginaServidor.Values[1] := GetPreviousData('PortaHttps', '443');

  PaginaDownload := CreateDownloadPage(SetupMessage(msgWizardPreparing), 'Baixando os componentes que faltam...', nil);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = PaginaBackup.ID) and BancoNovo();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  I, Porta: Integer;
begin
  Result := True;
  if CurPageID = PaginaEmpresa.ID then
  begin
    if Length(Trim(PaginaEmpresa.Values[0])) < 3 then begin MsgBox('Informe o nome da empresa (pelo menos 3 letras).', mbError, MB_OK); Result := False; end
    else if Length(Trim(PaginaEmpresa.Values[0])) > 150 then begin MsgBox('Nome da empresa com no máximo 150 caracteres.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaPastas.ID then
  begin
    if (Trim(PaginaPastas.Values[0]) = '') or (Trim(PaginaPastas.Values[1]) = '') then begin MsgBox('Informe as duas pastas.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaComponentes.ID then
  begin
    for I := 0 to QtdComponentes - 1 do
      if not Instalado[I] and not Baixar[I].Checked then
      begin
        MsgBox(NomeComponente(I) + ' é necessário. Marque a opção para instalar, ou instale antes e execute o setup de novo.', mbError, MB_OK);
        Result := False;
        Exit;
      end;
  end
  else if CurPageID = PaginaMySql.ID then
  begin
    if PaginaMySql.Values[0] = '' then begin MsgBox('Informe a senha do root do MySQL.', mbError, MB_OK); Result := False; end
    else if PaginaMySql.Values[0] <> PaginaMySql.Values[1] then begin MsgBox('As senhas não conferem.', mbError, MB_OK); Result := False; end
    else if (not Instalado[3]) and (Length(PaginaMySql.Values[0]) < 8) then begin MsgBox('A senha nova do root precisa ter pelo menos 8 caracteres.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaBackup.ID then
  begin
    if not FileExists(PaginaBackup.Values[0]) then begin MsgBox('Escolha o arquivo de backup (.zip).', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaServidor.ID then
  begin
    Porta := StrToIntDef(Trim(PaginaServidor.Values[1]), 0);
    if Trim(PaginaServidor.Values[0]) = '' then begin MsgBox('Informe o nome do servidor.', mbError, MB_OK); Result := False; end
    else if (Porta < 1) or (Porta > 65535) then begin MsgBox('Porta inválida.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = wpReady then
    Result := ObterPrerequisitos();
end;

// ---------------------------------------------------------------------------------------------- download

// Um pré-requisito em {tmp}: da pasta "prerequisitos" ao lado do setup (sem internet) ou baixado (com a URL
// alternativa, se a principal falhar). O hash vazio dispensa a conferência (VC++: o link da Microsoft muda de versão;
// o Instalar.ps1 confere a assinatura digital).
function Obter(Arquivo, Url, Alternativa, Sha256: string): Boolean;
var
  Local: string;
begin
  Result := True;
  Local := ExpandConstant('{src}\prerequisitos\' + Arquivo);
  if FileExists(Local) then
  begin
    Result := FileCopy(Local, ExpandConstant('{tmp}\' + Arquivo), False);
    if not Result then MsgBox('Não foi possível copiar ' + Local, mbError, MB_OK);
    Exit;
  end;
  PaginaDownload.Clear;
  PaginaDownload.Add(Url, Arquivo, Sha256);
  try
    PaginaDownload.Download;
  except
    if PaginaDownload.AbortedByUser or (Alternativa = '') then
    begin
      if not PaginaDownload.AbortedByUser then
        MsgBox('Falha ao baixar ' + Arquivo + ': ' + GetExceptionMessage + #13#10#13#10 +
               'Sem internet, coloque o arquivo numa pasta "prerequisitos" ao lado do setup.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    PaginaDownload.Clear;
    PaginaDownload.Add(Alternativa, Arquivo, Sha256);
    try
      PaginaDownload.Download;
    except
      MsgBox('Falha ao baixar ' + Arquivo + ': ' + GetExceptionMessage, mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function ObterPrerequisitos(): Boolean;
begin
  Result := True;
  PaginaDownload.Show;
  try
    if Result and not Instalado[1] then Result := Obter('{#VcArquivo}', '{#VcUrl}', '', '');
    if Result and not Instalado[2] then Result := Obter('{#HostingArquivo}', '{#HostingUrl}', '{#HostingUrl2}', '{#HostingSha}');
    if Result and not Instalado[3] then Result := Obter('{#MySqlArquivo}', '{#MySqlUrl}', '{#MySqlUrl2}', '{#MySqlSha}');
  finally
    PaginaDownload.Hide;
  end;
end;

// ---------------------------------------------------------------------------------------------- instalação

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'Empresa', Trim(PaginaEmpresa.Values[0]));
  SetPreviousData(PreviousDataKey, 'PastaBoletins', PaginaPastas.Values[0]);
  SetPreviousData(PreviousDataKey, 'PastaDados', PaginaPastas.Values[1]);
  SetPreviousData(PreviousDataKey, 'NomeServidor', Trim(PaginaServidor.Values[0]));
  SetPreviousData(PreviousDataKey, 'PortaHttps', Trim(PaginaServidor.Values[1]));
  if BancoNovo() then SetPreviousData(PreviousDataKey, 'Banco', 'novo') else SetPreviousData(PreviousDataKey, 'Banco', 'backup');
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  I: Integer;
  Banco, Componentes: string;
begin
  if BancoNovo() then Banco := '{#Banco} zerado (ou mantido, se já existir); login ADMINISTRADOR / Admin123'
  else Banco := '{#Banco} restaurado de ' + PaginaBackup.Values[0] + ' (ou mantido, se já existir)';
  Componentes := '';
  for I := 0 to QtdComponentes - 1 do
    if Instalado[I] then Componentes := Componentes + Space + NomeComponente(I) + ': já instalado' + NewLine
    else Componentes := Componentes + Space + NomeComponente(I) + ': será instalado' + NewLine;
  Result := 'Empresa:' + NewLine + Space + Trim(PaginaEmpresa.Values[0]) + NewLine + NewLine +
            'Sistema:' + NewLine + Space + ExpandConstant('{app}') + NewLine + NewLine +
            'Boletins:' + NewLine + Space + PaginaPastas.Values[0] + NewLine + NewLine +
            'Dados (backups, chaves, logs):' + NewLine + Space + PaginaPastas.Values[1] + NewLine + NewLine +
            'Banco:' + NewLine + Space + Banco + NewLine + NewLine +
            'Endereço:' + NewLine + Space + Endereco('') + NewLine + NewLine +
            'Componentes:' + NewLine + Componentes + NewLine +
            'A configuração abre uma janela com o andamento (alguns minutos).';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Parametros: string;
begin
  if CurStep = ssPostInstall then
  begin
    // senhas e o nome da empresa só por variável de ambiente do processo (nem linha de comando, nem log)
    SetEnvironmentVariable('LC_SENHA_ROOT', PaginaMySql.Values[0]);
    SetEnvironmentVariable('LC_EMPRESA', Trim(PaginaEmpresa.Values[0]));
    Parametros := '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\scripts\Instalar.ps1') + '"' +
      ' -PastaApp "' + ExpandConstant('{app}') + '"' +
      ' -PastaDados "' + PaginaPastas.Values[1] + '"' +
      ' -PastaBoletins "' + PaginaPastas.Values[0] + '"' +
      ' -PastaPrerequisitos "' + ExpandConstant('{tmp}') + '"' +
      ' -Banco "{#Banco}"' +
      ' -NomeServidor "' + Trim(PaginaServidor.Values[0]) + '"' +
      ' -PortaHttps ' + Trim(PaginaServidor.Values[1]);
    if not BancoNovo() then
      Parametros := Parametros + ' -ArquivoBackup "' + PaginaBackup.Values[0] + '"';
    WizardForm.StatusLabel.Caption := 'Instalando os componentes e configurando o servidor (veja a janela do PowerShell)...';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Parametros, '', SW_SHOW, ewWaitUntilTerminated, ResultadoInstalacao) then
      ResultadoInstalacao := 1;
    SetEnvironmentVariable('LC_SENHA_ROOT', '');
    SetEnvironmentVariable('LC_EMPRESA', '');
    if ResultadoInstalacao = 1 then
      MsgBox('A configuração do servidor falhou. O motivo está no log em ' + PaginaPastas.Values[1] + '\logs. ' +
             'Corrija e execute o instalador de novo (ele continua de onde parou).', mbCriticalError, MB_OK);
  end;
end;

function NeedRestart(): Boolean;
begin
  Result := ResultadoInstalacao = 3010;
end;
