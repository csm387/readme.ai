using ReadmeGenerator.Core;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "--cli")
        {
            Console.WriteLine("To run the CLI, use: dotnet run --project ReadmeGenerator.CLI -- --cli");
            return;
        }

        Console.WriteLine("📁 Enter the path to the project:");
        var path = Console.ReadLine()?.Trim() ?? "";
        if (!Directory.Exists(path))
        {
            Console.WriteLine("❌ Directorul nu există.");
            return;
        }

        Console.Write("🔑 API Key OpenRouter: ");
        var apiKey = Console.ReadLine()?.Trim() ?? "";

        var generator = new ReadmeService(apiKey);
        var prompt = generator.GeneratePromptFromDirectory(path);

        Console.WriteLine("🧠 Sent to AI...");
        var readme = await generator.GenerateReadmeAsync(prompt);

        File.WriteAllText(Path.Combine(path, "README.md"), readme);
        Console.WriteLine("✅ README.md successfully generated!");
    }
}