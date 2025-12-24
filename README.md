# Vocabulary Test App (词汇测试应用)

这是一个基于 Blazor Server 的词汇测试与评分应用，集成了 Coze 工作流以实现 AI 驱动的手写识别功能。应用针对移动端进行了优化，支持多端自适应布局。

## 功能特性

*   **词汇测试**：支持英译中和中译英的交互式测试。
*   **自动评分**：上传手写答题纸照片，利用 AI（Coze 工作流）自动进行评分。
*   **人工评分**：提供手动核对和评分的备用模式。
*   **统计分析**：通过详细的历史记录和正确率图表追踪学习进度。
*   **回顾复习**：查看过往的试卷和识别结果。
*   **移动端适配**：
    *   **响应式布局**：针对手机屏幕深度优化，包括统计卡片、配置表单、测试界面及**试卷评阅页面**（自动堆叠布局）。
    *   **表格优化**：所有长表格（历史记录、错误率分析、评阅明细）均支持横向滚动与列固定，防止小屏内容溢出。
    *   **交互优化**：按钮与操作栏在移动端自动调整间距与对齐方式，提升操作体验。
*   **后台管理**：
    *   **单词管理**：支持单词的增删改查（CRUD），具备严格的数据校验（重复 ID、输入格式限制）。
    *   **批量操作**：支持 Excel/CSV 批量导入及带确认的批量删除功能。
    *   **便捷交互**：支持按 ID、中英文快速搜索，优化了移动端与桌面端的表格布局。

## 环境要求

- 使用安装包：无需 .NET SDK
- 从源码运行：.NET 9.0 SDK
- Coze 的 ApiToken 与 WorkflowId（用于自动评分）

## 安装包使用

1. 准备 Coze 资源  
   在安装程序所在目录，找到提供的 zip 资源包，先导入到 Coze 空间（https://www.coze.cn/space/），创建工作流并拿到 `WorkflowId`。

2. 运行安装程序  
   双击 `Installer/Output/VocabularyTestApp_Setup.exe` 按向导完成安装：
   - 管理员配置：设置用于后台登录的账号与密码（路径 ` /admin/login`）
   - Coze 配置：填写 `ApiToken` 与 `WorkflowId`（均为必填）
   - 网络配置：设置端口（默认 `5267`；如启用 HTTPS，则使用 `端口+1`）
   - 安装选项：可选启用 HTTPS、允许局域网访问（添加防火墙规则）、随登录自启

3. 安装完成提示  
   完成后弹窗会显示后台登录地址，例如：
   - 仅 HTTP：`http://localhost:5267/admin/login`
   - HTTP + HTTPS：同时显示 `https://localhost:5268/admin/login`

4. 快捷方式与启动参数  
   桌面与开始菜单快捷方式会携带 `--urls` 参数，形如：
   ```
   VocabularyTestApp.exe --urls "http://0.0.0.0:5267;https://0.0.0.0:5268"
   ```

5. 敏感配置  
   安装过程会自动生成 `appsettings.Secret.json` 并写入管理员与 Coze 配置；该文件位于安装目录。

## 从源码运行

1. 克隆仓库
   ```bash
   git clone https://github.com/lucky126/VocabularyTestApp.git
   cd VocabularyTestApp
   ```

2. 本地配置（源码模式）  
   在项目根目录创建 `appsettings.Secret.json`：
   ```json
   {
     "Coze": {
       "ApiToken": "你的_真实_API_TOKEN",
       "WorkflowId": "你的_真实_WORKFLOW_ID"
     },
     "Admin": {
       "Username": "admin",
       "Password": "至少6位密码"
     }
   }
   ```
   该文件已被 Git 忽略。

3. 运行
   - 开发模式：
     ```bash
     dotnet run
     ```
   - 热重载：
     ```bash
     dotnet watch run
     ```
   - 局域网访问（推荐）：
     ```bash
     dotnet run --urls "http://0.0.0.0:5267"
     ```
     在手机浏览器访问：`http://<你的局域网IP>:5267`

## 使用说明

- 配置测试：选择单词数量和测试模式（英→中 或 中→英）
- 进行测试：书写答案并标注题号
- 评分：
  - 自动：上传答题纸照片，调用 Coze 工作流识别并评分
  - 人工：手动核对与评分
- 统计：查看历史成绩与识别明细

## 常见问题（FAQ）

- 端口被占用
  - 安装向导中更换端口，或启动参数 `--urls` 修改端口
  - 检查占用：`netstat -ano | findstr :5267`，结束对应进程后重试

- 局域网访问失败
  - 安装时勾选“允许局域网访问”，系统将添加防火墙入站规则
  - 访问地址格式：`http://<你的电脑IP>:<端口>`
  - 若仍失败，手动添加规则或关闭第三方安全软件拦截

- HTTPS 无法启动
  - 安装 .NET 开发证书并信任：`dotnet dev-certs https --trust`
  - 或在安装选项中取消启用 HTTPS，仅使用 HTTP

- 后台登录不了
  - 登录路径：`/admin/login`
  - 管理员用户名与密码由安装向导设置；忘记可编辑安装目录中的 `appsettings.Secret.json` 后重启

- Coze 接口报错（401/参数错误）
  - 确认已将安装目录旁提供的 zip 资源导入到 Coze 并创建工作流
  - 检查 `ApiToken` 与 `WorkflowId` 是否填写正确（可在 `appsettings.Secret.json` 中更新后重启）

- 修改启动地址
  - 桌面/开始菜单快捷方式包含 `--urls` 参数；可在属性中修改
  - 自动启动项位置：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`（值名 `VocabularyTestApp`）

- 卸载后防火墙规则未删除
  - 手动执行：`netsh advfirewall firewall delete rule name="VocabularyTestApp"`

## 许可证

[MIT](LICENSE)
