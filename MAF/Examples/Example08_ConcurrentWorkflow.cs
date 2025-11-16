using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MAF.Examples
{
    public class Example08_ConcurrentWorkflow
    {
        private readonly AzureOpenAISettings _settings;

        public Example08_ConcurrentWorkflow(AzureOpenAISettings settings)
        {
            _settings = settings;
        }

        public async Task RunAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_settings.Endpoint), new AzureCliCredential()).GetChatClient(_settings.ModelName).AsIChatClient();

            // Create the executors
            ChatClientAgent physicist = new(
                chatClient,
                name: "Physicist",
                instructions: "You are an expert in physics. You answer questions from a physics perspective."
            );
            ChatClientAgent chemist = new(
                chatClient,
                name: "Chemist",
                instructions: "You are an expert in chemistry. You answer questions from a chemistry perspective."
            );
            var startExecutor = new ConcurrentStartExecutor();
            var aggregationExecutor = new ConcurrentAggregationExecutor();

            // Build the workflow by adding executors and connecting them
            var workflow = new WorkflowBuilder(startExecutor)
                .AddFanOutEdge(startExecutor, [physicist, chemist])
                .AddFanInEdge([physicist, chemist], aggregationExecutor)
                .WithOutputFrom(aggregationExecutor)
                .Build();

            // Execute the workflow in streaming mode
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, input: "What is temperature?");
            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is WorkflowOutputEvent output)
                {
                    Console.WriteLine($"Workflow completed with results:\n{output.Data}");
                }
            }
        }
    }

    /// <summary>
    /// Executor that starts the concurrent processing by sending messages to the agents.
    /// </summary>
    internal sealed class ConcurrentStartExecutor() :
        Executor<string>("ConcurrentStartExecutor")
    {
        /// <summary>
        /// Starts the concurrent processing by sending messages to the agents.
        /// </summary>
        /// <param name="message">The user message to process</param>
        /// <param name="context">Workflow context for accessing workflow services and adding events</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
        /// The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // Broadcast the message to all connected agents. Receiving agents will queue
            // the message but will not start processing until they receive a turn token.
            await context.SendMessageAsync(new ChatMessage(ChatRole.User, message), cancellationToken: cancellationToken);
            // Broadcast the turn token to kick off the agents.
            await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Executor that aggregates the results from the concurrent agents.
    /// </summary>
    internal sealed class ConcurrentAggregationExecutor() :
        Executor<List<ChatMessage>>("ConcurrentAggregationExecutor")
    {
        private readonly List<ChatMessage> _messages = [];

        /// <summary>
        /// Handles incoming messages from the agents and aggregates their responses.
        /// </summary>
        /// <param name="message">The messages from the agent</param>
        /// <param name="context">Workflow context for accessing workflow services and adding events</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
        /// The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            this._messages.AddRange(message);

            if (this._messages.Count == 2)
            {
                var formattedMessages = string.Join(Environment.NewLine, this._messages.Select(m => $"{m.AuthorName}: {m.Text}"));
                await context.YieldOutputAsync(formattedMessages, cancellationToken);
            }
        }
    }
}
