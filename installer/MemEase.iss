#define MyAppName "MemEase"
#define MyAppVersion "1.0.0"
#define MyAppVerName "MemEase 1.0.0"
#define MyAppPublisher "MemEase"
#define MyAppExeName "MemEase.exe"
#define MyAppId "{{2169DE01-F090-404E-9040-C95491F02380}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppVerName}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) 2026 MemEase
AppComments=轻量内存托盘工具
VersionInfoVersion=1.0.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=1.0.0.0
VersionInfoOriginalFileName=MemEase-Setup-1.0.exe

DefaultDirName={localappdata}\Programs\MemEase
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
DisableDirPage=no
DisableWelcomePage=no
DisableReadyPage=no
DisableFinishedPage=no
UsePreviousAppDir=yes
UsePreviousTasks=yes
PrivilegesRequired=lowest
MinVersion=10.0.18362

LicenseFile=..\LICENSE.txt
OutputDir=..\dist
OutputBaseFilename=MemEase-Setup-1.0
SetupIconFile=..\assets\MemEase.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
LZMAUseSeparateProcess=yes
CloseApplications=no
RestartApplications=no
ChangesAssociations=no
ChangesEnvironment=no
WizardStyle=modern
WizardImageFile=assets\wizard-large.png
WizardSmallImageFile=assets\wizard-small.png
WizardImageStretch=yes
ShowLanguageDialog=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[CustomMessages]
chinesesimplified.CreateDesktopIcon=创建桌面快捷方式
chinesesimplified.AdditionalIcons=附加任务：
chinesesimplified.AutoStartApp=登录 Windows 时自动启动 MemEase
chinesesimplified.LaunchApp=启动 MemEase

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "autostart"; Description: "{cm:AutoStartApp}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "..\dist\MemEase.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\assets\icon-256.png"; DestDir: "{app}\assets"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\MemEase\MemEase"; Filename: "{app}\{#MyAppExeName}"; Comment: "轻量内存托盘工具"
Name: "{autoprograms}\MemEase\卸载 MemEase"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MemEase"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  DotNet48Release = 528040;

function IsDotNet48OrLater: Boolean;
var
  Release: Cardinal;
  RootKey: Integer;
begin
  if IsWin64 then
    RootKey := HKLM64
  else
    RootKey := HKLM;

  Result := RegQueryDWordValue(
    RootKey,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release',
    Release);

  if Result then
    Result := Release >= DotNet48Release;
end;

function InitializeSetup: Boolean;
begin
  Result := True;

  if not IsDotNet48OrLater then
  begin
    MsgBox(
      'MemEase 需要 .NET Framework 4.8 或更高版本。' + #13#10 + #13#10 +
      'Windows 10 1903 及以上版本通常已经自带。请安装 .NET Framework 4.8 后重试。',
      mbError,
      MB_OK);
    Result := False;
  end;
end;

procedure StopRunningApp;
var
  ResultCode: Integer;
begin
  Exec(
    ExpandConstant('{cmd}'),
    '/c taskkill /F /IM MemEase.exe >nul 2>&1',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
  Sleep(400);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopRunningApp;
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('autostart') then
    begin
      RegWriteStringValue(
        HKCU,
        RunKey,
        'MemEase',
        '"' + ExpandConstant('{app}\MemEase.exe') + '" --autostart');
    end
    else
    begin
      RegDeleteValue(HKCU, RunKey, 'MemEase');
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    WizardForm.FinishedLabel.Caption :=
      'MemEase 已安装完成。' + #13#10 + #13#10 +
      '程序会驻留在系统托盘。如果没有看到图标，请点击任务栏右下角的 ^ 展开。';
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    StopRunningApp;
  end
  else if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, RunKey, 'MemEase');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\MemEase');
  end;
end;
