# Vocabulary Test App

A Blazor Server application for vocabulary testing and grading, featuring AI-powered handwriting recognition via Coze workflow integration.

## Features

*   **Vocabulary Testing**: Interactive testing for English-to-Chinese and Chinese-to-English translation.
*   **Auto Grading**: Upload photos of your handwritten answer sheet for automatic grading using AI (Coze Workflow).
*   **Manual Grading**: Fallback mode for manual verification and grading.
*   **Statistics**: Track your progress with detailed history and accuracy charts.
*   **Review**: View past test papers and recognition results.

## Prerequisites

*   .NET 7.0 SDK
*   A Coze API Token and Workflow ID (for auto-grading features)

## Setup

1.  **Clone the repository**
    ```bash
    git clone https://github.com/lucky126/VocabularyTestApp.git
    cd VocabularyTestApp
    ```

2.  **Configuration**
    The application requires Coze API credentials to function correctly. These secrets should not be committed to version control.

    Create a file named `appsettings.Secret.json` in the root of the project (next to `appsettings.json`) with the following content:

    ```json
    {
      "Coze": {
        "ApiToken": "YOUR_ACTUAL_API_TOKEN",
        "WorkflowId": "YOUR_ACTUAL_WORKFLOW_ID"
      }
    }
    ```

    *Note: `appsettings.Secret.json` is configured to be ignored by Git.*

3.  **Run the application**
    ```bash
    dotnet run
    ```
    Or using hot reload:
    ```bash
    dotnet watch run
    ```

4.  **Access the app**
    Open your browser and navigate to `https://localhost:7196` (or the URL displayed in the console).

## Usage

1.  **Config Test**: Choose the number of words and the test mode (En->Cn or Cn->En).
2.  **Take Test**: Write down your answers on a piece of paper numbered 1 to N.
3.  **Grading**:
    *   **Auto**: Click the camera icon to upload a photo of your answer sheet. The AI will recognize the text and grade it against the correct answers.
    *   **Manual**: Manually mark answers as correct or incorrect.
4.  **Stats**: View your historical performance and review past test papers.

## License

[MIT](LICENSE)
