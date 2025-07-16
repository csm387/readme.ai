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

        public async Task<string> GeneratePromptFromDirectory(string path, int maxFiles = 5)
        {
            var sb = new StringBuilder();
            sb.AppendLine("These are the summaries of the files in the project:\n");

            var extensions = new[] { ".cs", ".py", ".js", ".ts", ".java", ".cpp" };
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f)))
                                 .Take(maxFiles);

            foreach (var file in files)
            {
                sb.AppendLine($"### {Path.GetRelativePath(path, file)}");

                try
                {
                    string summary = await SummarizeFileAsync(file);
                    sb.AppendLine(summary);
                    sb.AppendLine();
                }
                catch (Exception e)
                {
                    sb.AppendLine($"// Summary error: {e.Message}");
                }
            }

            return sb.ToString();
        }

        public async Task<string> SummarizeFileAsync(string filePath)
        {
            string content = File.ReadAllText(filePath);
            string prompt = $"Summarize this code file in a short paragraph:\n```\n{content}\n```";

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = _model,
                max_tokens = 200, 
                messages = new[]
                {
            new { role = "system", content = "You are an assistant who summarizes source code." },
            new { role = "user", content = prompt }
        }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var response = await http.PostAsync("https://openrouter.ai/api/v1/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Summary API error: {response.StatusCode}\n{responseString}");

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
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