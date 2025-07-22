# LinkedInGen

LinkedInGen is a .NET console application that helps you generate LinkedIn posts in your own voice using AI. It analyzes your LinkedIn profile data and creates Semantic Kernel plugins to mimic your writing style. You can then generate posts on topics you provide, revise them interactively, and automatically generate relevant images.

## Features

- **Profile Analysis**: Select CSV files from your LinkedIn data export to analyze your writing style.
- **Plugin Generation**: Save prompts as `skprompt.txt` plugins for use with Semantic Kernel.
- **Post Generation**: Enter a topic and details, then generate a LinkedIn post in your voice. Optionally revise the post in a loop.
- **Image Generation**: Automatically create a relevant image for each post using Azure OpenAI's image API.
- **Markdown Output**: Save generated posts and images to `NEW POSTS.md`.
- **Email Delivery**: Send generated posts and images to your email address.
- **Automation**: Supports cron jobs for scheduled post generation.

## Getting Started

### Prerequisites

- **.NET 8.0 or higher**
- **Azure OpenAI API access** (GPT-4 and GPT-IMAGE-1 deployments (*you have to request access to gpt-image-1*))
  - **NOTE**: Semantic Kernel supports almost all of the LLMs/API availalbe to developers, not just Azure. If you want to
  use other LLM providers (like Gemini or a locally hosted one) just alter the code accordingly; I didn't take the time to make it more configurable.
- **Gmail account** with an app password (for email delivery)

### Setup

1. **Export Your LinkedIn Data**
   - Download your LinkedIn data export (CSV format).
   - Place the files in a directory of your choice.

2. **Configure Azure OpenAI**
   - Create an Azure OpenAI resource and deploy GPT-4 and GPT-Image-1 or DALL-E 3.
   - Copy your endpoint URLs and API keys.

3. **Configure the Application**
   - Create an `appsettings.json` file in the project root with your Azure OpenAI and email settings:
     ```json
     {
       "AzureOpenAI": {
         "Endpoint": "https://your-resource-name.openai.azure.com/",
         "ApiKey": "your-api-key-here",
         "DeploymentName": "your-gpt-deployment-name"
       },
       "AzureOpenAIImage": {
         "Endpoint": "https://your-image-resource-name.cognitiveservices.azure.com/",
         "Deployment": "your-image-deployment-name",
         "ApiVersion": "your-targeted-API-version",
         "ApiKey": "your-image-api-key-here"
       },
       "Email": {
         "SmtpServer": "smtp.gmail.com",
         "SmtpPort": 587,
         "SenderEmail": "your-email@gmail.com",
         "SenderPassword": "your-gmail-app-password",
         "EnableSSL": true
       }
     }
     ```

4. **Set Up Gmail App Password**
   - Enable 2-Step Verification in your Google Account.
   - Generate an app password for Gmail and use it in `appsettings.json`.

5. **Create Topics File**
   - Add a `topics.md` file in the project root with your post topics.

### Usage

- **Interactive Mode**:
  Run the program and follow prompts to select LinkedIn CSV files, generate plugins, and create posts.
  ```bash
  dotnet run
  ```

- **Command Line Mode**:
  Generate a post on a specific topic:
  ```bash
  dotnet run post "Your topic here"
  ```
  Or use the next topic from `topics.md`:
  ```bash
  dotnet run post
  ```
    - Your topic.md file should have 1 to N lines that look like this:
    ```
    **TOPIC** Digital Transformation in Utilities

    **TOPIC** The Role of Open Standards in Infrastructure

    **TOPIC** Balancing Innovation and Stability in Engineering
    ```
    Your topics should have enough informaiton to generate a good post, 2 or 3 good sentenaces that provide a solid topic is enough.  When a topic is used, it will be marked as such.

- **Automated Mode**:
  Set up a cron job to run the program automatically and log output:
  ```
  45 8 * * 1-5 cd /home/projectLocation/LinkedInGen && dotnet run post >> logs/cron.log 2>&1
  ```

### Output

- Posts and images are saved to `NEW POSTS.md` and `POST_IMAGES/`.
- Emails are sent to your configured address.

## Directory Structure

- `PLUGINS/` - Semantic Kernel plugins (`skprompt.txt`)
- `POST_IMAGES/` - Generated images
- `NEW POSTS.md` - Markdown file with all posts
- `topics.md` - List of post topics

## Notes

- Keep your `appsettings.json` secure; it contains API keys and passwords.
  - HINT: take advantage of ``` dotnet user-secrets ```
- The code is designed to be as simple and maintainable as possible.
- For troubleshooting, check the console output and log files.

---

For questions or contributions, open an issue
