; Aganox LMS — Windows installer (Inno Setup 6)
; Build: .\scripts\build-installer.ps1

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\publish\win-x64"
#endif

#define AppName "Aganox LMS"
#define AppVersion "1.0.0"
#define AppPublisher "Aganox"
#define AppExeName "Start-AganoxLms.cmd"

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

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "prerequisites\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsVcRedistInstall
Source: "prerequisites\SqlLocalDB.msi"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsLocalDbInstall

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
var
  InstallerLogDir: String;
  VcRedistExitCode: Integer;
  VcRedistAttempted: Boolean;
  LocalDbMsiExitCode: Integer;
  LocalDbMsiAttempted: Boolean;

function FindSqlLocalDbExe: String; forward;
function NeedsVcRedistInstall: Boolean; forward;
function NeedsLocalDbInstall: Boolean; forward;

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
  begin
    if not ForceDirectories(InstallerLogDir) then
    begin
      Log('Failed to create installer log directory: ' + InstallerLogDir);
      InstallerLogDir := '';
    end;
  end;
end;

procedure AppendInstallSummary(const Line: String);
var
  SummaryPath: String;
  Existing: AnsiString;
begin
  EnsureInstallerLogDir;
  if not InstallerLogDirReady then
  begin
    Log('[summary] ' + Line);
    Exit;
  end;

  SummaryPath := InstallerLogDir + '\install-summary.txt';
  Existing := '';
  if FileExists(SummaryPath) then
    LoadStringFromFile(SummaryPath, Existing);

  if Length(Existing) > 0 then
    Existing := Existing + #13#10;
  Existing := Existing + Line;

  if not SaveStringToFile(SummaryPath, Existing, False) then
    Log('Failed to write install summary: ' + SummaryPath);
end;

function CopyInstallerLog(const SourcePath, DestFileName: String): Boolean;
var
  DestPath: String;
begin
  Result := False;
  if SourcePath = '' then
    Exit;
  if not FileExists(SourcePath) then
  begin
    Log('Log file not found: ' + SourcePath);
    Exit;
  end;

  EnsureInstallerLogDir;
  if not InstallerLogDirReady then
    Exit;

  DestPath := InstallerLogDir + '\' + DestFileName;
  Result := CopyFile(SourcePath, DestPath, False);
  if Result then
    Log('Saved installer log: ' + DestPath)
  else
    Log('Failed to copy log from ' + SourcePath + ' to ' + DestPath);
end;

procedure PersistSetupLog;
var
  SetupLogPath: String;
begin
  SetupLogPath := ExpandConstant('{log}');
  if SetupLogPath = '' then
  begin
    Log('Setup log path is unavailable (SetupLogging may be disabled).');
    Exit;
  end;

  CopyInstallerLog(SetupLogPath, 'Setup.log');
end;

function VcRedistExitCodeDescription(ExitCode: Integer): String;
begin
  case ExitCode of
    0: Result := 'success';
    1638: Result := 'newer version already installed';
    3010: Result := 'success; restart required to complete installation';
    else Result := 'see VcRedist-Install.log for details';
  end;
end;

function IsVcRedistInstalled: Boolean;
var
  Installed: Cardinal;
  Version: String;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and (Installed = 1) then
  begin
    Result := True;
    if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Version', Version) then
      Log('Visual C++ Redistributable x64 detected: ' + Version);
    Exit;
  end;

  if IsWin64 and RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and (Installed = 1) then
  begin
    Result := True;
    if RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Version', Version) then
      Log('Visual C++ Redistributable x64 detected (WOW6432Node): ' + Version);
  end;
end;

function NeedsVcRedistInstall: Boolean;
begin
  Result := not IsVcRedistInstalled;
end;

function InstallVcRedist: Boolean;
var
  ExePath: String;
  LogPath: String;
  ResultCode: Integer;
  Params: String;
