using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace LinkedInGen
{
    /// <summary>
    /// Handles the generation of LinkedIn posts using the Semantic Kernel plugin.
    /// Includes functionality for interactive post creation, image generation, and saving posts.
    /// </summary>
    public class PostGenerator
    {
        /// <summary>
        /// The default path where the LinkedIn Voice plugin is located.
        /// </summary>
        private const string DefaultPluginPath = "PLUGINS/LinkedInVoice";

        /// <summary>
        /// Interactively generates a LinkedIn post based on user input.
        /// Guides the user through providing a topic and details, generating a post,
        /// and offering options for revision.
        /// </summary>
        /// <returns>A task representing the asynchronous post generation operation.</returns>
        public async Task GeneratePostAsync()
        {
            // 1. Load configuration - make sure to use the correct path
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration;
            try
            {
                configuration = configBuilder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading appsettings.json: {ex.Message}");
                Console.WriteLine("Make sure the appsettings.json file exists and is properly formatted.");
                return;
            }

            string endpoint = configuration["AzureOpenAI:Endpoint"];
            string apiKey = configuration["AzureOpenAI:ApiKey"];
            string deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            // Check if config values are missing and prompt if needed
            if (string.IsNullOrEmpty(endpoint))
            {
                Console.WriteLine("Azure OpenAI Endpoint not found in appsettings.json. Please enter it:");
                endpoint = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(endpoint))
                {
                    Console.WriteLine("Endpoint is required. Operation cancelled.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Azure OpenAI API Key not found in appsettings.json. Please enter it:");
                apiKey = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("API Key is required. Operation cancelled.");
                    return;
                }
            }

            // 2. Try to use the default plugin directory first
            string pluginDirectory = DefaultPluginPath;
            if (!Directory.Exists(pluginDirectory) || !File.Exists(Path.Combine(pluginDirectory, "skprompt.txt")))
            {
                // Default plugin not found, prompt user
                Console.WriteLine($"Default plugin not found at {DefaultPluginPath}");
                pluginDirectory = PromptForPluginDirectory();
                if (string.IsNullOrEmpty(pluginDirectory))
                {
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Using plugin at: {pluginDirectory}");
            }

            try
            {
                // 3. Create the kernel with Azure OpenAI
                var builder = Kernel.CreateBuilder();
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey);
                var kernel = builder.Build();

                // 4. Read the prompt template directly
                string promptFilePath = Path.Combine(pluginDirectory, "skprompt.txt");
                string promptTemplate = await File.ReadAllTextAsync(promptFilePath);

                // Create config.json file if it doesn't exist
                string configFilePath = Path.Combine(pluginDirectory, "config.json");
                if (!File.Exists(configFilePath))
                {
                    await File.WriteAllTextAsync(configFilePath, @"{
                    ""schema"": 1,
                    ""type"": ""completion"",
                    ""description"": ""Generates LinkedIn posts in the user's voice"",
                    ""completion"": {
                        ""max_tokens"": 1000,
                        ""temperature"": 0.7
                    }
                    }");
                }

                // Import the plugin more directly - use the directory name as plugin name
                string pluginName = Path.GetFileName(pluginDirectory);
                var linkedInPlugin = kernel.CreateFunctionFromPrompt(
                    promptTemplate,
                    new PromptExecutionSettings
                    {
                        ExtensionData = new Dictionary<string, object> {
                            { "max_tokens", 1000 },
                            { "temperature", 0.7 }
                        }
                    });

                // 5. Prompt for post topic and details
                Console.WriteLine("\nWhat would you like to post about? Please provide a topic and any specific details:");
                string postTopic = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(postTopic))
                {
                    Console.WriteLine("Post topic cannot be empty. Operation cancelled.");
                    return;
                }

                // 6. Generate the post using the plugin
                bool continueEditing = true;
                string generatedPost = await GeneratePostWithFunctionAsync(kernel, linkedInPlugin, postTopic);

                while (continueEditing && !string.IsNullOrEmpty(generatedPost))
                {
                    // Display the generated post
                    Console.WriteLine("\n=== Generated LinkedIn Post ===\n");
                    Console.WriteLine(generatedPost);
                    Console.WriteLine("\n===============================\n");

                    // Ask if user wants to revise
                    Console.WriteLine("Would you like to revise this post?");
                    Console.WriteLine("1. Yes, with specific changes");
                    Console.WriteLine("2. Yes, regenerate completely");
                    Console.WriteLine("3. No, I'm satisfied with this post");
                    Console.Write("\nYour choice (1-3): ");

                    string revisionChoice = Console.ReadLine()?.Trim() ?? "";

                    switch (revisionChoice)
                    {
                        case "1":
                            Console.WriteLine("\nPlease describe what changes you'd like to make:");
                            string revisionRequest = Console.ReadLine()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(revisionRequest))
                            {
                                string revisedPrompt = $"Original post: {generatedPost}\n\nPlease revise this LinkedIn post with the following changes: {revisionRequest}";
                                generatedPost = await GeneratePostWithFunctionAsync(kernel, linkedInPlugin, revisedPrompt);
                            }
                            break;

                        case "2":
                            Console.WriteLine("\nPlease provide a new topic or more specific details:");
                            string newTopic = Console.ReadLine()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(newTopic))
                            {
                                generatedPost = await GeneratePostWithFunctionAsync(kernel, linkedInPlugin, newTopic);
                            }
                            break;

                        case "3":
                            continueEditing = false;
                            Console.WriteLine("\nGreat! Your LinkedIn post is ready to use.");

                            // Generate an image for the post before saving
                            Console.WriteLine("\nGenerating an image for your post...");
                            string imagePath = await GenerateImageForPostAsync(generatedPost, postTopic);
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                Console.WriteLine($"Image generated successfully at: {imagePath}");
                                // Save the post with the generated image
                                await SavePostToMarkdownFileAsync(postTopic, generatedPost, imagePath);
                            }
                            else
                            {
                                Console.WriteLine("Could not generate an image for your post.");
                                // Save the post without an image
                                await SavePostToMarkdownFileAsync(postTopic, generatedPost);
                            }
                            break;
                            

                        default:
                            Console.WriteLine("\nInvalid option. Continuing with current post.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating LinkedIn post: {ex.Message}");
            }
        }


        /// <summary>
        /// Generates a LinkedIn post in non-interactive mode using a provided topic.
        /// Used for automated post generation without user interaction.
        /// </summary>
        /// <param name="topic">The topic to generate a post about.</param>
        /// <returns>A task containing the generated LinkedIn post text.</returns>
        public async Task<string> GeneratePostNonInteractiveAsync(string topic)
        {
            // 1. Load configuration - make sure to use the correct path
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration;
            try
            {
                configuration = configBuilder.Build();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading appsettings.json: {ex.Message}");
            }

            string endpoint = configuration["AzureOpenAI:Endpoint"];
            string apiKey = configuration["AzureOpenAI:ApiKey"];
            string deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            // Check if config values are missing
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException("Azure OpenAI Endpoint not found in appsettings.json");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("Azure OpenAI API Key not found in appsettings.json");
            }

            // Use the default plugin directory
            string pluginDirectory = DefaultPluginPath;
            if (!Directory.Exists(pluginDirectory) || !File.Exists(Path.Combine(pluginDirectory, "skprompt.txt")))
            {
                throw new DirectoryNotFoundException($"Default plugin not found at {DefaultPluginPath}");
            }

            // Create the kernel with Azure OpenAI
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey);
            var kernel = builder.Build();

            // Read the prompt template directly
            string promptFilePath = Path.Combine(pluginDirectory, "skprompt.txt");
            string promptTemplate = await File.ReadAllTextAsync(promptFilePath);

            // Create config.json file if it doesn't exist
            string configFilePath = Path.Combine(pluginDirectory, "config.json");
            if (!File.Exists(configFilePath))
            {
                await File.WriteAllTextAsync(configFilePath, @"{
            ""schema"": 1,
            ""type"": ""completion"",
            ""description"": ""Generates LinkedIn posts in the user's voice"",
            ""completion"": {
                ""max_tokens"": 1000,
                ""temperature"": 0.7
            }
        }");
            }

            // Import the plugin directly
            var linkedInPlugin = kernel.CreateFunctionFromPrompt(
                promptTemplate,
                new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object> {
                { "max_tokens", 1000 },
                { "temperature", 0.7 }
                    }
                });

            // Generate the post
            return await GeneratePostWithFunctionAsync(kernel, linkedInPlugin, topic);
        }


        /// <summary>
        /// Saves a generated LinkedIn post to a markdown file for future reference.
        /// Includes the topic, content, and optional image reference.
        /// </summary>
        /// <param name="topic">The topic of the LinkedIn post.</param>
        /// <param name="content">The generated post content.</param>
        /// <param name="imagePath">Optional path to an associated image for the post.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SavePostToMarkdownFileAsync(string topic, string content, string imagePath = null)
        {
            try
            {
                // Define the file path
                string filePath = "NEW POSTS.md";

                // Create the markdown content for the post
                var postMarkdown = new StringBuilder();

                // Add a horizontal line if the file already exists and has content
                if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                {
                    postMarkdown.AppendLine("\n\n---\n\n");
                }

                // Add post date and time
                postMarkdown.AppendLine($"## LinkedIn Post - {DateTime.Now:yyyy-MM-dd}");
                postMarkdown.AppendLine();

                // Add topic
                postMarkdown.AppendLine($"**Topic:** {topic}");
                postMarkdown.AppendLine();

                // Add image reference if available
                if (!string.IsNullOrEmpty(imagePath))
                {
                    // Get relative path for better portability
                    string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), imagePath);
                    postMarkdown.AppendLine($"**Image:** ![Post Image]({relativePath.Replace('\\', '/')})");
                    postMarkdown.AppendLine();
                }

                // Add content
                postMarkdown.AppendLine("**Content:**");
                postMarkdown.AppendLine();
                postMarkdown.AppendLine(content);

                // Append to file (or create if it doesn't exist)
                await File.AppendAllTextAsync(filePath, postMarkdown.ToString());

                Console.WriteLine($"Post saved to {Path.GetFullPath(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving post to markdown file: {ex.Message}");
            }
        }

        /// <summary>
        /// Prompts the user to enter a directory containing a LinkedIn Voice plugin.
        /// </summary>
        /// <returns>The valid plugin directory path, or an empty string if validation fails.</returns>
        private string PromptForPluginDirectory()
        {
            Console.WriteLine("Enter the directory containing the LinkedIn Voice plugin (with skprompt.txt):");
            string directory = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(directory))
            {
                Console.WriteLine("Directory cannot be empty. Operation cancelled.");
                return string.Empty;
            }

            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Directory does not exist. Operation cancelled.");
                return string.Empty;
            }

            string promptFile = Path.Combine(directory, "skprompt.txt");
            if (!File.Exists(promptFile))
            {
                Console.WriteLine("Could not find skprompt.txt in the specified directory. Operation cancelled.");
                return string.Empty;
            }

            return directory;
        }

        /// <summary>
        /// Generates a LinkedIn post by invoking the Semantic Kernel function with the provided input.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance to use for generation.</param>
        /// <param name="function">The Kernel function to invoke for post generation.</param>
        /// <param name="input">The input text describing the post topic or revision request.</param>
        /// <returns>A task containing the generated LinkedIn post text, cleaned of any prompt artifacts.</returns>
        /// <exception cref="Exception">Throws if there's an error calling the AI service.</exception>
        private async Task<string> GeneratePostWithFunctionAsync(Kernel kernel, KernelFunction function, string input)
        {
            Console.WriteLine("Generating post with AI...");

            try
            {
                // Call the function directly instead of trying to find it in a plugin
                var result = await kernel.InvokeAsync(function, new() { ["input"] = input });

                var generatedText = result.GetValue<string>() ?? "";

                // Clean up the result if needed
                return generatedText.Replace("[Write a LinkedIn post", "")
                                   .Replace("in the style and voice of the profile described above,", "")
                                   .Replace("focusing on the topic provided in the input]", "").Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates an image for a LinkedIn post based on its content.
        /// </summary>
        /// <param name="postContent">The content of the generated LinkedIn post.</param>
        /// <param name="topic">The topic of the post, used for naming the image file.</param>
        /// <returns>A task containing the path to the saved image file, or null if image generation failed.</returns>
        public async Task<string> GenerateImageForPostAsync(string postContent, string topic)
        {
            try
            {
                // Use the same configuration as your text generation
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddUserSecrets<PostGenerator>();

                var configuration = configBuilder.Build();

                string endpoint = configuration["AzureOpenAI:Endpoint"];
                string apiKey = configuration["AzureOpenAI:ApiKey"];
                string imageDeployment = configuration["AzureOpenAI:ImageDeploymentName"] ?? "dall-e-3";

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Azure OpenAI endpoint or API key not found in configuration.");
                    return null;
                }

                // Create a short prompt for DALL-E based on the post content
                string imagePrompt = GenerateImagePromptFromPost(postContent);

                // Call Azure OpenAI DALL-E API to generate the image
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("api-key", apiKey);

                // Updated API endpoint format for Azure OpenAI DALL-E
                var requestBody = new
                {
                    prompt = imagePrompt,
                    n = 1,
                    size = "1024x1024"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json");

                // Use the correct endpoint format for your Azure OpenAI deployment
                var response = await client.PostAsync(
                    $"{endpoint}/openai/deployments/{imageDeployment}/images/generations?api-version=2023-12-01-preview",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Image generation API call failed: {response.StatusCode}");
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    return null;
                }

                // Parse the response to get image URL directly
                var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                var imageUrl = responseData.GetProperty("data")[0].GetProperty("url").GetString();

                // Download the image to a POST_IMAGES directory
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Create POST_IMAGES directory if it doesn't exist
                    string imageDir = Path.Combine(Directory.GetCurrentDirectory(), "POST_IMAGES");
                    Directory.CreateDirectory(imageDir);

                    // Create a filename with date and topic snippet
                    string dateStamp = DateTime.Now.ToString("yyyy-MM-dd");
                    string topicSnippet = new string(topic.Take(30).Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                    string fileName = $"{dateStamp}_{topicSnippet}.png";
                    string imageFilePath = Path.Combine(imageDir, fileName);

                    using (var imageResponse = await client.GetAsync(imageUrl))
                    using (var imageStream = await imageResponse.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(imageFilePath))
                    {
                        await imageStream.CopyToAsync(fileStream);
                    }

                    Console.WriteLine($"Image saved to {imageFilePath}");
                    return imageFilePath;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an image generation prompt based on the LinkedIn post content.
        /// </summary>
        /// <param name="postContent">The content of the LinkedIn post.</param>
        /// <returns>A prompt string optimized for generating a relevant professional image with DALL-E.</returns>
        private string GenerateImagePromptFromPost(string postContent)
        {
            // Extract key concepts from the post to create a prompt for DALL-E
            string prompt = $"Create a professional hero image that represents the following post: {postContent}";

            // Add stylistic guidance to match professional LinkedIn aesthetic
            prompt += " The image should be clean, professional, with a modern tech feel. Include subtle visual metaphors related to utilities, energy, and/or software engineering. DO NOT ADD TEXT TO THE IMAGE!! Make it suitable as a social media post header.";

            return prompt;
        }

        /// <summary>
        /// Polls the Azure OpenAI API for the result of an asynchronous image generation operation.
        /// </summary>
        /// <param name="client">The HttpClient instance to use for API requests.</param>
        /// <param name="endpoint">The Azure OpenAI API endpoint URL.</param>
        /// <param name="operationId">The ID of the image generation operation to poll.</param>
        /// <returns>A task containing the URL of the generated image if successful, or null if failed or timed out.</returns>
        private async Task<string> PollForImageGenerationResult(HttpClient client, string endpoint, string operationId)
        {
            int maxAttempts = 30;
            int attempt = 0;
            int delayMs = 1000;

            while (attempt < maxAttempts)
            {
                attempt++;

                var response = await client.GetAsync($"{endpoint}/openai/operations/images/{operationId}?api-version=2023-06-01-preview");

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                    if (responseData.TryGetProperty("status", out var status) && status.GetString() == "succeeded")
                    {
                        // Get the image URL
                        var result = responseData.GetProperty("result");
                        var data = result.GetProperty("data")[0];
                        return data.GetProperty("url").GetString();
                    }
                    else if (responseData.TryGetProperty("status", out var failStatus) &&
                            (failStatus.GetString() == "failed" || failStatus.GetString() == "cancelled"))
                    {
                        Console.WriteLine("Image generation failed or was cancelled.");
                        return null;
                    }
                }

                await Task.Delay(delayMs);
                delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10 seconds
            }

            Console.WriteLine($"Timeout waiting for image generation after {maxAttempts} attempts.");
            return null;
        }
    }
}