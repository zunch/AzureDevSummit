using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAF.Examples;

/// <summary>
/// Example 11: Complete Middleware Demo
/// Shows different types of middleware working together - timing, security, logging, and token counting.
/// </summary>
public class Example05_Middleware
{
    private readonly AzureOpenAISettings _settings;

    public Example05_Middleware(AzureOpenAISettings settings)
    {
        _settings = settings;
    }

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("🎯 COMPLETE MIDDLEWARE DEMO - All 4 Types Working Together");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine();
        Console.WriteLine("This demo shows 4 middleware working simultaneously:");
        Console.WriteLine();
        Console.WriteLine("1️⃣  TIMING MIDDLEWARE        → Tracks how long each request takes");
        Console.WriteLine("2️⃣  SECURITY MIDDLEWARE      → Blocks sensitive content");
        Console.WriteLine("3️⃣  FUNCTION LOGGER          → Logs all tool calls");
        Console.WriteLine("4️⃣  TOKEN COUNTER            → Counts tokens sent to AI");
        Console.WriteLine();
        Console.WriteLine("Watch how they all work together in a real conversation!");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine();

        try
        {
            Console.WriteLine("🔧 Creating agent with all 4 middleware...\n");


            AIAgent chatClient = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureCliCredential())
                .GetChatClient(_settings.ModelName)
                .CreateAIAgent(instructions: "You are a helpful assistant with access to various tools. Be friendly, concise, and helpful in your responses.",
                                tools: [AIFunctionFactory.Create(ToolDefinitions.GetWeather),
                                        AIFunctionFactory.Create(ToolDefinitions.Calculate),
                                        AIFunctionFactory.Create(ToolDefinitions.GetTimeAsync),
                                        AIFunctionFactory.Create(SearchDatabase)]);

            var middlewareClient = chatClient
                .AsBuilder()
                .Use(runFunc: TimingMiddleware, runStreamingFunc: null)
                .Use(runFunc: SecurityMiddleware, runStreamingFunc: null)
                .Build();

            Console.WriteLine("✅ Agent created with 4 middleware layers!");

            Console.WriteLine();
            Console.WriteLine(new string('=', 75));
            Console.WriteLine("📝 SUGGESTED TEST PROMPTS:");
            Console.WriteLine(new string('=', 75));
            Console.WriteLine();
            Console.WriteLine("To see all middleware in action, try these prompts:");
            Console.WriteLine();
            Console.WriteLine("✅ PROMPT 1: \"tell me a joke\"");
            Console.WriteLine("   → Triggers: Timing + Token Counter");
            Console.WriteLine("   → Simple request, no functions");
            Console.WriteLine();
            Console.WriteLine("✅ PROMPT 2: \"what's the weather in Tokyo?\"");
            Console.WriteLine("   → Triggers: Timing + Function Logger + Token Counter");
            Console.WriteLine("   → Calls the get_weather function");
            Console.WriteLine();
            Console.WriteLine("✅ PROMPT 3: \"what time is it and calculate 15 * 8\"");
            Console.WriteLine("   → Triggers: Timing + Function Logger (2 calls) + Token Counter");
            Console.WriteLine("   → Multiple function calls");
            Console.WriteLine();
            Console.WriteLine("✅ PROMPT 4: \"what is my password?\"");
            Console.WriteLine("   → Triggers: Security (BLOCKS) + Timing");
            Console.WriteLine("   → Security middleware blocks this request!");
            Console.WriteLine();
            Console.WriteLine("✅ PROMPT 5: \"search for users and get weather in Paris\"");
            Console.WriteLine("   → Triggers: ALL 4 middleware");
            Console.WriteLine("   → Multiple functions, shows complete flow");
            Console.WriteLine();
            Console.WriteLine("Type 'quit' to exit");
            Console.WriteLine(new string('=', 75));
            Console.WriteLine();

            // Setup chat with tools
            var conversationHistory = new List<OpenAI.Chat.ChatMessage>();

            while (true)
            {
                Console.Write("💬 You: ");
                var userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userInput))
                    continue;

                if (userInput.ToLower() is "quit" or "exit" or "bye")
                {
                    Console.WriteLine("\n👋 Demo ended! Thanks for testing all the middleware!");
                    break;
                }

                Console.WriteLine();
                Console.WriteLine(new string('-', 75));
                Console.WriteLine("🔄 PROCESSING YOUR REQUEST...");
                Console.WriteLine(new string('-', 75));

                // Add user message
                conversationHistory.Add(new UserChatMessage(userInput));

                try
                {
                    Console.WriteLine("\n🤖 Agent: ");

                    var response = await middlewareClient.RunAsync(conversationHistory);

                    var assistantMessage = response.AsChatResponse().Text;
                    Console.WriteLine(assistantMessage);
                    conversationHistory.Add(new AssistantChatMessage(assistantMessage));


                    Console.WriteLine();
                    Console.WriteLine(new string('-', 75));
                    Console.WriteLine("✅ Request completed!");
                    Console.WriteLine();
                }
                catch (SecurityException ex)
                {
                    Console.WriteLine($"🚫 {ex.Message}");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            throw;
        }
    }


    private static string SearchDatabase(string query)
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["users"] = "Found 150 users matching criteria",
            ["products"] = "Found 45 products in inventory",
            ["orders"] = "Found 230 orders in last 30 days"
        };

        foreach (var kvp in results)
        {
            if (query.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return $"No results found for: {query}";
    }

    async Task<AgentRunResponse> TimingMiddleware(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            AgentThread? thread,
            AgentRunOptions? options,
            AIAgent innerAgent,
            CancellationToken cancellationToken)
    {
        var _startTime = DateTime.Now;
        Console.WriteLine($"⏱️  [TIMING] Started at {_startTime:HH:mm:ss}");

        var response = await innerAgent.RunAsync(messages, thread, options, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"⏱️  [TIMING] Stopped at {_startTime:HH:mm:ss} seconds");

        return response;
    }

    async Task<AgentRunResponse> SecurityMiddleware(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        AgentThread? thread,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        string[] _blockedKeywords = { "password", "secret", "hack", "exploit", "bypass" };

        var lastMessage = messages.LastOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.User);
        if (lastMessage != null)
        {
            var content = lastMessage.Text ?? "";
            string text = content.ToLowerInvariant();

            foreach (var keyword in _blockedKeywords)
            {
                if (text.Contains(keyword))
                {
                    Console.WriteLine($"🚫 [SECURITY] Request BLOCKED! Detected: '{keyword}'");
                    Console.WriteLine("🚫 [SECURITY] This request contains sensitive content and cannot be processed.");
                    throw new SecurityException($"Request blocked due to sensitive content: {keyword}");
                }
            }
        }

        var response = await innerAgent.RunAsync(messages, thread, options, cancellationToken).ConfigureAwait(false);

        return response;
    }
}