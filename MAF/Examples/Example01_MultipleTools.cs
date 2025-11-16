using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.Buffers.Text;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAF.Examples;

/// <summary>
/// Example 01: Multiple Function Tools (Interactive Demo)
/// 
/// This demo shows an agent with MULTIPLE tools:
/// - Weather tool
/// - Calculator tool
/// - Time zone tool
/// 
/// The agent automatically chooses the right tool based on your question.
/// </summary>
public class Example01_MultipleTools
{
    private readonly AzureOpenAISettings _settings;

    public Example01_MultipleTools(AzureOpenAISettings settings)
    {
        _settings = settings;
    }

    public async Task RunAsync()
    {
        ChatInterface.PrintWelcomeMessage(
            "Multiple Function Tools",
            "This demo shows an agent with multiple tools that automatically chooses the right one."
        );

        try
        {
            // Create Azure OpenAI client

            AIAgent chatClient = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureCliCredential())
                .GetChatClient(_settings.ModelName)
                .CreateAIAgent(instructions: "You are a helpful assistant with weather, calculator, and time tools.Choose the right tool automatically based on the user's question.", 
                                tools: [AIFunctionFactory.Create(ToolDefinitions.GetWeather),
                                        AIFunctionFactory.Create(ToolDefinitions.Calculate),
                                        AIFunctionFactory.Create(ToolDefinitions.GetTimeAsync)]);


            Console.WriteLine("\n✅ Agent created with 3 tools:");
            Console.WriteLine("   🌤️  Weather tool");
            Console.WriteLine("   🧮 Calculator tool");
            Console.WriteLine("   ⏰ Time zone tool");

            // Start chat session with multiple tools
            await StartChatSession(chatClient);
        }
        catch (Exception ex)
        {
            ChatInterface.PrintError($"Failed to create multi-tool agent: {ex.Message}");
        }
    }

    private async Task StartChatSession(AIAgent chatClient)
    {
        var conversationHistory = new List<OpenAI.Chat.ChatMessage>();

        Console.WriteLine("\n💬 Starting multi-tool chat...");

        while (true)
        {
            var userInput = ChatInterface.GetUserInput();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (ChatInterface.ShouldExit(userInput))
            {
                ChatInterface.PrintGoodbye();
                break;
            }

            try
            {
                conversationHistory.Add(new UserChatMessage(userInput));

                var response = await chatClient.RunAsync(conversationHistory);

                var assistantMessage = response.AsChatResponse().Text;
                Console.WriteLine($"Agent: {assistantMessage}");
                conversationHistory.Add(new AssistantChatMessage(assistantMessage));
            }
            catch (Exception ex)
            {
                ChatInterface.PrintError($"Error during chat: {ex.Message}");
            }
        }
    }
}