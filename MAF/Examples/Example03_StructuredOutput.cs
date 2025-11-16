using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using MAF.Models;
using MAF.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace MAF.Examples;

/// <summary>
/// Example 08: Structured Output with JSON (Interactive Demo)
/// 
/// A demo demonstrating how to extract structured data from text using JSON schemas
/// with better organization and error handling.
/// </summary>
public class Example03_StructuredOutput
{
    private readonly AzureOpenAISettings _settings;
    private string _currentSchema = "person";

    private readonly Dictionary<string, Type> _availableSchemas = new()
    {
        ["person"] = typeof(PersonInfo),
        ["company"] = typeof(CompanyInfo),
        ["product"] = typeof(ProductInfo)
    };

    public Example03_StructuredOutput(AzureOpenAISettings settings)
    {
        _settings = settings;
    }

    public async Task RunAsync()
    {
        PrintWelcomeMessage();

        try
        {
            JsonElement personSchema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));
            JsonElement companySchema = AIJsonUtilities.CreateJsonSchema(typeof(CompanyInfo));
            JsonElement productSchema = AIJsonUtilities.CreateJsonSchema(typeof(ProductInfo));

            ChatOptions chatOptions = new ()
            {
                        
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema(
                    schema: personSchema,
                    schemaName: "PersonInfo",
                    schemaDescription: "Information about a person including their name, age, and occupation")                
            };

            string instructions = $"""
                You are an expert information extraction assistant.
                
                Extract structured {_currentSchema} information from the user's text and return it as valid JSON.
                Only extract information that is explicitly mentioned or can be reasonably inferred.
                If information is not available, use null for that field.
                
                Return ONLY the JSON object, no additional text or formatting.
                """;

