; Inno Setup script for VocabularyTestApp
[Setup]
AppName=VocabularyTestApp
AppVersion=1.0.0
DefaultDirName={localappdata}\VocabularyTestApp
DefaultGroupName=VocabularyTestApp
OutputBaseFilename=VocabularyTestApp_Setup
Compression=lzma
SolidCompression=yes
DisableDirPage=no
DisableProgramGroupPage=no

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\bin\Release\net9.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\VocabularyTestApp"; Filename: "{app}\VocabularyTestApp.exe"; Parameters: "--urls ""{code:GetUrls}"""; WorkingDir: "{app}"
Name: "{commondesktop}\VocabularyTestApp"; Filename: "{app}\VocabularyTestApp.exe"; Parameters: "--urls ""{code:GetUrls}"""; WorkingDir: "{app}"

[Run]
Filename: "{app}\VocabularyTestApp.exe"; Description: "启动 VocabularyTestApp"; Flags: nowait postinstall skipifsilent; Parameters: "--urls ""{code:GetUrls}"""

[UninstallRun]
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""VocabularyTestApp"""; Flags: runhidden

[Code]
var
  AdminQ: TInputQueryWizardPage;
  CozeQ: TInputQueryWizardPage;
  NetQ: TInputQueryWizardPage;
  CozeInfo: TWizardPage;
  InfoText: TNewStaticText;
  OptPage: TWizardPage;
  OptInfo: TNewStaticText;
  EnableHttpsChk: TNewCheckBox;
  LanAccessChk: TNewCheckBox;
  AutoStartChk: TNewCheckBox;
 
function EscapeJson(S: String): String;
var
  i: Integer;
  ch: Char;
  R: String;
begin
  R := '';
  for i := 1 to Length(S) do
  begin
    ch := S[i];
    if ch = '\' then
      R := R + '\\'
    else if ch = '"' then
      R := R + '\"'
    else if ch = #13 then
      R := R + '\r'
    else if ch = #10 then
      R := R + '\n'
    else
      R := R + ch;
  end;
  Result := R;
end;

procedure InitializeWizard;
begin
  AdminQ := CreateInputQueryPage(wpSelectDir, '管理员配置', '', '');
  AdminQ.Add('后台管理员用户名（用于登录 /admin/login）', False);
  AdminQ.Add('后台管理员密码（用于登录 /admin/login）', False);
  AdminQ.Values[0] := 'admin';
  AdminQ.Values[1] := '';

  CozeInfo := CreateCustomPage(AdminQ.ID, 'Coze 资源导入说明', '请优先完成 Coze 工作流准备');
  InfoText := TNewStaticText.Create(CozeInfo);
  InfoText.Parent := CozeInfo.Surface;
  InfoText.AutoSize := False;
  InfoText.Width := CozeInfo.SurfaceWidth;
  InfoText.Height := 140;
  InfoText.Left := 0;
  InfoText.Top := 0;
  InfoText.WordWrap := True;
  InfoText.Caption :=
    '请先到当前目录下的zip文件导入到 Coze 平(https://www.coze.cn/space/)的资源库，创建工作流并获取 WorkflowId。' + #13#10 +   
    '完成后再继续安装并填写 WorkflowId。';

  CozeQ := CreateInputQueryPage(CozeInfo.ID, 'Coze 配置', '用于 Coze 接口访问与工作流调用', '请填写 Coze 的 ApiToken 与 WorkflowId');
  CozeQ.Add('Coze ApiToken', False);
  CozeQ.Add('Coze WorkflowId', False);
  CozeQ.Values[0] := '';
  CozeQ.Values[1] := '';

  NetQ := CreateInputQueryPage(CozeQ.ID, '网络配置', '', '');
  NetQ.Add('端口', False);
  NetQ.Values[0] := '5267';

  OptPage := CreateCustomPage(NetQ.ID, '安装选项', '选择安装后的行为');
  OptInfo := TNewStaticText.Create(OptPage);
  OptInfo.Parent := OptPage.Surface;
  OptInfo.AutoSize := False;
  OptInfo.Width := OptPage.SurfaceWidth;
  OptInfo.Height := 96;
  OptInfo.Left := 0;
  OptInfo.Top := 0;
  OptInfo.WordWrap := True;
  OptInfo.Caption :=
    '启用 HTTPS：绑定额外端口为 HTTP+1。' + #13#10 +
    '允许局域网访问：添加防火墙入站规则。' + #13#10 +
    '随登录自启：将程序加入当前用户启动项。';
  EnableHttpsChk := TNewCheckBox.Create(OptPage);
  EnableHttpsChk.Parent := OptPage.Surface;
  EnableHttpsChk.Caption := '启用 HTTPS';
  EnableHttpsChk.Checked := False;
  EnableHttpsChk.Left := 0;
  EnableHttpsChk.Top := OptInfo.Top + OptInfo.Height + 8;
  EnableHttpsChk.Width := OptPage.SurfaceWidth;
  LanAccessChk := TNewCheckBox.Create(OptPage);
  LanAccessChk.Parent := OptPage.Surface;
  LanAccessChk.Caption := '允许局域网访问';
  LanAccessChk.Checked := False;
  LanAccessChk.Left := 0;
  LanAccessChk.Top := EnableHttpsChk.Top + 24;
  LanAccessChk.Width := OptPage.SurfaceWidth;
  AutoStartChk := TNewCheckBox.Create(OptPage);
  AutoStartChk.Parent := OptPage.Surface;
  AutoStartChk.Caption := '随登录自启';
  AutoStartChk.Checked := False;
  AutoStartChk.Left := 0;
  AutoStartChk.Top := LanAccessChk.Top + 24;
  AutoStartChk.Width := OptPage.SurfaceWidth;
end;

function GetUrls(Param: string): string;
var
  port: string;
  pnum: Integer;
begin
  port := Trim(NetQ.Values[0]);
  if port = '' then port := '5267';
  Result := 'http://0.0.0.0:' + port;
  pnum := StrToIntDef(port, 5267);
  if EnableHttpsChk.Checked then
    Result := Result + ';https://0.0.0.0:' + IntToStr(pnum + 1);
end;

function ValidateAdminPage(): Boolean;
var
  u, p: string;
begin
  u := Trim(AdminQ.Values[0]);
  p := Trim(AdminQ.Values[1]);
  Result := True;
  if u = '' then
  begin
    MsgBox('请填写后台管理员用户名（用于登录 /admin/login）。', mbError, MB_OK);
    Result := False;
    exit;
  end;
  if Length(p) < 6 then
  begin
    MsgBox('后台管理员密码至少 6 位。', mbError, MB_OK);
    Result := False;
    exit;
  end;
end;

function ValidateCozePage(): Boolean;
var
  t, w: string;
begin
  t := Trim(CozeQ.Values[0]);
  w := Trim(CozeQ.Values[1]);
  Result := True;
  if (t = '') or (w = '') then
  begin
    MsgBox('请填写 Coze 的 ApiToken 与 WorkflowId（用于调用工作流）。', mbError, MB_OK);
    Result := False;
    exit;
  end;
end;

function ValidateNetPage(): Boolean;
var
  port: string;
  pnum: Integer;
begin
  port := Trim(NetQ.Values[0]);
  pnum := StrToIntDef(port, 0);
  Result := True;
  if (pnum <= 0) or (pnum > 65535) then
  begin
    MsgBox('端口号不合法。', mbError, MB_OK);
    Result := False;
    exit;
  end;
end;

function GenerateSecretConfig(): Boolean;
var
  f, json: string;
begin
  json :=
    '{' + #13#10 +
    '  "Coze": {' + #13#10 +
    '    "ApiToken": "' + EscapeJson(CozeQ.Values[0]) + '",' + #13#10 +
    '    "WorkflowId": "' + EscapeJson(CozeQ.Values[1]) + '"' + #13#10 +
    '  },' + #13#10 +
    '  "Admin": {' + #13#10 +
    '    "Username": "' + EscapeJson(AdminQ.Values[0]) + '",' + #13#10 +
    '    "Password": "' + EscapeJson(AdminQ.Values[1]) + '"' + #13#10 +
    '  },' + #13#10 +
    '  "AllowedHosts": "*"' + #13#10 +
    '}';
  f := ExpandConstant('{app}\appsettings.Secret.json');
  Result := SaveStringToFile(f, json, False);
end;

procedure ConfigureFirewall();
var
  rc: Integer;
  port: string;
begin
  if LanAccessChk.Checked then
  begin
    port := Trim(NetQ.Values[0]);
    Exec('netsh', 'advfirewall firewall add rule name="VocabularyTestApp" dir=in action=allow protocol=TCP localport=' + port, '', SW_HIDE, ewWaitUntilTerminated, rc);
  end;
end;

procedure ConfigureStartup();
var
  urls, cmd: string;
begin
  if AutoStartChk.Checked then
  begin
    urls := GetUrls('');
    cmd := '"' + ExpandConstant('{app}\VocabularyTestApp.exe') + '" --urls "' + urls + '"';
    RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'VocabularyTestApp', cmd);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = AdminQ.ID then
    Result := ValidateAdminPage()
  else if CurPageID = CozeQ.ID then
    Result := ValidateCozePage()
  else if CurPageID = NetQ.ID then
    Result := ValidateNetPage();
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  urls: string;
  rc: Integer;
  port: string;
  pnum: Integer;
  baseHttp: string;
  baseHttps: string;
begin
  if CurStep = ssPostInstall then
  begin
    if not GenerateSecretConfig() then
      MsgBox('写入 appsettings.Secret.json 失败。', mbError, MB_OK);
    ConfigureFirewall();
    ConfigureStartup();
    urls := GetUrls('');
    port := Trim(NetQ.Values[0]);
    pnum := StrToIntDef(port, 5267);
    baseHttp := 'http://localhost:' + port;
    baseHttps := 'https://localhost:' + IntToStr(pnum + 1);
    if EnableHttpsChk.Checked then
      MsgBox('安装完成。后台登录地址：' + #13#10 + baseHttp + '/admin/login' + #13#10 + baseHttps + '/admin/login', mbInformation, MB_OK)
    else
      MsgBox('安装完成。后台登录地址：' + #13#10 + baseHttp + '/admin/login', mbInformation, MB_OK);
  end;
end;

