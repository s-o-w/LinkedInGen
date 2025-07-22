using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace LinkedInGen
{
    /// <summary>
    /// Handles generation of Semantic Kernel plugins based on LinkedIn profile data.
    /// Analyzes CSV files exported from LinkedIn to create a personalized writing style profile.
    /// </summary>
    public class PluginGenerator
    {
        /// <summary>
        /// Main method to generate a LinkedIn voice plugin by analyzing LinkedIn CSV data.
        /// Guides the user through selecting files, processes the data, and creates a Semantic Kernel plugin.
        /// </summary>
        /// <returns>A task representing the asynchronous plugin generation operation.</returns>
        public async Task GeneratePluginAsync()
        {
            // 1. Prompt for directory containing LinkedIn CSV files
            string csvDirectory = PromptForDirectory("Enter the directory containing LinkedIn CSV files:");

            // 2. Get CSV files and let user select which ones to analyze
            var selectedFiles = SelectCsvFiles(csvDirectory);
            if (!selectedFiles.Any())
            {
                Console.WriteLine("No files selected. Exiting profile analysis.");
                return;
            }

            // 3. Prompt for output directory to save generated plugin
            string outputDirectory = PromptForDirectory("Enter the directory to save generated plugin:");

            // 4. Process all selected files to create a comprehensive profile
            await ProcessLinkedInProfileAsync(selectedFiles, outputDirectory);

            Console.WriteLine($"Successfully generated plugin in {outputDirectory}");
        }

        /// <summary>
        /// Prompts the user to enter a directory path and validates it.
        /// Creates the directory if it doesn't exist and the user confirms.
        /// </summary>
        /// <param name="message">The message to display to the user when prompting for input.</param>
        /// <returns>A valid directory path entered by the user.</returns>
        private string PromptForDirectory(string message)
        {
            while (true)
            {
                Console.WriteLine(message);
                string directory = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(directory))
                {
                    Console.WriteLine("Directory cannot be empty. Please try again.");
                    continue;
                }

                if (!Directory.Exists(directory))
                {
                    Console.WriteLine("Directory does not exist. Would you like to create it? (y/n)");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else
                    {
                        continue;
                    }
                }

                return directory;
            }
        }

        /// <summary>
        /// Displays available CSV files in a directory and allows the user to select which ones to analyze.
        /// </summary>
        /// <param name="directory">The directory path containing CSV files.</param>
        /// <returns>A list of full paths to the selected CSV files.</returns>
        private List<string> SelectCsvFiles(string directory)
        {
            var csvFiles = Directory.GetFiles(directory, "*.csv").ToList();

            if (!csvFiles.Any())
            {
                Console.WriteLine("No CSV files found in the specified directory.");
                return new List<string>();
            }

            Console.WriteLine("Available CSV files:");
            for (int i = 0; i < csvFiles.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(csvFiles[i])}");
            }

            Console.WriteLine("Enter the numbers of the files you want to analyze (comma-separated) or 'all' to select all files:");
            string input = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (input == "all")
            {
                return csvFiles;
            }

            var selectedIndices = input.Split(',')
                .Select(x => x.Trim())
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Where(i => i > 0 && i <= csvFiles.Count)
                .ToList();

            return selectedIndices.Select(i => csvFiles[i - 1]).ToList();
        }

        /// <summary>
        /// Processes LinkedIn profile data from multiple CSV files to create a comprehensive profile.
        /// Extracts profile details, experience, education, skills, and writing samples.
        /// Generates a Semantic Kernel plugin file with the compiled profile data.
        /// </summary>
        /// <param name="csvFiles">List of paths to LinkedIn CSV export files.</param>
        /// <param name="outputDirectory">Directory where the generated plugin will be saved.</param>
        /// <returns>A task representing the asynchronous profile processing operation.</returns>
        private async Task ProcessLinkedInProfileAsync(List<string> csvFiles, string outputDirectory)
        {
            Console.WriteLine("Analyzing LinkedIn profile data across multiple files...");

            // Combined profile data from all files
            var profileData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Extract basic profile information
                var profileFile = csvFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("Profile.csv"));
                if (profileFile != null)
                {
                    var basicProfile = ReadLinkedInProfileCsv(profileFile);
                    string fullName = $"{basicProfile.GetValueOrDefault("First Name", "")} {basicProfile.GetValueOrDefault("Last Name", "")}".Trim();
                    profileData["Name"] = fullName;
                    profileData["Headline"] = basicProfile.GetValueOrDefault("Headline", "");
                    profileData["Summary"] = basicProfile.GetValueOrDefault("Summary", "");
                }

                // Extract positions/experience
                var positionsFile = csvFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("Positions.csv"));
                if (positionsFile != null)
                {
                    var positions = ReadLinkedInCsvRows(positionsFile);
                    profileData["Experience"] = positions;
                }

                // Extract education
                var educationFile = csvFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("Education.csv"));
                if (educationFile != null)
                {
                    var education = ReadLinkedInCsvRows(educationFile);
                    profileData["Education"] = education;
                }

                // Extract skills
                var skillsFile = csvFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("Skills.csv"));
                if (skillsFile != null)
                {
                    var skills = ReadLinkedInCsvRows(skillsFile);
                    profileData["Skills"] = skills.Select(row => row.GetValueOrDefault("Name", "")).ToList();
                }

                // Extract sample messages/posts for writing style
                var sharesFile = csvFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("Shares.csv"));
                if (sharesFile != null)
                {
                    var shares = ReadLinkedInCsvRows(sharesFile);

                    // Sort by date (newest first) and take the 5 most recent shares
                    var recentShares = shares
                        .Where(s => !string.IsNullOrEmpty(s.GetValueOrDefault("ShareCommentary", "")))
                        .OrderByDescending(s => DateTime.TryParse(s.GetValueOrDefault("Date", ""), out var date) ? date : DateTime.MinValue)
                        .Take(15)
                        .ToList();

                    profileData["MessageSamples"] = recentShares;
                }


                // Generate plugin from combined profile data
                var pluginContent = GeneratePluginContent(profileData);

                // Create the output directory if needed
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Write the plugin file
                string pluginDirectory = Path.Combine(outputDirectory, "LinkedInVoice");
                if (!Directory.Exists(pluginDirectory))
                {
                    Directory.CreateDirectory(pluginDirectory);
                }

                string pluginFilePath = Path.Combine(pluginDirectory, "skprompt.txt");
                await File.WriteAllTextAsync(pluginFilePath, pluginContent);

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

                Console.WriteLine($"Successfully created LinkedIn voice plugin at {pluginFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing LinkedIn profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a LinkedIn profile CSV file and extracts its data into a dictionary.
        /// Designed to handle the format of LinkedIn's profile data exports.
        /// </summary>
        /// <param name="filePath">Path to the LinkedIn profile CSV file.</param>
        /// <returns>A dictionary containing the profile data with field names as keys.</returns>
        private Dictionary<string, string> ReadLinkedInProfileCsv(string filePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, // Ignore header validation
                    MissingFieldFound = null // Ignore missing fields
                });

                // Read the header row
                if (csv.Read())
                {
                    csv.ReadHeader();

                    // Read the first data row
                    if (csv.Read())
                    {
                        // Add all fields from the CSV to our dictionary
                        foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
                        {
                            try
                            {
                                string value = csv.GetField(header) ?? string.Empty;
                                result[header] = value;
                            }
                            catch
                            {
                                // Skip problematic fields
                                continue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Issue reading file {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Reads all rows from a LinkedIn CSV file into a list of dictionaries.
        /// Each row becomes a dictionary with column headers as keys.
        /// </summary>
        /// <param name="filePath">Path to the LinkedIn CSV file.</param>
        /// <returns>A list of dictionaries containing the data from each row of the CSV file.</returns>
        private List<Dictionary<string, string>> ReadLinkedInCsvRows(string filePath)
        {
            var results = new List<Dictionary<string, string>>();

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, // Ignore header validation
                    MissingFieldFound = null // Ignore missing fields
                });

                // Read the header row
                if (csv.Read())
                {
                    csv.ReadHeader();

                    // Read all data rows
                    while (csv.Read())
                    {
                        var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        // Add all fields from the CSV to our dictionary
                        foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
                        {
                            try
                            {
                                string value = csv.GetField(header) ?? string.Empty;
                                rowData[header] = value;
                            }
                            catch
                            {
                                // Skip problematic fields
                                continue;
                            }
                        }

                        results.Add(rowData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Issue reading file {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Generates the content for a Semantic Kernel plugin prompt file (skprompt.txt)
        /// based on the analyzed LinkedIn profile data.
        /// Creates a prompt that can be used to generate text in the user's writing style.
        /// </summary>
        /// <param name="profileData">Dictionary containing the combined LinkedIn profile data.</param>
        /// <returns>A string containing the generated plugin content.</returns>
        private string GeneratePluginContent(Dictionary<string, object> profileData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an AI assistant that writes LinkedIn posts in the style and voice of a specific person.");
            sb.AppendLine();
            sb.AppendLine("Profile Information:");

            // Add basic profile data
            sb.AppendLine($"Name: {profileData.GetValueOrDefault("Name", "Unknown")}");
            sb.AppendLine($"Headline: {profileData.GetValueOrDefault("Headline", "")}");
            sb.AppendLine($"Summary: {profileData.GetValueOrDefault("Summary", "")}");

            // Process experience/positions
            sb.AppendLine("\nWork Experience:");
            if (profileData.TryGetValue("Experience", out var expObj) && expObj is List<Dictionary<string, string>> experience)
            {
                foreach (var position in experience.Take(5)) // Take most recent positions
                {
                    string company = position.GetValueOrDefault("Company Name", "");
                    string title = position.GetValueOrDefault("Title", "");
                    string description = position.GetValueOrDefault("Description", "");
                    string startDate = position.GetValueOrDefault("Started On", "");
                    string endDate = position.GetValueOrDefault("Finished On", "Current");

                    if (string.IsNullOrEmpty(company) && string.IsNullOrEmpty(title))
                        continue;

                    sb.AppendLine($"- {title} at {company} ({startDate} - {endDate})");
                    if (!string.IsNullOrEmpty(description))
                    {
                        sb.AppendLine($"  {description.Replace("\n", " ").Substring(0, Math.Min(description.Length, 200))}...");
                    }
                }
            }

            // Process education
            sb.AppendLine("\nEducation:");
            if (profileData.TryGetValue("Education", out var eduObj) && eduObj is List<Dictionary<string, string>> education)
            {
                foreach (var edu in education)
                {
                    string school = edu.GetValueOrDefault("School Name", "");
                    string degree = edu.GetValueOrDefault("Degree Name", "");
                    string field = edu.GetValueOrDefault("Field of Study", "");

                    if (string.IsNullOrEmpty(school))
                        continue;

                    sb.AppendLine($"- {degree} {field} at {school}");
                }
            }

            // Process skills
            sb.AppendLine("\nSkills:");
            if (profileData.TryGetValue("Skills", out var skillsObj) && skillsObj is List<string> skills)
            {
                sb.AppendLine(string.Join(", ", skills.Take(20)));
            }

            // Add LinkedIn shares as writing samples
            sb.AppendLine("\nWriting Style Examples from Shares:");
            if (profileData.TryGetValue("MessageSamples", out var sharesObj) && sharesObj is List<Dictionary<string, string>> shares)
            {
                int exampleCount = 0;
                foreach (var share in shares)
                {
                    string content = share.GetValueOrDefault("ShareCommentary", "")?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(content) && content.Length > 30)
                    {
                        exampleCount++;
                        sb.AppendLine($"Share Example {exampleCount}:");
                        sb.AppendLine(content.Trim());
                        sb.AppendLine();

                        if (exampleCount >= 10) break; // 10 examples
                    }
                }
            }

            // Add paragraphs from LinkedIn articles as additional writing samples
            sb.AppendLine("\nWriting Style Examples from Articles:");
            string articlesPath = Path.Combine(Directory.GetCurrentDirectory(), "LinkedInProfile", "Articles", "Articles");
            if (Directory.Exists(articlesPath))
            {
                var articleFiles = Directory.GetFiles(articlesPath, "*.html");
                int articleCount = 0;

                foreach (var articleFile in articleFiles)
                {
                    try
                    {
                        string articleContent = File.ReadAllText(articleFile);
                        string articleTitle = Path.GetFileNameWithoutExtension(articleFile);

                        // Extract a paragraph from the article content
                        int divStart = articleContent.IndexOf("<div>");
                        if (divStart != -1)
                        {
                            int pStart = articleContent.IndexOf("<p>", divStart);
                            if (pStart != -1)
                            {
                                int pEnd = articleContent.IndexOf("</p>", pStart);
                                if (pEnd != -1)
                                {
                                    string paragraph = articleContent.Substring(pStart + 3, pEnd - pStart - 3);

                                    // Clean up HTML tags
                                    paragraph = paragraph
                                        .Replace("&nbsp;", " ")
                                        .Replace("<strong>", "")
                                        .Replace("</strong>", "")
                                        .Replace("<a href", "")
                                        .Replace("</a>", "")
                                        .Replace("<em>", "")
                                        .Replace("</em>", "")
                                        .Replace("<br>", " ")
                                        .Replace("<br/>", " ");

                                    // Remove remaining HTML tags
                                    paragraph = System.Text.RegularExpressions.Regex.Replace(paragraph, "<.*?>", "");

                                    articleCount++;
                                    sb.AppendLine($"Article Example {articleCount} (from {articleTitle}):");
                                    sb.AppendLine(paragraph.Trim());
                                    sb.AppendLine();

                                    if (articleCount >= 7) break; // 7 articles
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Issue reading article {Path.GetFileName(articleFile)}: {ex.Message}");
                        // Continue to the next article
                    }
                }
            }


            sb.AppendLine();
            sb.AppendLine("INPUT:");
            sb.AppendLine("{{$input}}");
            sb.AppendLine();
            sb.AppendLine("OUTPUT:");
            sb.AppendLine("[");
            sb.AppendLine("\nBased on the profile information above, when given a topic or subject, write a LinkedIn post that sounds authentically like this person would write it.");
            sb.AppendLine("- DO NOT USE EMDASHES!!! Use commas, periods, and other punctuation as appropriate. BUT DO NOT, I REPEAT, DO NOT USE EMDASHES!!!");
            sb.AppendLine("- Match their tone, style, and perspective based on their profile and writing samples.");
            sb.AppendLine("- Incorporate their professional expertise, experience, and interests into the content when relevant.");
            sb.AppendLine("- Use appropriate emojis and/or emoticons but no more than 2-3 per post.");
            sb.AppendLine("  - Always include at least one emoji or emoticon that fits into the theme of the article.");
            sb.AppendLine("- generate the post as properly formatted markdown that can be displayed properly in a LinkedIn post");
            sb.AppendLine("- SERIOUSLY, DO NOT USE EMDASHES!!!");
            sb.AppendLine("]");

            return sb.ToString();
        }
    }
}