using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace MAF.Examples;

/// <summary>
/// Example 07: Human-in-the-Loop Approval (Interactive Demo)
/// 
/// Simple real example with 2 functions:
/// 1. create_file() - No approval needed (safe operation)
/// 2. delete_file() - Requires approval (dangerous operation)
/// </summary>
public class Example02_HumanInTheLoop
{
    private readonly AzureOpenAISettings _settings;
    
    public Example02_HumanInTheLoop(AzureOpenAISettings settings)
    {
        _settings = settings;
    }
    
    public async Task RunAsync()
    {
        ChatInterface.PrintWelcomeMessage(
            "Human-in-the-Loop - Create vs Delete",
            "This demo shows safe vs dangerous operations with approval workflow."
        );
        
        Console.WriteLine("\n📋 This demo has 2 functions:");
        Console.WriteLine("   ✅ create_file() - Runs immediately (no approval)");
        Console.WriteLine("   🔒 delete_file() - Requires your approval first");
        
        try
        {
            string instructions = """
                You are a file management assistant with access to file operations.

                IMPORTANT: You MUST call the functions directly. Do NOT ask the user for permission in chat.

                Rules:
                1. When user asks to create a file: IMMEDIATELY call create_file() function
                2. When user asks to delete a file: IMMEDIATELY call delete_file() function  
                3. Do NOT ask for confirmation in the chat - the system will handle approvals automatically
                4. Just call the function and report the result
                """;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AIAgent chatClient = new AzureOpenAIClient(
                     new Uri(_settings.Endpoint),
                     new AzureCliCredential())
                     .GetChatClient(_settings.ModelName)                     
                     .CreateAIAgent(instructions: instructions,
                                     tools: [AIFunctionFactory.Create(ToolDefinitions.CreateFile),
                                             new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ToolDefinitions.DeleteFile))]);
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


            // Create demo directory - use same path as ToolDefinitions
            var demoDir = "C:/demo_files";
            
            try
            {
                if (!Directory.Exists(demoDir))
                {
                    Directory.CreateDirectory(demoDir);
                    Console.WriteLine($"✅ Created demo directory: {demoDir}");
                }
                else
                {
                    Console.WriteLine($"✅ Using existing demo directory: {demoDir}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Could not create demo directory: {ex.Message}");
                Console.WriteLine("   File operations may fail.");
            }
            
            Console.WriteLine($"\n✅ Agent created with 2 functions");
            Console.WriteLine($"📁 Files will be created in: {Path.GetFullPath(demoDir)}");
            
            Console.WriteLine("\n💡 Try these commands:");
            Console.WriteLine("   • Create a file named test.txt with some content");
            Console.WriteLine("   • Delete test.txt");
            Console.WriteLine("   • Create file notes.txt saying 'Hello World'");
            Console.WriteLine("   • Delete notes.txt");
            
            // Start chat session
            await StartChatSession(chatClient);
        }
        catch (Exception ex)
        {
            ChatInterface.PrintError($"Failed to create human-in-the-loop agent: {ex.Message}");
        }
    }
    
    private async Task StartChatSession(AIAgent chatClient)
    {
        var conversationHistory = new List<Microsoft.Extensions.AI.ChatMessage>();
        
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
                conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userInput));

                AgentThread t = chatClient.GetNewThread();

                AgentRunResponse response = await chatClient.RunAsync(conversationHistory, t);

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var functionApprovalRequests = response.Messages
                        .SelectMany(x => x.Contents)
                        .OfType<FunctionApprovalRequestContent>()
                        .ToList();
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                if (functionApprovalRequests.Count > 0)
                {
#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    FunctionApprovalRequestContent requestContent = functionApprovalRequests.First();
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    Console.WriteLine($"We require approval to execute '{requestContent.FunctionCall.Name}'");

                    var appr = Console.ReadLine()?.Trim().ToLower();

                    var approvalMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, [requestContent.CreateResponse(appr == "true")]);
                    Console.WriteLine(await chatClient.RunAsync(approvalMessage, t));
                    
                }

                var assistantMessage = response.AsChatResponse().Text;
                Console.WriteLine($"Agent: {assistantMessage}");
                conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, assistantMessage));
            }
            catch (Exception ex)
            {
                ChatInterface.PrintError($"Error during chat: {ex.Message}");
            }
        }
    }
    
    private string HandleToolCallWithApproval(ChatToolCall functionCall)
    {
        try
        {
            return functionCall.FunctionName switch
            {
                "create_file" => HandleCreateFile(functionCall.FunctionArguments),
                "delete_file" => HandleDeleteFileWithApproval(functionCall.FunctionArguments),
                _ => $"Unknown function: {functionCall.FunctionName}"
            };
        }
        catch (Exception ex)
        {
            return $"Error executing {functionCall.FunctionName}: {ex.Message}";
        }
    }
    
    private string HandleCreateFile(BinaryData arguments)
    {
        var request = JsonSerializer.Deserialize<CreateFileRequest>(arguments.ToString());
        return ToolDefinitions.CreateFile(request?.Filename ?? "", request?.Content ?? "");
    }
    
    private string HandleDeleteFileWithApproval(BinaryData arguments)
    {
        var request = JsonSerializer.Deserialize<DeleteFileRequest>(arguments.ToString());
        var filename = request?.Filename ?? "";
        
        // Request approval for dangerous operation
        var approved = ApprovalService.RequestApproval(
            "delete_file", 
            new Dictionary<string, object> { ["filename"] = filename }
        );
        
        if (approved)
        {
            Console.WriteLine("✅ APPROVED: Executing delete_file");
            return ToolDefinitions.DeleteFile(filename);
        }
        else
        {
            Console.WriteLine("❌ REJECTED: Not executing delete_file");
            return $"⛔ Function 'delete_file' was rejected by the user.";
        }
    }
    
    private class CreateFileRequest
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
    
    private class DeleteFileRequest
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";
    }
}