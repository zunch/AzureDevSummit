using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MAF.Examples
{
    public class Example09_AgentInWorkflow
    {
        private readonly AzureOpenAISettings _settings;
        private readonly GitHubMCPSettings _gitHubMCPSettings;

        public Example09_AgentInWorkflow(AzureOpenAISettings settings, GitHubMCPSettings gitHubMCPSettings)
        {
            _settings = settings;
            _gitHubMCPSettings = gitHubMCPSettings;
        }

        public async Task RunAsync()
        {
            ChatInterface.PrintWelcomeMessage(
           "Agent in Workflow",
           "This demo shows how to use agents in a sequential workflow."
       );

            Console.WriteLine("\n📋 This demo has 3 agents:");
            Console.WriteLine("   Architect agent");
            Console.WriteLine("   Coder agent");
            Console.WriteLine("   Code rewiew agent");

            try
            {
                // Create an MCPClient for the GitHub server
                await using var mcpGitHub = await McpClient.CreateAsync(new StdioClientTransport(new()
                {
                    Name = "MCPServer",
                    Command = "npx",
                    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        ["GITHUB_PERSONAL_ACCESS_TOKEN"] = _gitHubMCPSettings.GitHubPersonalAccessToken
                    }
                }));

                await using var mcpFileSystem = await McpClient.CreateAsync(new StdioClientTransport(new()
                {
                    Name = "MCPFileSystem",
                    Command = "npx",
                    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-filesystem", "c:/tmp"]
                }));


                // Retrieve the list of tools available on the GitHub server
                var mcpGitHubTools = await mcpGitHub.ListToolsAsync().ConfigureAwait(false);
                var mcpFileSystemTools = await mcpFileSystem.ListToolsAsync().ConfigureAwait(false);

                var chatClient = new AzureOpenAIClient(
                    new Uri(_settings.Endpoint),
                    new AzureCliCredential())
                .GetChatClient("gpt-4o");

                // Create the three AI agents
                AIAgent architectAgent = chatClient.CreateAIAgent(
                    name: "SoftwareArchitect",
                    instructions: @"
You are an experienced software architect. Your task is to:
1. Carefully analyze user requirements
2. Define a clear technical architecture
3. Choose appropriate technologies and patterns
4. Create a detailed specification that a developer can implement from

Your response should include:
- System overview
- Technology choices (language, framework, database)
- API endpoints (for REST APIs)
- Data models
- Architecture patterns
- Security considerations

Be concise but complete."
                );

                AIAgent coderAgent = chatClient.CreateAIAgent(
                    name: "SoftwareDeveloper",
                    instructions: @"
You are a skilled developer. Your task is to:
1. Carefully read the architect's specification
2. Implement complete, working code
3. Follow best practices and conventions
4. Write clean, well-structured code

Your code should:
- Be complete and executable
- Follow the specification exactly
- Include appropriate comments
- Use modern language features
- Be production quality

Produce ONLY code with necessary comments.
When all code is ready, 
create the solution files in c:\tmp folder
initiate git in the folder
push the files to a new repository on GitHub,
respond with 'CODE COMPLETE'.
",
                    tools: [.. mcpGitHubTools.Cast<AITool>(), .. mcpFileSystemTools.Cast<AITool>()]
                );

                AIAgent reviewerAgent = chatClient.CreateAIAgent(
                    name: "CodeReviewer",
                    instructions: @"
You are a senior code reviewer. Your task is to:
1. Review the code against the specification
2. Identify potential bugs
3. Check for security issues
4. Verify best practices
5. Provide constructive feedback

Focus on:
- Functional correctness
- Security issues (injection, auth, etc)
- Performance and scalability
- Code quality and readability
- Error handling
- Testing possibilities

Provide concrete, actionable feedback. Be honest but constructive."
                );

                // Build the workflow connecting the three agents
                WorkflowBuilder builder = new(architectAgent);
                builder.AddEdge(architectAgent, coderAgent);
                builder.AddEdge(coderAgent, reviewerAgent);
                Workflow workflow = builder.Build();

                // User requirement
                var userRequirement = @"
Build a REST API for a todo application in C# .NET with the following features:
- Create new todos
- Get all todos
- Get a specific todo
- Update a todo
- Delete a todo
- Mark todo as complete/incomplete

Each todo should have: id, title, description, isCompleted, createdDate.
";

                Console.WriteLine("=== Software Development Workflow ===");
                Console.WriteLine($"User Requirement:\n{userRequirement}");
                Console.WriteLine(new string('=', 70));
                Console.WriteLine();

                // Execute the workflow with streaming
                StreamingRun run = await InProcessExecution.StreamAsync(
                    workflow,
                    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userRequirement)
                );

                // Send turn token to trigger the agents with events enabled
                await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

                string? lastExecutorId = null;

                // Watch and display streaming events
                await foreach (WorkflowEvent evt in run.WatchStreamAsync())
                {
                    switch (evt)
                    {
                        case ExecutorInvokedEvent invoke:
                            // When an executor starts
                            var executorName = GetFriendlyName(invoke.ExecutorId);

                            if (executorName != lastExecutorId)
                            {
                                if (lastExecutorId != null)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine();
                                }

                                string icon = executorName switch
                                {
                                    "Architect" => "📐",
                                    "Developer" => "💻",
                                    "Reviewer" => "🔍",
                                    _ => "🤖"
                                };

                                Console.WriteLine($"{icon} {executorName.ToUpper()}");
                                Console.WriteLine(new string('-', 70));
                                lastExecutorId = executorName;
                            }
                            break;

                        case ExecutorCompletedEvent complete:
                            // When an executor finishes - show the result
                            if (complete.Data != null)
                            {
                                Console.WriteLine(complete.Data);
                            }
                            break;

                        case AgentRunUpdateEvent agentUpdate:
                            // Streaming updates from agents (if they come)
                            Console.Write(agentUpdate.Data);
                            break;

                        case ExecutorFailedEvent failed:
                            // Show what's actually in the event
                            Console.WriteLine($"\n❌ EXECUTOR FAILED: {failed.ExecutorId}");
                            Console.WriteLine($"   Event details: {failed}");
                            break;

                        case WorkflowErrorEvent error:
                            // Show what's actually in the event
                            Console.WriteLine($"\n❌ WORKFLOW ERROR");
                            Console.WriteLine($"   Event details: {error}");
                            break;

                        case WorkflowOutputEvent output:
                            Console.WriteLine($"\n✅ Final output: {output.Data}");
                            break;
                    }
                }

                static string GetFriendlyName(string executorId)
                {
                    if (executorId.Contains("Architect")) return "Architect";
                    if (executorId.Contains("Developer")) return "Developer";
                    if (executorId.Contains("Reviewer")) return "Reviewer";
                    return executorId;
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(new string('=', 70));
                Console.WriteLine("=== Workflow Completed ===");
            }
            catch (Exception ex)
            {
                ChatInterface.PrintError($"Failed to create agents {ex.Message}");
            }
        }
    }
}
