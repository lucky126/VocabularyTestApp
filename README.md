# Vocabulary Test App (词汇测试应用)

这是一个基于 Blazor Server 的词汇测试与评分应用，集成了 Coze 工作流以实现 AI 驱动的手写识别功能。

## 功能特性

*   **词汇测试**：支持英译中和中译英的交互式测试。
*   **自动评分**：上传手写答题纸照片，利用 AI（Coze 工作流）自动进行评分。
*   **人工评分**：提供手动核对和评分的备用模式。
*   **统计分析**：通过详细的历史记录和正确率图表追踪学习进度。
*   **回顾复习**：查看过往的试卷和识别结果。

## 环境要求

*   .NET 7.0 SDK
*   Coze API Token 和 Workflow ID（用于自动评分功能）

## 安装与设置

1.  **克隆仓库**
    ```bash
    git clone https://github.com/lucky126/VocabularyTestApp.git
    cd VocabularyTestApp
    ```

2.  **配置**
    应用需要 Coze API 凭证才能正常工作。这些敏感信息不应提交到版本控制中。

    在项目根目录（`appsettings.json` 旁边）创建一个名为 `appsettings.Secret.json` 的文件，内容如下：

    ```json
    {
      "Coze": {
        "ApiToken": "你的_真实_API_TOKEN",
        "WorkflowId": "你的_真实_WORKFLOW_ID"
      }
    }
    ```

    *注意：`appsettings.Secret.json` 已配置为被 Git 忽略。*

3.  **运行应用**
    ```bash
    dotnet run
    ```
    或者使用热重载模式：
    ```bash
    dotnet watch run
    ```

4.  **访问应用**
    打开浏览器并访问 `https://localhost:7196`（或控制台中显示的 URL）。

## 使用说明

1.  **配置测试**：选择单词数量和测试模式（英->中 或 中->英）。
2.  **进行测试**：在纸上写下你的答案，并在题号前标注 1 到 N。
3.  **评分**：
    *   **自动**：点击相机图标上传答题纸照片。AI 将识别文本并与正确答案比对进行评分。
    *   **人工**：手动标记答案正确与否。
4.  **统计**：查看历史成绩并回顾过往试卷。

## 许可证

[MIT](LICENSE)
