using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;


namespace ReadmeGenerator.Core
{
    public class ReadmeService
    {
        private readonly string _apiKey;
        private readonly string _model;

        public ReadmeService(string apiKey, string model = "openai/gpt-4-turbo")
        {
            _apiKey = apiKey;
            _model = model;
        }

        public string GeneratePromptFromDirectory(string path, int maxTokens = 500)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The project contains the following files:\n");

            var extensions = new[] { ".cs", ".py", ".js", ".ts", ".java", ".cpp" };
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f)));

            int currentTokens = 0;
            int maxChars = maxTokens * 4;

            foreach (var file in files)
            {
                if (currentTokens >= maxTokens)
                    break;

                var header = $"### {Path.GetRelativePath(path, file)}\n```{Path.GetExtension(file).TrimStart('.')}\n";
                if (currentTokens + header.Length / 4 > maxTokens)
                    break;

                sb.Append(header);
                currentTokens += header.Length / 4;

                try
                {
                    foreach (var line in File.ReadLines(file))
                    {
                        var lineTokens = line.Length / 4;
                        if (currentTokens + lineTokens > maxTokens)
                            break;

                        sb.AppendLine(line);
                        currentTokens += lineTokens;
                    }
                }
                catch (Exception e)
                {
                    var errorLine = $"// Error reading file: {e.Message}\n";
                    if (currentTokens + errorLine.Length / 4 <= maxTokens)
                    {
                        sb.AppendLine(errorLine);
                        currentTokens += errorLine.Length / 4;
                    }
                }

                var footer = "```\n\n";
                if (currentTokens + footer.Length / 4 > maxTokens)
                    break;

                sb.AppendLine(footer);
                currentTokens += footer.Length / 4;
            }

            Console.WriteLine(currentTokens);
            return sb.ToString();
        }



        public async Task<string> GenerateReadmeAsync(string prompt)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            int promptTokens = prompt.Length / 4;

            int maxTokensForResponse = 1333 - promptTokens;
            if (maxTokensForResponse < 50) maxTokensForResponse = 50;

            var requestBody = new
                {
                    model = _model,
                    max_tokens = maxTokensForResponse,
                    messages = new[]
                    {
                        new { role = "system", content = "Generate a professional README.md file based on the code below." },
                        new { role = "user", content = prompt }
                    }
                };

            var json = JsonSerializer.Serialize(requestBody);
            var response = await http.PostAsync("https://openrouter.ai/api/v1/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Eroare API: {response.StatusCode}\n{responseString}");

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}