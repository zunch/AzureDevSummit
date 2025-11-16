using MAF.Examples;
using MAF.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MAF;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ConfigurationService>();

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            var configService = serviceProvider.GetRequiredService<ConfigurationService>();

            // Show menu
            while (true)
            {
                ShowMenu();
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await RunExample01(configService);
                        break;
                    case "2":
                        await RunExample02(configService);
                        break;
                    case "3":
                        await RunExample03(configService);
                        break;
                    case "4":
                        await RunExample04(configService);
                        break;
                    case "5":
                        await RunExample05(configService);
                        break;
                    case "6":
                        await RunExample06(configService);
                        break;
                    case "7":
                        await RunExample07(configService);
                        break;
                    case "8":
                        await RunExample08(configService);
                        break;
                    case "9":
                        await RunExample09(configService);
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        Console.WriteLine("\n👋 Goodbye!");
                        return;
                    default:
                        Console.WriteLine("\n❌ Invalid choice. Please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Application error: {ex.Message}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    private static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("🤖 Microsoft Agent Framework - .NET 8 Examples");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();
        Console.WriteLine("Available Examples:");
        Console.WriteLine("  1. Multiple Function Tools");
        Console.WriteLine("  2. Human-in-the-Loop Approval");
        Console.WriteLine("  3. Structured Output with JSON");
        Console.WriteLine("  4. Long-Term Memory");
        Console.WriteLine("  5. Middleware Pipeline");
        Console.WriteLine("  6. MCP Interactive Demo");
        Console.WriteLine("  7. Sequential workflow");
        Console.WriteLine("  8. Concurrent workflow");
        Console.WriteLine("  9. Agents in workflow");
        Console.WriteLine();
        Console.WriteLine("  Q. Quit");
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.Write("Enter your choice (1-9 or Q): ");
    }

    private static async Task RunExample01(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example01_MultipleTools(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 6: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample02(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example02_HumanInTheLoop(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 7: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample03(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example03_StructuredOutput(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 8: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample04(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example04_LongTermMemory(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 10: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample05(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example05_Middleware(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 11: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample06(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example06_MCPInteractive(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 13: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample07(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example07_SequentialWorkflow(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 14: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample08(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example08_ConcurrentWorkflow(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 15: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

    private static async Task RunExample09(ConfigurationService configService)
    {
        try
        {
            var settings = configService.GetAzureOpenAISettings();
            var example = new Example09_AgentInWorkflow(settings);
            await example.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running Example 15: {ex.Message}");
            Console.WriteLine("Please check your Azure OpenAI configuration in appsettings.json");
        }
    }

}