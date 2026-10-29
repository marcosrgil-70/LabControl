; LabControl v2 — instalador para o servidor Windows do laboratório (Inno Setup 6).
; Gerado por instalador\construir.ps1, que publica o app e confere os pré-requisitos antes de compilar.
; O assistente pede pastas, endereço, senha do root do MySQL, o backup do banco migrado e a senha provisória do
; ADMINISTRADOR; quem instala de fato é o scripts\Instalar.ps1 (também serve para atualizar o sistema).

#ifndef Versao
  #define Versao "2.0.0"
#endif
#ifndef VersaoNumerica
  #define VersaoNumerica "2.0.0.0"
#endif
#ifndef Saida
  #define Saida "saida"
#endif

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
SolidCompression=no
WizardStyle=modern
ShowLanguageDialog=no
UninstallDisplayName=LabControl {#Versao}
SetupLogging=yes

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
; sistema publicado (dotnet publish -c Release) — o Instalar.ps1 copia para {app}\site com o pool parado
Source: "{#Saida}\app\*"; DestDir: "{app}\pacote\app"; Flags: recursesubdirs createallsubdirs ignoreversion
; pré-requisitos offline — apagados depois da instalação
Source: "prerequisitos\dotnet-hosting-*.exe"; DestDir: "{app}\pacote\prerequisitos"; Flags: ignoreversion deleteafterinstall
Source: "prerequisitos\vc_redist.x64.exe"; DestDir: "{app}\pacote\prerequisitos"; Flags: ignoreversion deleteafterinstall
Source: "prerequisitos\mysql-8.0.*-winx64.zip"; DestDir: "{app}\pacote\prerequisitos"; Flags: ignoreversion deleteafterinstall nocompression
; scripts de instalação, backup e desinstalação
Source: "scripts\*.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\backup.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\Scripts\v2\recriar.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

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

var
  PaginaDados: TInputDirWizardPage;
  PaginaServidor: TInputQueryWizardPage;
  PaginaMySql: TInputQueryWizardPage;
  PaginaBackup: TInputFileWizardPage;
  PaginaAdmin: TInputQueryWizardPage;
  ChaveRestaurar: TNewCheckBox;
  ResultadoInstalacao: Integer;

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

function JaInstalado(): Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\site\LabControl.dll'));
end;

procedure InitializeWizard();
begin
  PaginaDados := CreateInputDirPage(wpSelectDir, 'Pasta dos dados',
    'Onde ficam os boletins emitidos, as chaves de criptografia e os backups?',
    'Use uma pasta fora da pasta do sistema (de preferência em outro disco, se houver). ' +
    'Ela entra no backup diário e nunca é apagada pela desinstalação.', False, '');
  PaginaDados.Add('');
  PaginaDados.Values[0] := GetPreviousData('PastaDados', 'C:\LabControlDados');

  PaginaServidor := CreateInputQueryPage(PaginaDados.ID, 'Endereço do sistema',
    'Como as estações vão acessar o LabControl?',
    'O endereço será https://<nome do servidor>/ (com certificado autoassinado para esse nome).');
  PaginaServidor.Add('Nome do servidor na rede:', False);
  PaginaServidor.Add('Porta HTTPS:', False);
  PaginaServidor.Values[0] := GetPreviousData('NomeServidor', GetComputerNameString());
  PaginaServidor.Values[1] := GetPreviousData('PortaHttps', '443');

  PaginaMySql := CreateInputQueryPage(PaginaServidor.ID, 'Banco de dados MySQL',
    'Senha do administrador (root) do MySQL',
    'Se o MySQL ainda não estiver instalado neste servidor, ele será instalado agora e esta será a senha nova do root ' +
    '(guarde-a em lugar seguro). Se já existir, informe a senha atual. O sistema e o backup usam usuários próprios, ' +
    'com senhas internas geradas pelo instalador.');
  PaginaMySql.Add('Senha do root:', True);
  PaginaMySql.Add('Confirme a senha:', True);

  PaginaBackup := CreateInputFilePage(PaginaMySql.ID, 'Dados do laboratório',
    'Backup do banco migrado (.zip)',
    'Na primeira instalação, escolha o .zip gerado pelo Scripts\v2\backup.ps1 a partir do banco migrado (T07). ' +
    'Numa atualização (o banco labcontrol_v2 já existe neste servidor), deixe em branco: o banco é mantido.');
  PaginaBackup.Add('Arquivo de backup:', 'Backup do LabControl (*.zip)|*.zip', '.zip');
  ChaveRestaurar := TNewCheckBox.Create(PaginaBackup);
  ChaveRestaurar.Parent := PaginaBackup.Surface;
  ChaveRestaurar.Top := PaginaBackup.Edits[0].Top + PaginaBackup.Edits[0].Height + ScaleY(16);
  ChaveRestaurar.Width := PaginaBackup.SurfaceWidth;
  ChaveRestaurar.Caption := 'Restaurar também as chaves de criptografia (só para recuperar este mesmo servidor)';

  PaginaAdmin := CreateInputQueryPage(PaginaBackup.ID, 'Primeiro acesso',
    'Senha provisória do usuário ADMINISTRADOR',
    'O ADMINISTRADOR do banco restaurado recebe esta senha e é obrigado a trocá-la no primeiro acesso. ' +
    'Mínimo de 8 caracteres.');
  PaginaAdmin.Add('Senha provisória:', True);
  PaginaAdmin.Add('Confirme a senha:', True);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = PaginaAdmin.ID) and (Trim(PaginaBackup.Values[0]) = '');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Porta: Integer;
