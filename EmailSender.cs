using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LinkedInGen
{
    /// <summary>
    /// Handles email sending functionality for LinkedIn post notifications.
    /// Reads email configuration from appsettings.json and sends generated post content via email.
    /// Supports sending both plain emails and emails with inline image attachments.
    /// Automatically converts markdown-formatted text to HTML for better email display.
    /// </summary>
    public class EmailSender
    {
        /// <summary>
        /// Gets the email address of the recipient.
        /// </summary>
        public string RecipientEmail { get; }

        /// <summary>
        /// Initializes a new instance of the EmailSender class with a specified recipient email address.
        /// </summary>
        /// <param name="recipientEmail">The email address to send posts to.</param>
        /// <exception cref="ArgumentNullException">Thrown when recipientEmail is null.</exception>
        public EmailSender(string recipientEmail)
        {
            RecipientEmail = recipientEmail ?? throw new ArgumentNullException(nameof(recipientEmail));
        }

        /// <summary>
        /// Sends an email with an HTML body and an attached image.
        /// The image is included as an inline attachment and referenced in the HTML.
        /// </summary>
        /// <param name="subject">The email subject line.</param>
        /// <param name="htmlBody">The HTML body of the email.</param>
        /// <param name="imagePath">The file path to the image to be attached.</param>
        /// <returns>A task representing the asynchronous email sending operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when email settings are missing in appsettings.json.</exception>
        /// <exception cref="Exception">Rethrows any exceptions that occur during the email sending process.</exception>
        public async Task SendEmailWithImageAsync(string subject, string htmlBody, string imagePath)
        {
            try
            {
                // Load email configuration from appsettings.json
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);

                var configuration = configBuilder.Build();

                string smtpServer = configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
                string senderEmail = configuration["Email:SenderEmail"];
                string senderPassword = configuration["Email:SenderPassword"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("Email settings not found in appsettings.json");
                }

                // Convert markdown content to HTML properly (simple approach)
                htmlBody = FormatContentAsHtml(htmlBody);

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(RecipientEmail);

                // Add image as attachment if available
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    // Add image inline
                    var imageAttachment = new Attachment(imagePath);
                    string contentId = "PostImage";
                    imageAttachment.ContentId = contentId;
                    imageAttachment.ContentDisposition.Inline = true;
                    imageAttachment.ContentDisposition.DispositionType = "inline";
                    message.Attachments.Add(imageAttachment);

                    // Reference the inline image in HTML - look for closing paragraph or just append
                    if (htmlBody.Contains("</p>"))
                    {
                        htmlBody = htmlBody.Replace("</p>", $"</p><br/><img src=\"cid:{contentId}\" style=\"max-width:100%\" />");
                    }
                    else
                    {
                        htmlBody += $"<br/><img src=\"cid:{contentId}\" style=\"max-width:100%\" />";
                    }
                    message.Body = htmlBody;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends an email with an HTML body without any attachments.
        /// Uses SMTP settings from appsettings.json to send the email.
        /// </summary>
        /// <param name="subject">The email subject line.</param>
        /// <param name="htmlBody">The HTML body of the email.</param>
        /// <returns>A task representing the asynchronous email sending operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when email settings are missing in appsettings.json.</exception>
        /// <exception cref="Exception">Rethrows any exceptions that occur during the email sending process.</exception>
        public async Task SendEmailAsync(string subject, string htmlBody)
        {
            try
            {
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);

                var configuration = configBuilder.Build();

                string smtpServer = configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
                string senderEmail = configuration["Email:SenderEmail"];
                string senderPassword = configuration["Email:SenderPassword"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("Email settings not found in appsettings.json");
                }

                // Convert markdown content to HTML properly
                htmlBody = FormatContentAsHtml(htmlBody);

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(RecipientEmail);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts plain text or markdown-style content to basic HTML for email display.
        /// Handles line breaks, headings, bold/italic formatting, and converts LinkedIn hashtags to clickable links.
        /// If the content is already HTML (contains &lt;html&gt; or &lt;body&gt; tags), it will be returned as is.
        /// </summary>
        /// <param name="content">The content to format as HTML.</param>
        /// <returns>The HTML-formatted content, or the original content if it's null, empty, or already HTML.</returns>
        private string FormatContentAsHtml(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // If content isn't already HTML, convert line breaks to <br> tags
            if (!content.Contains("<html") && !content.Contains("<body"))
            {
                // Replace line breaks with HTML breaks
                content = content.Replace("\n", "<br/>");

                // Basic handling for markdown-style formatting
                content = System.Text.RegularExpressions.Regex.Replace(content, @"^\s*#{1,6}\s+(.+)$", "<h2>$1</h2>", System.Text.RegularExpressions.RegexOptions.Multiline);
                content = System.Text.RegularExpressions.Regex.Replace(content, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
                content = System.Text.RegularExpressions.Regex.Replace(content, @"\*([^*]+)\*", "<em>$1</em>");

                // Hashtags to links
                content = System.Text.RegularExpressions.Regex.Replace(content, @"#(\w+)", "<a href=\"https://www.linkedin.com/feed/hashtag/$1\">#$1</a>");

                // Wrap in HTML if needed
                if (!content.StartsWith("<"))
                {
                    content = $"<p>{content}</p>";
                }
            }

            return content;
        }
    }
}