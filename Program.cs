namespace LinkedInGen
{
    /// <summary>
    /// Main entry point class for the LinkedInGen application.
    /// This application analyzes LinkedIn profile data and generates posts in the user's voice.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments. If provided, application runs in non-interactive mode.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task Main(string[] args)
        {
            // Process command line arguments if provided
            if (args.Length > 0)
            {
                await ProcessCommandLineArgsAsync(args);
                return;
            }

            // Interactive mode when no command line arguments are provided
            Console.WriteLine("Welcome to LinkedInGen!");

            bool exitProgram = false;
            while (!exitProgram)
            {
                Console.WriteLine("\nPlease select an option:");
                Console.WriteLine("1. Generate LinkedIn Voice Plugin from Profile Data");
                Console.WriteLine("2. Generate LinkedIn Post using Existing Plugin");
                Console.WriteLine("3. Exit");
                Console.Write("\nYour choice (1-3): ");

                string choice = Console.ReadLine()?.Trim() ?? "";

                try
                {
                    switch (choice)
                    {
                        case "1":
                            var pluginGenerator = new PluginGenerator();
                            await pluginGenerator.GeneratePluginAsync();
                            break;
                        case "2":
                            var postGenerator = new PostGenerator();
                            await postGenerator.GeneratePostAsync();
                            break;
                        case "3":
                            exitProgram = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Thank you for using LinkedInGen!");
        }

        /// <summary>
        /// Processes command line arguments for non-interactive mode operation.
        /// Supports the 'post' command to generate LinkedIn posts with or without a specified topic.
        /// </summary>
        /// <param name="args">Command line arguments array.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task ProcessCommandLineArgsAsync(string[] args)
        {
            if (args[0].ToLower() == "post")
            {
                string topic;
                if (args.Length > 1)
                {
                    // Topic provided directly as an argument
                    topic = string.Join(" ", args, 1, args.Length - 1);
                }
                else
                {
                    // No topic provided, get one from the topics file
                    topic = await TopicManager.GetNextTopicAsync();
                    if (string.IsNullOrEmpty(topic))
                    {
                        Console.WriteLine("No topic available. Please provide a topic or add topics to topics.md.");
                        return;
                    }
                }

                var emailSender = new EmailSender("shawn.o.weekly@gmail.com");
                var postGenerator = new PostGenerator();

                try
                {
                    // Generate post in non-interactive mode
                    string post = await postGenerator.GeneratePostNonInteractiveAsync(topic);

                    // Generate an image for the post
                    string imagePath = await postGenerator.GenerateImageForPostAsync(post, topic);

                    // Save post to markdown file
                    await postGenerator.SavePostToMarkdownFileAsync(topic, post, imagePath);

                    // Send email with the image
                    string emailBody = $"<h2>LinkedIn Post</h2><p><strong>Topic:</strong> {topic}</p><p>{post}</p>";

                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        await emailSender.SendEmailWithImageAsync("LinkedIn Post Generated", emailBody, imagePath);
                    }
                    else
                    {
                        await emailSender.SendEmailAsync("LinkedIn Post Generated", emailBody);
                    }

                    Console.WriteLine($"Post generated and email sent to {emailSender.RecipientEmail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating post: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Usage: LinkedInGen post [\"your topic here\"]");
                Console.WriteLine("Example: LinkedInGen post \"The future of AI in software development\"");
                Console.WriteLine("If no topic is provided, one will be taken from topics.md");
            }
        }
    }
}