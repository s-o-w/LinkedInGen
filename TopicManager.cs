using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LinkedInGen
{
    /// <summary>
    /// Manages topics for LinkedIn post generation.
    /// Reads from and updates a topics.md file to track which topics have been used.
    /// </summary>
    public class TopicManager
    {
        /// <summary>
        /// Path to the file containing LinkedIn post topics.
        /// </summary>
        private const string TopicsFilePath = "topics.md";
        
        /// <summary>
        /// Gets the next unused topic from the topics file and marks it as used.
        /// </summary>
        /// <returns>
        /// A task containing the next unused topic as a string, or null if no unused topics are available
        /// or if an error occurs.
        /// </returns>
        public static async Task<string?> GetNextTopicAsync()
        {
            // Check if topics file exists
            if (!File.Exists(TopicsFilePath))
            {
                Console.WriteLine($"Topics file not found: {TopicsFilePath}");
                return null;
            }
            
            try
            {
                // Read the entire file
                string content = await File.ReadAllTextAsync(TopicsFilePath);
                
                // Find unprocessed topics
                var matches = Regex.Matches(content, @"\*\*TOPIC\*\*\s*([^\n]+)");
                foreach (Match match in matches)
                {
                    string topic = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(topic))
                    {
                        // Mark this topic as processed by changing **TOPIC** to **USED**
                        string updatedContent = content.Replace(match.Value, match.Value.Replace("**TOPIC**", "**USED**"));
                        await File.WriteAllTextAsync(TopicsFilePath, updatedContent);
                        
                        return topic;
                    }
                }
                
                Console.WriteLine("No unprocessed topics found in the topics file.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing topics file: {ex.Message}");
                return null;
            }
        }
    }
}