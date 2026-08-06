; Aganox LMS — Enterprise on-prem server installer (Inno Setup 6)
; Detects SQL instances; installs Express when none found; creates database on chosen instance.
; Build: .\scripts\build-installer.ps1

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\publish\server-x64"
#endif

#define AppName "Aganox LMS"
#define AppVersion "1.0.0"
#define AppPublisher "Aganox"
#define ServiceName "AganoxLmsApi"
#define SqlExpressInstance "SQLEXPRESS"

[Setup]
AppId={{B8E4F2A1-3C5D-4E7F-9A1B-2D4E6F8091C3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Aganox\LMS
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
OutputBaseFilename=AganoxLms-Setup-{#AppVersion}-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Aganox.Lms.Api.exe
SetupLogging=yes
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut to the web UI"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "createdatabase"; Description: "Create the &AganoxLms database if it does not exist"; GroupDescription: "Database:"; Flags: checkedonce
Name: "firewall"; Description: "Add Windows &Firewall rule for the HTTP port"; GroupDescription: "Network:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "prerequisites\SQL2022-SSEI-Expr.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsSqlExpressBootstrapper

[Icons]
Name: "{group}\{#AppName} Web UI"; Filename: "{code:GetWebUiUrl}"; Flags: excludefromshowinnewinstall
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{code:GetWebUiUrl}"; Tasks: desktopicon

[Run]
Filename: "{code:GetWebUiUrl}"; Description: "Open {#AppName} in browser"; Flags: postinstall shellexec skipifsilent nowait

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\tools\Uninstall-Service.ps1"""; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
const
  SqlInstanceRegKey = 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL';

var
  SqlExpressPage: TInputOptionWizardPage;
  ConfigPage: TInputQueryWizardPage;
  InstallerLogDir: String;
  SqlServer: String;
  SqlDatabase: String;
  HttpPort: Integer;
  ServiceAccount: String;
  SqlInstancesFound: Boolean;
  InstallSqlExpress: Boolean;
  InstallFailed: Boolean;
  InstallFailureMessage: String;

function GetWebUiUrl(Param: String): String; forward;
function NeedsSqlExpressBootstrapper: Boolean; forward;
function BoolToStr(Value: Boolean): String; forward;

function InstanceNameToServer(const InstanceName: String): String;
begin
  if CompareText(InstanceName, 'MSSQLSERVER') = 0 then
    Result := '.'
  else
    Result := '.\' + InstanceName;
end;

function TryPickInstanceFromKey(const RootKey: Integer; var ServerName: String): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if not RegGetValueNames(RootKey, SqlInstanceRegKey, Names) then
    Exit;
  if GetArrayLength(Names) = 0 then
    Exit;

  for I := 0 to GetArrayLength(Names) - 1 do
  begin
    if CompareText(Names[I], '{#SqlExpressInstance}') = 0 then
    begin
      ServerName := InstanceNameToServer(Names[I]);
      Result := True;
      Exit;
    end;
  end;

  for I := 0 to GetArrayLength(Names) - 1 do
  begin
    if CompareText(Names[I], 'MSSQLSERVER') = 0 then
    begin
      ServerName := InstanceNameToServer(Names[I]);
      Result := True;
      Exit;
    end;
  end;

  ServerName := InstanceNameToServer(Names[0]);
  Result := True;
end;

procedure DetectSqlInstances;
var
  ServerName: String;
begin
  SqlInstancesFound := False;
  InstallSqlExpress := True;
  SqlServer := InstanceNameToServer('{#SqlExpressInstance}');

  if TryPickInstanceFromKey(HKLM, ServerName) then
  begin
    SqlInstancesFound := True;
    SqlServer := ServerName;
    InstallSqlExpress := False;
    Log('Detected SQL instance (HKLM): ' + SqlServer);
    Exit;
  end;

  if IsWin64 and TryPickInstanceFromKey(HKLM32, ServerName) then
  begin
    SqlInstancesFound := True;
    SqlServer := ServerName;
    InstallSqlExpress := False;
    Log('Detected SQL instance (WOW6432Node): ' + SqlServer);
    Exit;
  end;

  Log('No SQL Server instance detected. SQL Express install will be offered by default.');
end;

function NeedsSqlExpressBootstrapper: Boolean;
begin
  Result := InstallSqlExpress;
end;

function InstallerLogDirReady: Boolean;
begin
  Result := (InstallerLogDir <> '') and DirExists(InstallerLogDir);
end;

procedure EnsureInstallerLogDir;
var
  AppDir: String;
begin
  if InstallerLogDirReady then
    Exit;

  AppDir := ExpandConstant('{app}');
  if (AppDir = '') or (not DirExists(AppDir)) then
    Exit;

  InstallerLogDir := AppDir + '\logs\installer';
  if not DirExists(InstallerLogDir) then
    ForceDirectories(InstallerLogDir);
end;

procedure AppendInstallSummary(const Line: String);
var
  SummaryPath: String;
  Existing: AnsiString;
begin
  EnsureInstallerLogDir;
  SummaryPath := InstallerLogDir + '\install-summary.txt';
  Existing := '';
  if FileExists(SummaryPath) then
    LoadStringFromFile(SummaryPath, Existing);
  if Length(Existing) > 0 then
    Existing := Existing + #13#10;
  Existing := Existing + Line;
  SaveStringToFile(SummaryPath, Existing, False);
end;

procedure FailInstall(const Message: String);
begin
  InstallFailed := True;
  InstallFailureMessage := Message;
  Log('INSTALL FAILED: ' + Message);
  AppendInstallSummary('ERROR: ' + Message);
end;

function GetWebUiUrl(Param: String): String;
begin
  Result := 'http://localhost:' + IntToStr(HttpPort) + '/';
end;

function ParsePort(const Value: String; out Port: Integer): Boolean;
var
  Trimmed: String;
  Parsed: Integer;
begin
  Result := False;
  Trimmed := Trim(Value);
  if Trimmed = '' then
    Exit;

  Parsed := StrToIntDef(Trimmed, -1);
  if (Parsed < 1) or (Parsed > 65535) then
    Exit;

  Port := Parsed;
  Result := True;
end;

function InitializeSetup(): Boolean;
var
  ParamValue: String;
begin
  InstallFailed := False;
  InstallFailureMessage := '';
  SqlDatabase := 'AganoxLms';
  HttpPort := 8080;
  ServiceAccount := 'LocalSystem';

  DetectSqlInstances;

  ParamValue := ExpandConstant('{param:SqlServer}');
  if ParamValue <> '' then SqlServer := ParamValue;

  ParamValue := ExpandConstant('{param:SqlDatabase}');
  if ParamValue <> '' then SqlDatabase := ParamValue;

  ParamValue := ExpandConstant('{param:Port}');
  if (ParamValue <> '') and not ParsePort(ParamValue, HttpPort) then
  begin
    MsgBox('Invalid /Port value: ' + ParamValue, mbError, MB_OK);
    Result := False;
    Exit;
  end;

  ParamValue := ExpandConstant('{param:ServiceAccount}');
  if ParamValue <> '' then ServiceAccount := ParamValue;

  ParamValue := ExpandConstant('{param:InstallSqlExpress}');
  if CompareText(ParamValue, '1') = 0 then
    InstallSqlExpress := True
  else if CompareText(ParamValue, '0') = 0 then
    InstallSqlExpress := False;

  if InstallSqlExpress then
    SqlServer := InstanceNameToServer('{#SqlExpressInstance}');

  Result := True;
end;

procedure InitializeWizard;
var
  AfterPage: Integer;
  ConfigDescription: String;
begin
  if not SqlInstancesFound then
  begin
    SqlExpressPage := CreateInputOptionPage(wpSelectDir,
      'SQL Server Setup',
      'No SQL Server instance was detected on this computer.',
      'Choose how to provide SQL Server for Aganox LMS. Installing SQL Server Express is recommended for new deployments.' + #13#10 +
      'Internet access is required during Express setup to download engine files.',
      True, False);
    SqlExpressPage.Add('Install SQL Server 2022 Express ({#SqlExpressInstance} instance) — recommended');
    SqlExpressPage.Add('Skip SQL Server install (I will configure an existing server manually)');
    SqlExpressPage.SelectedValueIndex := 0;
    AfterPage := SqlExpressPage.ID;
    ConfigDescription :=
      'If you chose to install SQL Server Express, instance .\SQLEXPRESS will be configured after setup completes.';
  end
  else
  begin
    AfterPage := wpSelectDir;
    ConfigDescription :=
      'An existing SQL Server instance was detected. The AganoxLms database will be created on the selected instance unless you disable that task on the next page.';
  end;

  ConfigPage := CreateInputQueryPage(
    AfterPage,
    'Database and Service Configuration',
    'Configure the SQL Server connection and web listener.',
    ConfigDescription);

  ConfigPage.Add('SQL Server instance (e.g. .\SQLEXPRESS or .):', False);
  ConfigPage.Add('Database name:', False);
  ConfigPage.Add('HTTP port:', False);
  ConfigPage.Add('Service account (LocalSystem recommended):', False);

  ConfigPage.Values[0] := SqlServer;
  ConfigPage.Values[1] := SqlDatabase;
  ConfigPage.Values[2] := IntToStr(HttpPort);
  ConfigPage.Values[3] := ServiceAccount;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (SqlExpressPage <> nil) and (PageID = SqlExpressPage.ID) then
    Result := SqlInstancesFound;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  PortValue: Integer;
begin
  Result := True;

  if (SqlExpressPage <> nil) and (CurPageID = SqlExpressPage.ID) then
  begin
    InstallSqlExpress := (SqlExpressPage.SelectedValueIndex = 0);
    if InstallSqlExpress then
      SqlServer := InstanceNameToServer('{#SqlExpressInstance}')
    else
      SqlServer := ConfigPage.Values[0];
    ConfigPage.Values[0] := SqlServer;
    Exit;
  end;

  if CurPageID = ConfigPage.ID then
  begin
    SqlServer := Trim(ConfigPage.Values[0]);
    SqlDatabase := Trim(ConfigPage.Values[1]);
    ServiceAccount := Trim(ConfigPage.Values[3]);

    if SqlServer = '' then
    begin
      MsgBox('SQL Server instance is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if SqlDatabase = '' then
    begin
      MsgBox('Database name is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if not ParsePort(Trim(ConfigPage.Values[2]), PortValue) then
    begin
      MsgBox('Enter a valid HTTP port (1-65535).', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    HttpPort := PortValue;

    if (not SqlInstancesFound) and (not InstallSqlExpress) then
    begin
      if MsgBox(
        'SQL Server Express will not be installed and no existing instance was detected.' + #13#10#13#10 +
        'Continue only if SQL Server is reachable at "' + SqlServer + '".',
        mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;

function RunInstallSqlExpress: Boolean;
var
  BootstrapperPath, Ps1Path, LogPath, Params: String;
  ResultCode: Integer;
begin
  Result := True;
  if not InstallSqlExpress then
  begin
    AppendInstallSummary('SQL Server Express install skipped.');
    Exit;
  end;

  BootstrapperPath := ExpandConstant('{tmp}\SQL2022-SSEI-Expr.exe');
  Ps1Path := ExpandConstant('{app}\tools\Install-SqlExpress.ps1');
  LogPath := InstallerLogDir + '\Install-SqlExpress.log';

  if not FileExists(Ps1Path) then
  begin
    FailInstall('Install-SqlExpress.ps1 not found.');
    Result := False;
    Exit;
  end;

  if not FileExists(BootstrapperPath) then
  begin
    FailInstall('SQL2022-SSEI-Expr.exe not found in temporary folder.');
    Result := False;
    Exit;
  end;

  AppendInstallSummary('SQL Server Express install starting for instance {#SqlExpressInstance}...');
  if WizardForm <> nil then
    WizardForm.StatusLabel.Caption := 'Installing SQL Server Express (this may take several minutes)...';

  Params :=
    '-NoProfile -ExecutionPolicy Bypass -File "' + Ps1Path + '"' +
    ' -BootstrapperPath "' + BootstrapperPath + '"' +
    ' -InstanceName "{#SqlExpressInstance}"' +
    ' -LogFile "' + LogPath + '"';

  if not Exec('powershell.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    FailInstall('Install-SqlExpress.ps1 failed to start.');
    Result := False;
    Exit;
  end;

  AppendInstallSummary('Install-SqlExpress.ps1 exit code: ' + IntToStr(ResultCode));
  if ResultCode <> 0 then
  begin
    FailInstall('SQL Server Express installation failed. See Install-SqlExpress.log.');
    Result := False;
    Exit;
  end;

  SqlServer := InstanceNameToServer('{#SqlExpressInstance}');
  AppendInstallSummary('SQL Server Express ready at ' + SqlServer + '.');
end;

function RunConfigureServer: Boolean;
var
  Ps1Path, LogPath, Params: String;
  ResultCode: Integer;
  CreateDbFlag: String;
begin
  Result := True;
  Ps1Path := ExpandConstant('{app}\tools\Configure-Server.ps1');
  LogPath := InstallerLogDir + '\Configure-Server.log';

  if not FileExists(Ps1Path) then
  begin
    FailInstall('Configure-Server.ps1 not found.');
    Result := False;
    Exit;
  end;

  if WizardIsTaskSelected('createdatabase') then
    CreateDbFlag := ' -CreateDatabase'
  else
    CreateDbFlag := '';

  Params :=
    '-NoProfile -ExecutionPolicy Bypass -File "' + Ps1Path + '"' +
    ' -InstallDir "' + ExpandConstant('{app}') + '"' +
    ' -SqlServer "' + SqlServer + '"' +
    ' -SqlDatabase "' + SqlDatabase + '"' +
    ' -Port ' + IntToStr(HttpPort) +
    ' -ServiceAccount "' + ServiceAccount + '"' +
    CreateDbFlag +
    ' -LogFile "' + LogPath + '"';

  if WizardForm <> nil then
    WizardForm.StatusLabel.Caption := 'Configuring database and Windows Service...';

  if not Exec('powershell.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    FailInstall('Configure-Server.ps1 failed to start.');
    Result := False;
    Exit;
  end;

  AppendInstallSummary('Configure-Server.ps1 exit code: ' + IntToStr(ResultCode));
  if ResultCode <> 0 then
  begin
    FailInstall('Server configuration failed (exit code ' + IntToStr(ResultCode) + '). See Configure-Server.log.');
    Result := False;
  end;
end;

procedure AddFirewallRule;
var
  ResultCode: Integer;
  RuleName: String;
begin
  if not WizardIsTaskSelected('firewall') then
    Exit;

  RuleName := 'Aganox LMS HTTP ' + IntToStr(HttpPort);
  Exec('netsh.exe',
    'advfirewall firewall add rule name="' + RuleName + '" dir=in action=allow protocol=TCP localport=' + IntToStr(HttpPort),
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  AppendInstallSummary('Firewall rule exit code: ' + IntToStr(ResultCode));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    EnsureInstallerLogDir;
    AppendInstallSummary('Post-install started at ' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
    AppendInstallSummary('SQL Server target: ' + SqlServer + '  Database: ' + SqlDatabase + '  Port: ' + IntToStr(HttpPort));
    AppendInstallSummary('InstallSqlExpress=' + BoolToStr(InstallSqlExpress) + '  SqlInstancesFound=' + BoolToStr(SqlInstancesFound));

    if not RunInstallSqlExpress then
    begin
      if not WizardSilent() then
        MsgBox(InstallFailureMessage, mbError, MB_OK);
      Abort;
    end;

    if not RunConfigureServer then
    begin
      if not WizardSilent() then
        MsgBox(InstallFailureMessage + #13#10#13#10 + 'Logs: ' + InstallerLogDir, mbError, MB_OK);
      Abort;
    end;

    AddFirewallRule;
    AppendInstallSummary('Post-install completed successfully.');
  end;
end;

procedure DeinitializeSetup();
begin
  EnsureInstallerLogDir;
  if ExpandConstant('{log}') <> '' then
    CopyFile(ExpandConstant('{log}'), InstallerLogDir + '\Setup.log', False);
end;

function BoolToStr(Value: Boolean): String;
begin
  if Value then
    Result := 'True'
  else
    Result := 'False';
end;
