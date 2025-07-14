# LinkedInGen

LinkedInGen is a tool that generates personalized LinkedIn posts in your own writing style using AI. It analyzes your LinkedIn data export (including your profile, articles, and shares) to create authentic-sounding content based on topics you provide.

## Features

- **Profile Analysis**: Processes your LinkedIn profile export to understand your professional background and writing style
- **Voice Capture**: Creates a Semantic Kernel plugin that mimics your writing style
- **Post Generation**: Creates LinkedIn posts on topics you specify that sound authentically like you wrote them
- **Image Generation**: Creates relevant images to accompany your posts using DALL-E 3
- **Email Delivery**: Sends the generated posts to your email address
- **Automation Support**: Can be set up as a cron job to generate posts automatically

## Prerequisites

- **.NET 8.0 or higher**
- **LinkedIn Data Export** (instructions below)
- **Azure OpenAI API access** with GPT-4 and DALL-E 3 deployments
- **Gmail account** with an app password (for email functionality)

## Setup Instructions

### 1. Export Your LinkedIn Data

1. Log in to LinkedIn
2. Go to your profile > Settings & Privacy > Data Privacy > How LinkedIn uses your data
3. Click "Get a copy of your data"
4. Request an archive of your data (select all data)
5. LinkedIn will email you when your data is ready to download (usually takes a few hours to a day)
6. Download and extract the archive to a directory called `LinkedInProfile` in the project root

### 2. Configure Your Azure OpenAI Access

1. Create an Azure OpenAI resource if you don't have one already
2. Deploy GPT-4 (or similar model) and DALL-E 3 models
3. Note your endpoint URL and API key
4. Create an `appsettings.json` file in the project root with the following content:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "your-gpt-deployment-name",
    "ImageDeploymentName": "dall-e-3"
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

### 3. Set Up Gmail App Password

1. Go to your Google Account > Security
2. Enable 2-Step Verification if not already enabled
3. Go to "App passwords"
4. Select "Mail" and your device
5. Generate and copy the 16-character password to use in your `appsettings.json`

### 4. Create Topics File

Create a file named `topics.md` in the project root with topics for post generation:

```markdown
# LinkedIn Post Topics

**TOPIC** The case for implementing data standards like CIM or IFC across utility organizations - breaking data silos for better interoperability

**TOPIC** Why simple software solutions win in the utility industry - the dangers of over-engineering and over-architecting from day one

**TOPIC** Comparing IFC and CIM standards - could we adapt building information modeling approaches for high-voltage electrical infrastructure?

**TOPIC** The real value of AI isn't replacing engineers - it's automating the mundane tasks that slow us down
```

The program will process topics marked with `**TOPIC**` and mark them as `**USED**` after generating posts for them.

## Usage

### Interactive Mode

Run the program without arguments to use the interactive menu:

```bash
dotnet run
```

### Command Line Mode

Generate a post on a specific topic:

```bash
dotnet run post "Your topic here"
```

Without a specific topic, the program will use the next available topic from your `topics.md` file:

```bash
dotnet run post
```

### Automated Daily Posts

Set up a cron job to generate posts automatically. For example, to run daily at 8:20 AM:

```bash
crontab -e
```

Add the line:

```
20 8 * * * cd /home/shawn/Projects/LinkedInGen && dotnet run post >> /home/shawn/Projects/LinkedInGen/cron.log 2>&1
```

## Output

- Generated posts are saved to `NEW POSTS.md` for your review
- Images are saved to the `POST_IMAGES` directory
- Generated posts and images are emailed to your configured email address

## Using a Different LLM Provider

This project is configured for Azure OpenAI by default. If you want to use a different provider:

1. Modify the `PostGenerator.cs` file to use your preferred LLM API
2. Update the configuration structure in `appsettings.json` accordingly
3. If using the image generation feature, you'll need to update that code as well

## Directory Structure

- `LinkedInProfile/` - Place your LinkedIn data export here
- `PLUGINS/` - Generated Semantic Kernel plugins will be stored here
- `POST_IMAGES/` - Generated images for posts
- `NEW POSTS.md` - Output file with all generated posts

## Troubleshooting

- **LinkedIn Data Format**: If your LinkedIn export format changes, you may need to update the parsing logic in `PluginGenerator.cs`
- **API Limits**: Be mindful of Azure OpenAI rate limits, especially when running in batch mode
- **Email Issues**: If emails aren't being sent, check your app password and email settings