            AIAgent chatClient = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureCliCredential())
                .GetChatClient(_settings.ModelName)
                .CreateAIAgent(new ChatClientAgentOptions()
                {
                    Instructions = instructions,
                    ChatOptions = chatOptions
                });

            Console.WriteLine("✅ Agent created for structured data extraction");

            await StartExtractionLoop(chatClient);

        }
        catch (Exception ex)
        {
            ChatInterface.PrintError($"Failed to create structured output agent: {ex.Message}");
        }
    }

    private void PrintWelcomeMessage()
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("📊 DEMO: Structured Output with JSON");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\n✨ This demo extracts structured data from your text using AI");
        Console.WriteLine($"🎯 Current extraction schema: {_currentSchema}");

        Console.WriteLine($"\n📋 Available schemas:");
        foreach (var schema in _availableSchemas.Keys)
        {
            Console.WriteLine($"   • {schema}");
        }

        Console.WriteLine($"\n🔍 Current schema fields ({_currentSchema}):");
        PrintSchemaDescription(_currentSchema);

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("💬 Interactive Chat");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\n🎮 Commands:");
        Console.WriteLine("   • Type text to extract information");
        Console.WriteLine("   • 'schema <name>' - Switch extraction schema");
        Console.WriteLine("   • 'schemas' - List available schemas");
        Console.WriteLine("   • 'help' - Show this help");
        Console.WriteLine("   • 'quit' - Exit demo");

        Console.WriteLine("\n💡 Example inputs:");
        Console.WriteLine("   • 'John is 30 years old, works as a software engineer in Seattle'");
        Console.WriteLine("   • 'Apple Inc. is a technology company founded in 1976 in Cupertino'");
        Console.WriteLine("   • 'iPhone 15 is a smartphone by Apple priced at $999'");
    }

    private async Task StartExtractionLoop(AIAgent chatClient)
    {
        while (true)
        {
            try
            {
                Console.Write($"\nYou ({_currentSchema}): ");
                var userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (ChatInterface.ShouldExit(userInput))
                {
                    ChatInterface.PrintGoodbye();
                    break;
                }

                // Handle commands
                if (HandleCommands(userInput))
                    continue;

                // Extract structured data
                await ExtractStructuredData(chatClient, userInput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ An error occurred: {ex.Message}");
            }
        }
    }



    private bool HandleCommands(string userInput)
    {
        var commandParts = userInput.ToLower().Split();
        var command = commandParts.Length > 0 ? commandParts[0] : "";

        return command switch
        {
            "schema" when commandParts.Length > 1 => SwitchSchema(commandParts[1]),
            "schemas" => ListSchemas(),
            "help" => ShowHelp(),
            _ => false
        };
    }

    private bool SwitchSchema(string schemaName)
    {
        if (_availableSchemas.ContainsKey(schemaName))
        {
            _currentSchema = schemaName;
            Console.WriteLine($"\n✅ Switched to '{schemaName}' schema");
            Console.WriteLine($"\n🔍 Schema fields:");
            PrintSchemaDescription(schemaName);
        }
        else
        {
            Console.WriteLine($"\n❌ Unknown schema '{schemaName}'");
            Console.WriteLine($"Available schemas: {string.Join(", ", _availableSchemas.Keys)}");
        }
        return true;
    }

    private bool ListSchemas()
    {
        Console.WriteLine($"\n📋 Available extraction schemas:");
        foreach (var schema in _availableSchemas.Keys)
        {
            Console.WriteLine($"\n🎯 {schema}:");
            PrintSchemaDescription(schema);
        }
        return true;
    }

    private bool ShowHelp()
    {
        PrintWelcomeMessage();
        return true;
    }

    private void PrintSchemaDescription(string schemaName)
    {
        var descriptions = schemaName switch
        {
            "person" => new[]
            {
                "   • name: Person's full name",
                "   • age: Person's age in years",
                "   • occupation: Person's job or profession",
                "   • city: City where person lives"
            },
            "company" => new[]
            {
                "   • name: Company name",
                "   • industry: Industry or sector",
                "   • founded_year: Year company was founded",
                "   • location: Company headquarters location",
                "   • employees: Number of employees"
            },
            "product" => new[]
            {
                "   • name: Product name",
                "   • category: Product category",
                "   • price: Product price",
                "   • brand: Brand or manufacturer",
                "   • description: Product description"
            },
            _ => new[] { "   Schema not found" }
        };

        foreach (var description in descriptions)
        {
            Console.WriteLine(description);
        }
    }

    private async Task ExtractStructuredData(AIAgent chatClient, string userInput)
    {
        Console.WriteLine($"\n🔄 Extracting {_currentSchema} information...");

        try
        {
            var messages = new List<OpenAI.Chat.ChatMessage>();

            messages.Add(new UserChatMessage(userInput));

            var response = await chatClient.RunAsync(messages);

            var assistentMessage = response.AsChatResponse().Text;
            Console.WriteLine($"\n🤖 Assistant Response:\n{assistentMessage}");
            
            messages.Add(new AssistantChatMessage(assistentMessage));

            if (!string.IsNullOrEmpty(assistentMessage))
            {
                ProcessExtractedData(assistentMessage);
            }
            else
            {
                Console.WriteLine($"❌ Could not extract {_currentSchema} information from the provided text");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during extraction: {ex.Message}");
        }
    }

    private void ProcessExtractedData(string jsonContent)
    {
        try
        {
            var schemaType = _availableSchemas[_currentSchema];
            var extractedData = JsonSerializer.Deserialize(jsonContent, schemaType);

            if (extractedData != null && HasAnyData(extractedData))
            {
                Console.WriteLine($"\n📊 Extracted {_currentSchema.Substring(0, 1).ToUpper()}{_currentSchema[1..]} Information:");

                var displayDict = GetDisplayDictionary(extractedData);
                foreach (var kvp in displayDict)
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
                }

                // Show confidence based on how many fields were extracted
                var filledFields = displayDict.Values.Count(v => v != "Not specified");
                var totalFields = displayDict.Count;
                var confidence = (double)filledFields / totalFields * 100;
                Console.WriteLine($"\n📈 Extraction confidence: {confidence:F1}% ({filledFields}/{totalFields} fields)");
            }
            else
            {
                Console.WriteLine($"❌ Could not extract {_currentSchema} information from the provided text");
                Console.WriteLine("💡 Try providing more detailed information or switch to a different schema");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing extracted data: {ex.Message}");
        }
    }

    private bool HasAnyData(object extractedData)
    {
        return extractedData switch
        {
            PersonInfo person => person.HasAnyData(),
            CompanyInfo company => company.HasAnyData(),
            ProductInfo product => product.HasAnyData(),
            _ => false
        };
    }

    private Dictionary<string, string> GetDisplayDictionary(object extractedData)
    {
        return extractedData switch
        {
            PersonInfo person => person.ToDisplayDictionary(),
            CompanyInfo company => company.ToDisplayDictionary(),
            ProductInfo product => product.ToDisplayDictionary(),
            _ => new Dictionary<string, string>()
        };
    }

}