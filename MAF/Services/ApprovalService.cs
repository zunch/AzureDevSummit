namespace MAF.Services;

public class ApprovalService
{
    public static bool RequestApproval(string functionName, Dictionary<string, object> arguments)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("🚨 APPROVAL REQUIRED");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"📝 Function: {functionName}");
        Console.WriteLine("📊 Arguments:");
        
        foreach (var arg in arguments)
        {
            Console.WriteLine($"   - {arg.Key}: {arg.Value}");
        }
        
        Console.WriteLine(new string('-', 70));
        
        while (true)
        {
            Console.Write("⚠️ Do you want to APPROVE this action? (yes/no): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            
            if (response is "yes" or "y")
                return true;
            if (response is "no" or "n")
                return false;
            
            Console.WriteLine("   Please enter 'yes' or 'no'");
        }
    }
}