using Azure.AI.OpenAI;
using Azure.Identity;
using MAF.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace MAF.Examples;

/// <summary>
/// Example 10: AI-Powered Long-Term Memory Demo
/// Shows how AI can intelligently extract and persist user information across conversations.
/// </summary>
public class Example04_LongTermMemory
{
    private readonly AzureOpenAISettings _settings;
    private const string MemoryFile = "ai_memory_profile.json";

    public Example04_LongTermMemory(AzureOpenAISettings settings)
    {
        _settings = settings;
    }

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("🤖 AI-POWERED LONG-TERM MEMORY with FILE PERSISTENCE");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();
        Console.WriteLine("Concept: AI intelligently extracts & saves important information!");
        Console.WriteLine($"Memory File: {MemoryFile}");
        Console.WriteLine(new string('=', 70));

        try
        {
            // Create AI client

            AIAgent chatClient = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureCliCredential())
                .GetChatClient(_settings.ModelName)
                .CreateAIAgent();

            //var credential = new DefaultAzureCredential();
            //var openAIClient = new AzureOpenAIClient(
            //    new Uri(_settings.Endpoint),
            //    credential
            //);
            
            //var chatClient = openAIClient.GetChatClient(_settings.ModelName);

            Console.WriteLine("\n🔧 Creating agent with AI-powered memory...");

            // Create AI-powered memory system
            var aiMemory = new AIMemoryExtractor(chatClient, MemoryFile);
            Console.WriteLine("✅ AI memory analyzer initialized");

            // Start with system message that includes any existing profile
            var messages = new List<OpenAI.Chat.ChatMessage>();
            var profileContext = await aiMemory.GetProfileContextAsync();

            messages.Add(new SystemChatMessage(
                "You are a helpful, friendly assistant with long-term memory.\n\n" +
                profileContext +
                "\nWhen you recognize information about the user from their profile:\n" +
                "- Reference it naturally in conversation\n" +
                "- Be enthusiastic when you recognize them\n" +
                "- Provide personalized responses based on what you know\n\n" +
                "Be conversational and warm!"));

            Console.WriteLine("✅ Agent created with AI-powered memory\n");

            Console.WriteLine(new string('=', 70));
            Console.WriteLine("💡 COMMANDS:");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("  • Chat naturally - AI extracts & saves info to file");
            Console.WriteLine("  • 'new' - Create new conversation (test cross-thread memory)");
            Console.WriteLine("  • 'profile' - Show what AI learned about you");
            Console.WriteLine("  • 'quit' - Exit");
            Console.WriteLine(new string('=', 70));

            int conversationNum = 0;