begin
  Result := True;
  if CurPageID = PaginaServidor.ID then
  begin
    Porta := StrToIntDef(Trim(PaginaServidor.Values[1]), 0);
    if Trim(PaginaServidor.Values[0]) = '' then begin MsgBox('Informe o nome do servidor.', mbError, MB_OK); Result := False; end
    else if (Porta < 1) or (Porta > 65535) then begin MsgBox('Porta inválida.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaMySql.ID then
  begin
    if PaginaMySql.Values[0] = '' then begin MsgBox('Informe a senha do root do MySQL.', mbError, MB_OK); Result := False; end
    else if PaginaMySql.Values[0] <> PaginaMySql.Values[1] then begin MsgBox('As senhas não conferem.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaBackup.ID then
  begin
    if (Trim(PaginaBackup.Values[0]) = '') and not JaInstalado() then
      Result := MsgBox('Sem o backup, a instalação só funciona se o banco labcontrol_v2 já existir neste servidor. Continuar?',
                       mbConfirmation, MB_YESNO) = IDYES
    else if (Trim(PaginaBackup.Values[0]) <> '') and not FileExists(PaginaBackup.Values[0]) then
    begin MsgBox('Arquivo de backup não encontrado.', mbError, MB_OK); Result := False; end;
  end
  else if CurPageID = PaginaAdmin.ID then
  begin
    if Length(PaginaAdmin.Values[0]) < 8 then begin MsgBox('A senha provisória precisa ter pelo menos 8 caracteres.', mbError, MB_OK); Result := False; end
    else if PaginaAdmin.Values[0] <> PaginaAdmin.Values[1] then begin MsgBox('As senhas não conferem.', mbError, MB_OK); Result := False; end;
  end;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'PastaDados', PaginaDados.Values[0]);
  SetPreviousData(PreviousDataKey, 'NomeServidor', Trim(PaginaServidor.Values[0]));
  SetPreviousData(PreviousDataKey, 'PortaHttps', Trim(PaginaServidor.Values[1]));
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  Banco: string;
begin
  if Trim(PaginaBackup.Values[0]) = '' then Banco := 'manter o banco labcontrol_v2 existente'
  else Banco := 'restaurar ' + PaginaBackup.Values[0];
  Result := 'Sistema:' + NewLine + Space + ExpandConstant('{app}') + NewLine + NewLine +
            'Dados (boletins, chaves, backups):' + NewLine + Space + PaginaDados.Values[0] + NewLine + NewLine +
            'Endereço:' + NewLine + Space + Endereco('') + NewLine + NewLine +
            'Banco:' + NewLine + Space + Banco + NewLine + NewLine +
            'Serão instalados, se faltarem: IIS, Visual C++ 2015-2022, .NET 10 Hosting Bundle e MySQL 8.0.' + NewLine +
            'A instalação abre uma janela com o andamento (alguns minutos).';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Parametros: string;
begin
  if CurStep = ssPostInstall then
  begin
    // senhas só por variável de ambiente do processo (não aparecem na linha de comando nem no log)
    SetEnvironmentVariable('LC_SENHA_ROOT', PaginaMySql.Values[0]);
    SetEnvironmentVariable('LC_SENHA_ADMIN', PaginaAdmin.Values[0]);
    Parametros := '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\scripts\Instalar.ps1') + '"' +
      ' -PastaApp "' + ExpandConstant('{app}') + '"' +
      ' -PastaDados "' + PaginaDados.Values[0] + '"' +
      ' -NomeServidor "' + Trim(PaginaServidor.Values[0]) + '"' +
      ' -PortaHttps ' + Trim(PaginaServidor.Values[1]);
    if Trim(PaginaBackup.Values[0]) <> '' then
      Parametros := Parametros + ' -ArquivoBackup "' + PaginaBackup.Values[0] + '"';
    if ChaveRestaurar.Checked then
      Parametros := Parametros + ' -RestaurarChaves';
    WizardForm.StatusLabel.Caption := 'Instalando os componentes e configurando o servidor (veja a janela do PowerShell)...';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Parametros, '', SW_SHOW, ewWaitUntilTerminated, ResultadoInstalacao) then
      ResultadoInstalacao := 1;
    SetEnvironmentVariable('LC_SENHA_ROOT', '');
    SetEnvironmentVariable('LC_SENHA_ADMIN', '');
    if ResultadoInstalacao = 1 then
      MsgBox('A configuração do servidor falhou. O motivo está no log em ' + PaginaDados.Values[0] + '\logs. ' +
             'Corrija e execute o instalador de novo (ele continua de onde parou).', mbCriticalError, MB_OK);
  end;
end;

function NeedRestart(): Boolean;
begin
  Result := ResultadoInstalacao = 3010;
end;