begin
  Result := True;
  VcRedistAttempted := True;
  VcRedistExitCode := -1;

  ExePath := ExpandConstant('{tmp}\vc_redist.x64.exe');
  LogPath := ExpandConstant('{tmp}\VcRedist-Install.log');

  AppendInstallSummary('Visual C++ Redistributable install started at ' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
  Log('Installing Visual C++ 2015-2022 Redistributable (x64) from ' + ExePath);

  if not FileExists(ExePath) then
  begin
    VcRedistExitCode := -2;
    Log('vc_redist.x64.exe not found at ' + ExePath);
    AppendInstallSummary('ERROR: vc_redist.x64.exe not found in temporary folder.');
    Result := False;
    Exit;
  end;

  Params := '/install /quiet /norestart /log "' + LogPath + '"';

  if WizardForm <> nil then
    WizardForm.StatusLabel.Caption := 'Installing Visual C++ Redistributable...';

  if not Exec(ExePath, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    VcRedistExitCode := -3;
    Log('vc_redist.x64.exe failed to start.');
    AppendInstallSummary('ERROR: vc_redist.x64.exe failed to start.');
    Result := False;
  end
  else
  begin
    VcRedistExitCode := ResultCode;
    Log('vc_redist.x64.exe exit code: ' + IntToStr(ResultCode) + ' (' + VcRedistExitCodeDescription(ResultCode) + ').');
    AppendInstallSummary('vc_redist.x64.exe exit code: ' + IntToStr(ResultCode) + ' (' + VcRedistExitCodeDescription(ResultCode) + ').');
    if (ResultCode <> 0) and (ResultCode <> 1638) and (ResultCode <> 3010) then
      Result := False;
  end;

  if FileExists(LogPath) then
    CopyInstallerLog(LogPath, 'VcRedist-Install.log')
  else
  begin
    Log('Visual C++ install log was not created: ' + LogPath);
    AppendInstallSummary('WARNING: VcRedist-Install.log was not created.');
  end;
end;

function MsiExecExitCodeDescription(ExitCode: Integer): String;
begin
  case ExitCode of
    0: Result := 'success';
    1602: Result := 'installation canceled by user';
    1603: Result := 'fatal error during installation';
    1618: Result := 'another installation is already in progress';
    1619: Result := 'installation package could not be opened';
    1620: Result := 'installation package could not be opened (invalid)';
    1638: Result := 'another version of this product is already installed';
    3010: Result := 'success; restart required to complete installation';
    else Result := 'see SqlLocalDB-Install.log for details';
  end;
end;

procedure LogMsiFailureHints(const MsiLogPath: String);
var
  Content: AnsiString;
  ContentStr: String;
begin
  if not FileExists(MsiLogPath) then
    Exit;

  if not LoadStringFromFile(MsiLogPath, Content) then
    Exit;

  ContentStr := String(Content);
  if Pos('Return value 3', ContentStr) > 0 then
  begin
    Log('SqlLocalDB-Install.log indicates MSI failure (Return value 3).');
    AppendInstallSummary('MSI failure detected: Return value 3 in SqlLocalDB-Install.log.');
  end;

  if Pos('Installation success or error status: 1603', ContentStr) > 0 then
  begin
    Log('SqlLocalDB-Install.log reports Windows Installer error 1603.');
    AppendInstallSummary('Windows Installer error 1603 reported in SqlLocalDB-Install.log.');
  end;

  if Pos('Product: Microsoft SQL Server', ContentStr) > 0 then
    AppendInstallSummary('See SqlLocalDB-Install.log for full MSI verbose output.');
end;

function InstallLocalDbMsi: Boolean;
var
  MsiPath: String;
  MsiLogPath: String;
  ResultCode: Integer;
  Params: String;
begin
  Result := True;
  LocalDbMsiAttempted := True;
  LocalDbMsiExitCode := -1;

  MsiPath := ExpandConstant('{tmp}\SqlLocalDB.msi');
  MsiLogPath := ExpandConstant('{tmp}\SqlLocalDB-Install.log');

  AppendInstallSummary('SQL Server LocalDB MSI install started at ' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
  Log('Installing SQL Server LocalDB from ' + MsiPath);

  if not FileExists(MsiPath) then
  begin
    LocalDbMsiExitCode := -2;
    Log('SqlLocalDB.msi not found at ' + MsiPath);
    AppendInstallSummary('ERROR: SqlLocalDB.msi not found in temporary folder.');
    Result := False;
    Exit;
  end;

  Params := '/i "' + MsiPath + '" /qn IACCEPTSQLLOCALDBLICENSETERMS=YES /L*v "' + MsiLogPath + '"';

  if WizardForm <> nil then
    WizardForm.StatusLabel.Caption := 'Installing SQL Server LocalDB...';

  if not Exec('msiexec.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    LocalDbMsiExitCode := -3;
    Log('msiexec.exe failed to start for SqlLocalDB.msi.');
    AppendInstallSummary('ERROR: msiexec.exe failed to start.');
    Result := False;
  end
  else
  begin
    LocalDbMsiExitCode := ResultCode;
    Log('SqlLocalDB.msi msiexec exit code: ' + IntToStr(ResultCode) + ' (' + MsiExecExitCodeDescription(ResultCode) + ').');
    AppendInstallSummary('SqlLocalDB.msi exit code: ' + IntToStr(ResultCode) + ' (' + MsiExecExitCodeDescription(ResultCode) + ').');
    if (ResultCode <> 0) and (ResultCode <> 3010) then
      Result := False;
  end;

  if FileExists(MsiLogPath) then
  begin
    CopyInstallerLog(MsiLogPath, 'SqlLocalDB-Install.log');
    if not Result then
      LogMsiFailureHints(MsiLogPath);
  end
  else
  begin
    Log('SqlLocalDB verbose install log was not created: ' + MsiLogPath);
    AppendInstallSummary('WARNING: SqlLocalDB-Install.log was not created.');
  end;
end;

procedure LogLocalDbDiagnostics;
var
  SqlLocalDb: String;
  Versions: TArrayOfString;
  I: Integer;
begin
  SqlLocalDb := FindSqlLocalDbExe;
  if SqlLocalDb <> '' then
  begin
    Log('SqlLocalDB.exe: ' + SqlLocalDb);
    AppendInstallSummary('SqlLocalDB.exe: ' + SqlLocalDb);
  end
  else
  begin
    Log('SqlLocalDB.exe: not found');
    AppendInstallSummary('SqlLocalDB.exe: not found');
  end;

  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions') then
  begin
    if RegGetSubkeyNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions', Versions) then
    begin
      for I := 0 to GetArrayLength(Versions) - 1 do
      begin
        Log('LocalDB registry version: ' + Versions[I]);
        AppendInstallSummary('LocalDB registry version: ' + Versions[I]);
      end;
    end;
  end
  else
  begin
    Log('LocalDB registry key not found under HKLM.');
    AppendInstallSummary('LocalDB registry key not found under HKLM.');
  end;
end;

function FindSqlLocalDbExe: String;
begin
  Result := '';
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := ExpandConstant('{pf}\Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe');
    Exit;
  end;
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe');
end;

function LocalDbRegistryInstalled: Boolean;
var
  Versions: TArrayOfString;
begin
  Result := False;
  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions') then
  begin
    if RegGetSubkeyNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions', Versions) then
    begin
      Result := GetArrayLength(Versions) > 0;
      Exit;
    end;
  end;

  if IsWin64 and RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions') then
  begin
    if RegGetSubkeyNames(HKLM32, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions', Versions) then
    begin
      Result := GetArrayLength(Versions) > 0;
    end;
  end;
end;

function IsLocalDbInstalled: Boolean;
begin
  Result := (FindSqlLocalDbExe <> '') or LocalDbRegistryInstalled;
end;

function NeedsLocalDbInstall: Boolean;
begin
  Result := not IsLocalDbInstalled;
end;

function EnsureLocalDbInstance: Boolean;
var
  SqlLocalDb: String;
  ResultCode: Integer;
begin
  Result := True;
  SqlLocalDb := FindSqlLocalDbExe;
  if SqlLocalDb = '' then
  begin
    Log('SqlLocalDB.exe not found after prerequisite step.');
    AppendInstallSummary('ERROR: SqlLocalDB.exe not found after prerequisite step.');
    Result := False;
    Exit;
  end;

  if Exec(SqlLocalDb, 'info MSSQLLOCALDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
  begin
    Log('LocalDB instance MSSQLLOCALDB is available.');
    AppendInstallSummary('LocalDB instance MSSQLLOCALDB is available.');
    Exit;
  end;

  Log('Creating LocalDB instance MSSQLLOCALDB...');
  AppendInstallSummary('Creating LocalDB instance MSSQLLOCALDB...');
  if not Exec(SqlLocalDb, 'create MSSQLLOCALDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    Log('Failed to create MSSQLLOCALDB (exit code ' + IntToStr(ResultCode) + ').');
    AppendInstallSummary('ERROR: Failed to create MSSQLLOCALDB (exit code ' + IntToStr(ResultCode) + ').');
    Result := False;
    Exit;
  end;

  if Exec(SqlLocalDb, 'start MSSQLLOCALDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Started MSSQLLOCALDB (exit code ' + IntToStr(ResultCode) + ').');
    AppendInstallSummary('Started MSSQLLOCALDB (exit code ' + IntToStr(ResultCode) + ').');
  end
  else
  begin
    Log('Could not start MSSQLLOCALDB; it will start on first API connection.');
    AppendInstallSummary('WARNING: Could not start MSSQLLOCALDB; it will start on first API connection.');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  LogDirMsg: String;
  LocalDbReady: Boolean;
begin
  if CurStep = ssPostInstall then
  begin
    EnsureInstallerLogDir;
    AppendInstallSummary('Aganox LMS installation post-install started at ' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
    Log('Aganox LMS installed to ' + ExpandConstant('{app}'));
    AppendInstallSummary('Application directory: ' + ExpandConstant('{app}'));

    if NeedsVcRedistInstall then
    begin
      if not InstallVcRedist then
        Log('Visual C++ Redistributable installation failed.');
    end
    else
    begin
      AppendInstallSummary('Visual C++ Redistributable already installed; setup step skipped.');
      Log('Visual C++ Redistributable already installed; skipping setup.');
    end;

    if NeedsLocalDbInstall then
    begin
      if not InstallLocalDbMsi then
        Log('SQL Server LocalDB MSI installation failed.');
    end
    else
    begin
      AppendInstallSummary('SQL Server LocalDB already installed; MSI step skipped.');
      Log('SQL Server LocalDB already installed; skipping MSI install.');
    end;

    LogLocalDbDiagnostics;
    LocalDbReady := IsLocalDbInstalled;

    if LocalDbReady then
    begin
      if not EnsureLocalDbInstance then
      begin
        Log('Warning: LocalDB is installed but MSSQLLOCALDB could not be verified.');
        AppendInstallSummary('WARNING: LocalDB is installed but MSSQLLOCALDB could not be verified.');
      end;
    end
    else
    begin
      Log('Warning: SQL Server LocalDB is not installed after prerequisite step.');
      AppendInstallSummary('ERROR: SQL Server LocalDB is not installed after prerequisite step.');
      if LocalDbMsiAttempted then
        AppendInstallSummary('Review VcRedist-Install.log, SqlLocalDB-Install.log, and Setup.log in the installer logs folder.');

      if InstallerLogDirReady then
        LogDirMsg := InstallerLogDir
      else
        LogDirMsg := ExpandConstant('{tmp}');

      Log('Detailed installer logs: ' + LogDirMsg);
      AppendInstallSummary('Detailed installer logs folder: ' + LogDirMsg);

      if not WizardSilent() then
        MsgBox(
          'Aganox LMS requires SQL Server LocalDB, but it could not be installed automatically.' + #13#10 + #13#10 +
          'Detailed installation logs were saved to:' + #13#10 +
          LogDirMsg + #13#10 + #13#10 +
          'Review install-summary.txt, VcRedist-Install.log, SqlLocalDB-Install.log, and Setup.log, then install prerequisites manually if needed.',
          mbError, MB_OK);
    end;

    Log('On first launch the API applies migrations and seeds default data.');
    AppendInstallSummary('Post-install completed at ' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
    PersistSetupLog;
  end;
end;

procedure DeinitializeSetup();
begin
  EnsureInstallerLogDir;
  PersistSetupLog;
end;
