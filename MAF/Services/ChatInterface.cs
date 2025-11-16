namespace MAF.Services;

public class ChatInterface
{
    public static void PrintWelcomeMessage(string title, string description)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"🤖 DEMO: {title}");
        Console.WriteLine(new string('=', 70));
        
        if (!string.IsNullOrEmpty(description))
        {
            Console.WriteLine();
            Console.WriteLine(description);
        }
        
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("💬 Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();
    }
    
    public static bool ShouldExit(string input)
    {
        var exitCommands = new[] { "quit", "exit", "q", "bye", "stop" };
        return exitCommands.Contains(input.ToLower());
    }
    
    public static string? GetUserInput()
    {
        Console.Write("You: ");
        return Console.ReadLine()?.Trim();
    }
    
    public static void PrintGoodbye()
    {
        Console.WriteLine("\n👋 Goodbye!");
    }
    
    public static void PrintError(string message)
    {
        Console.WriteLine($"\n❌ Error: {message}");
    }
    
    public static void WriteAgentResponseStart()
    {
        Console.Write("Agent: ");
    }
    
    public static void WriteAgentResponseChunk(string text)
    {
        Console.Write(text);
    }
    
    public static void WriteAgentResponseEnd()
    {
        Console.WriteLine();
    }
}