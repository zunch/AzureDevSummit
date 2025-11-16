using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAF.Examples;

/// <summary>
/// Example 13: MCP Interactive Demo
/// Shows integration with Model Context Protocol (MCP) servers for extended functionality.
/// </summary>
public class Example06_MCPInteractive
{
    private readonly AzureOpenAISettings _settings;

    public Example06_MCPInteractive(AzureOpenAISettings settings)
    {
        _settings = settings;
    }

    public async Task RunAsync()
    {
        Console.Clear();
        try
        {
            // Create an MCPClient for the GitHub server
            await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "MCPServer",
                Command = "npx",
                Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
            }));

            // Retrieve the list of tools available on the GitHub server
            var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

            // Create AI client
            AIAgent chatClient = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureCliCredential())
                .GetChatClient(_settings.ModelName)
                .CreateAIAgent(instructions: "You answer questions related to GitHub repositories only.",
                                tools: [.. mcpTools.Cast<AITool>()]);

            Console.WriteLine("Type 'quit' to exit");
            Console.WriteLine(new string('=', 75));

            var messages = new List<OpenAI.Chat.ChatMessage>();

            while (true)
            {
                Console.Write("\n💭 You: ");
                var userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userInput))
                {
                    Console.WriteLine("⚠️  Please enter a message.");
                    continue;
                }

                if (userInput.ToLower() is "quit" or "exit")
                {
                    Console.WriteLine("\n✅ Thanks for trying MCP! Goodbye!");
                    break;
                }

                messages.Add(new UserChatMessage(userInput));

                try
                {
                    Console.WriteLine("\n🤖 Agent: ");

                    var response = await chatClient.RunAsync(messages);

                    var assistantMessage = response.AsChatResponse().Text;
                    Console.WriteLine(assistantMessage);
                    messages.Add(new AssistantChatMessage(assistantMessage));                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine("\nTROUBLESHOOTING:");
            Console.WriteLine("1. Check 'uv' is installed: uv --version");
            Console.WriteLine("2. Try manually: uvx mcp-server-calculator");
            Console.WriteLine("3. Check Python version (3.10+ required)");
        }
    }

    private async Task<Process?> StartMCPServerAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "uvx",
                Arguments = "mcp-server-calculator",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,  // Don't redirect stdin - let console work normally
                CreateNoWindow = true
            };

            var process = Process.Start(processInfo);
            if (process != null)
            {
                // Start reading output asynchronously to prevent blocking
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Give it a moment to start
                await Task.Delay(2000);
                if (!process.HasExited)
                {
                    return process;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start MCP server: {ex.Message}");
        }

        return null;
    }

    private ChatTool CreateCalculatorTool(string functionName, string description)
    {
        string parameters;
        
        if (functionName == "squareRoot")
        {
            parameters = """
                {
                    "type": "object",
                    "properties": {
                        "number": {
                            "type": "number",
                            "description": "The number to calculate square root of"
                        }
                    },
                    "required": ["number"]
                }
                """;
        }
        else if (functionName == "power")
        {
            parameters = """
                {
                    "type": "object",
                    "properties": {
                        "baseNumber": {
                            "type": "number",
                            "description": "The base number"
                        },
                        "exponent": {
                            "type": "number",
                            "description": "The exponent"
                        }
                    },
                    "required": ["baseNumber", "exponent"]
                }
                """;
        }
        else
        {
            parameters = """
                {
                    "type": "object",
                    "properties": {
                        "a": {
                            "type": "number",
                            "description": "First number"
                        },
                        "b": {
                            "type": "number",
                            "description": "Second number"
                        }
                    },
                    "required": ["a", "b"]
                }
                """;
        }

        return ChatTool.CreateFunctionTool(
            functionName: functionName,
            functionDescription: description,
            functionParameters: BinaryData.FromString(parameters)
        );
    }

    private string ExecuteCalculatorFunction(string functionName, BinaryData arguments)
    {
        try
        {
            var json = arguments.ToString();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            double result = functionName switch
            {
                "add" => Add(root.GetProperty("a").GetDouble(), root.GetProperty("b").GetDouble()),
                "subtract" => Subtract(root.GetProperty("a").GetDouble(), root.GetProperty("b").GetDouble()),
                "multiply" => Multiply(root.GetProperty("a").GetDouble(), root.GetProperty("b").GetDouble()),
                "divide" => Divide(root.GetProperty("a").GetDouble(), root.GetProperty("b").GetDouble()),
                "power" => Power(root.GetProperty("baseNumber").GetDouble(), root.GetProperty("exponent").GetDouble()),
                "squareRoot" => SquareRoot(root.GetProperty("number").GetDouble()),
                _ => throw new InvalidOperationException($"Unknown function: {functionName}")
            };

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error executing {functionName}: {ex.Message}";
        }
    }

    // Simulated MCP Calculator Functions
    private static double Add(double a, double b)
    {
        Console.WriteLine($"   📊 MCP Calculator: {a} + {b}");
        return a + b;
    }

    private static double Subtract(double a, double b)
    {
        Console.WriteLine($"   📊 MCP Calculator: {a} - {b}");
        return a - b;
    }

    private static double Multiply(double a, double b)
    {
        Console.WriteLine($"   📊 MCP Calculator: {a} × {b}");
        return a * b;
    }

    private static double Divide(double a, double b)
    {
        Console.WriteLine($"   📊 MCP Calculator: {a} ÷ {b}");
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return a / b;
    }

    private static double Power(double baseNumber, double exponent)
    {
        Console.WriteLine($"   📊 MCP Calculator: {baseNumber} ^ {exponent}");
        return Math.Pow(baseNumber, exponent);
    }

    private static double SquareRoot(double number)
    {
        Console.WriteLine($"   📊 MCP Calculator: √{number}");
        if (number < 0)
            throw new ArgumentException("Cannot calculate square root of negative number");
        return Math.Sqrt(number);
    }
}