            while (true)
            {
                if (conversationNum == 0 || messages.Count <= 1)
                {
                    conversationNum++;
                    Console.WriteLine($"\n🆕 CONVERSATION #{conversationNum} started\n");
                }

                Console.Write("You: ");
                var userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userInput))
                    continue;

                // Handle commands
                if (userInput.ToLower() == "quit")
                {
                    Console.WriteLine("\n👋 Demo ended!");
                    var profile = await aiMemory.GetUserProfileAsync();
                    if (profile.Any())
                    {
                        Console.WriteLine("\n📊 Final AI-Learned Profile:");
                        foreach (var kvp in profile)
                        {
                            Console.WriteLine($"   • {kvp.Key}: {kvp.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   (No profile data learned)");
                    }
                    break;
                }

                if (userInput.ToLower() == "new")
                {
                    // Start new conversation but keep the profile context
                    messages.Clear();
                    var newProfileContext = await aiMemory.GetProfileContextAsync();
                    messages.Add(new SystemChatMessage(
                        "You are a helpful, friendly assistant with long-term memory.\n\n" +
                        newProfileContext +
                        "\nWhen you recognize information about the user from their profile:\n" +
                        "- Reference it naturally in conversation\n" +
                        "- Be enthusiastic when you recognize them\n" +
                        "- Provide personalized responses based on what you know\n\n" +
                        "Be conversational and warm!"));
                    continue;
                }

                if (userInput.ToLower() == "profile")
                {
                    Console.WriteLine("\n📋 AI-LEARNED PROFILE:");
                    var profile = await aiMemory.GetUserProfileAsync();
                    if (profile.Any())
                    {
                        foreach (var kvp in profile)
                        {
                            Console.WriteLine($"   • {kvp.Key}: {kvp.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   (AI hasn't learned anything about you yet)");
                    }
                    Console.WriteLine();
                    continue;
                }

                // Add user message
                messages.Add(new UserChatMessage(userInput));

                var response = await chatClient.RunAsync(messages);

                var assistentMessage = response.AsChatResponse().Text;

                Console.WriteLine($"\n🤖 Assistant Response:\n{assistentMessage}");

                Console.WriteLine();

                // Add AI response to conversation
                messages.Add(new AssistantChatMessage(assistentMessage));

                // Let AI analyze and extract information
                await aiMemory.AnalyzeAndExtractAsync(userInput);

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            throw;
        }
    }

    private class AIMemoryExtractor
    {
        private readonly AIAgent _chatClient;
        private readonly string _memoryFile;
        private Dictionary<string, string> _userProfile;

        public AIMemoryExtractor(AIAgent chatClient, string memoryFile)
        {
            _chatClient = chatClient;
            _memoryFile = memoryFile;
            _userProfile = new Dictionary<string, string>();
            LoadProfile();
        }


        private void LoadProfile()
        {
            if (File.Exists(_memoryFile))
            {
                try
                {
                    var json = File.ReadAllText(_memoryFile);
                    var data = JsonSerializer.Deserialize<MemoryData>(json);
                    _userProfile = data?.Profile ?? new Dictionary<string, string>();

                    Console.WriteLine($"\n📂 [LOADED MEMORY] from {_memoryFile}");
                    if (_userProfile.Any())
                    {
                        var profileItems = string.Join(", ", _userProfile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                        Console.WriteLine($"   🧠 Restored profile: {profileItems}");
                    }
                    else
                    {
                        Console.WriteLine("   📋 File exists but profile is empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n⚠️  [LOAD ERROR] Could not load {_memoryFile}: {ex.Message}");
                    _userProfile = new Dictionary<string, string>();
                }
            }
            else
            {
                Console.WriteLine($"\n📋 [NEW MEMORY] No existing memory file found");
            }
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                var data = new MemoryData
                {
                    Timestamp = DateTime.Now,
                    Profile = _userProfile
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(_memoryFile, json);
                Console.WriteLine($"   💾 [SAVED TO FILE] {_memoryFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  [SAVE ERROR] Could not save to {_memoryFile}: {ex.Message}");
            }
        }

        public async Task<string> GetProfileContextAsync()
        {
            if (!_userProfile.Any())
                return "";

            var profileText = string.Join("\n", _userProfile.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));

            Console.WriteLine("\n   💭 [INJECTING LONG-TERM MEMORY]");
            var profileItems = string.Join(", ", _userProfile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            Console.WriteLine($"   📋 Profile: {profileItems}\n");

            return $"[USER PROFILE - LONG-TERM MEMORY]:\n{profileText}\n\n" +
                   "IMPORTANT: This is information about the user that persists across all conversations.\n" +
                   "Reference this naturally when relevant, and be enthusiastic when recognizing the user!";
        }

        public async Task<Dictionary<string, string>> GetUserProfileAsync()
        {
            return new Dictionary<string, string>(_userProfile);
        }

        public async Task AnalyzeAndExtractAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Length < 3)
                return;

            Console.WriteLine($"   🤖 [AI ANALYZING]: '{userMessage}'");

            var currentProfileJson = _userProfile.Any()
                ? JsonSerializer.Serialize(_userProfile)
                : "{}";

            var analysisPrompt = $$"""
                Analyze this user message and extract any personal information worth remembering for future conversations.

                User message: "{{userMessage}}"

                Current profile: {{currentProfileJson}}

                Extract ONLY factual information about the user (name, age, profession, preferences, hobbies, etc.).
                Return as JSON format: {"key": "value", "key2": "value2"}
                If nothing important, return empty: {}

                Examples:
                - "My name is Alice" → {"name": "Alice"}
                - "I'm a teacher" → {"profession": "teacher"}
                - "I love pizza and my favorite color is blue" → {"favorite_food": "pizza", "favorite_color": "blue"}
                - "How are you?" → {}

                Extract only NEW or UPDATED information. Be concise with values.
                JSON only, no explanation:
                """;

            try
            {
                var response = await _chatClient.RunAsync(analysisPrompt);

                var aiResponse = response.AsChatResponse().Text;

                if (aiResponse.Contains("{") && aiResponse.Contains("}"))
                {
                    var startIndex = aiResponse.IndexOf('{');
                    var endIndex = aiResponse.LastIndexOf('}') + 1;
                    var jsonStr = aiResponse.Substring(startIndex, endIndex - startIndex);

                    var extracted = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonStr);

                    if (extracted?.Any() == true)
                    {
                        foreach (var kvp in extracted)
                        {
                            var value = kvp.Value.GetString() ?? kvp.Value.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                _userProfile[kvp.Key] = value;
                                Console.WriteLine($"   💾 [AI LEARNED] {kvp.Key} = {value}");
                            }
                        }

                        await SaveProfileAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  [AI EXTRACTION ERROR]: {ex.Message}");
            }
        }

        private class MemoryData
        {
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Profile { get; set; } = new();
        }
    }
}