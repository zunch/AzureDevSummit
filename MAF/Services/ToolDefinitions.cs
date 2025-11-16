using System.ComponentModel;
using System.Text.Json;

namespace MAF.Services;

public static class ToolDefinitions
{
    [Description("Evaluate a mathematical expression")]
    public static string Calculate(
        [Description("Mathematical expression to evaluate, e.g. '2 + 2' or '10 * 5'")] string expression)
    {
        try
        {
            // Basic safe evaluation - only allow basic math operations
            var sanitized = expression.Replace(" ", "");
            
            // Simple validation to prevent code injection
            if (sanitized.Any(c => !char.IsDigit(c) && !"+-*/().".Contains(c)))
            {
                return $"Error: Invalid characters in expression '{expression}'";
            }
            
            // Use DataTable.Compute for safe evaluation
            var table = new System.Data.DataTable();
            var result = table.Compute(expression, null);
            
            return $"Result: {result}";
        }
        catch (Exception ex)
        {
            return $"Error: Could not calculate '{expression}' - {ex.Message}";
        }
    }
    
    [Description("Get current weather for a location")]
    public static string GetWeather(
        [Description("City name")] string location)
    {
        // Mock weather data
        var weatherData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["london"] = "🌧️ 15°C, Rainy",
            ["paris"] = "☀️ 22°C, Sunny", 
            ["tokyo"] = "⛅ 18°C, Partly Cloudy",
            ["new york"] = "🌤️ 20°C, Clear",
            ["stockholm"] = "❄️ 2°C, Snow",
            ["madrid"] = "☀️ 25°C, Sunny"
        };
        
        return weatherData.TryGetValue(location, out var weather) 
            ? weather 
            : $"Weather data not available for {location}";
    }
    
    [Description("Get current time in a timezone")]
    public static async Task<string> GetTimeAsync(
        [Description("Timezone like 'America/New_York' or 'Europe/London'")] string timezone)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await client.GetAsync($"http://worldtimeapi.org/api/timezone/{timezone}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (data.TryGetProperty("datetime", out var datetime))
                {
                    var time = datetime.GetString()?.Split('T')[1].Split('.')[0];
                    return $"⏰ Current time in {timezone}: {time}";
                }
            }
            
            return $"Could not get time for {timezone}";
        }
        catch (Exception ex)
        {
            return $"Error getting time for {timezone}: {ex.Message}";
        }
    }
    
    [Description("Create a new file with content")]
    public static string CreateFile(
        [Description("Name of file to create")] string filename,
        [Description("Content to write in file")] string content)
    {
        try
        {
            // Sanitize filename to prevent path traversal
            filename = Path.GetFileName(filename);

            var demoDir = "C:/demo_files";

            // Try to create directory if it doesn't exist
            if (!Directory.Exists(demoDir))
            {
                Directory.CreateDirectory(demoDir);
            }
            
            var filePath = Path.Combine(demoDir, filename);
            
            // Check if file is locked by another process
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (UnauthorizedAccessException)
            {
                return $"❌ Access denied: Cannot write to '{filename}'. File may be locked or insufficient permissions.";
            }
            catch (IOException ioEx)
            {
                return $"❌ IO Error: {ioEx.Message}. File may be locked by another process.";
            }
            
            return $"✅ File '{filename}' created successfully with {content.Length} characters at {Path.GetFullPath(filePath)}";
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return $"❌ Access denied: {uaEx.Message}. Try running as administrator or check folder permissions.";
        }
        catch (Exception ex)
        {
            return $"❌ Error creating file: {ex.Message}";
        }
    }
    
    [Description("Delete a file from the demo directory")]
    public static string DeleteFile(
        [Description("Name of file to delete")] string filename)
    {
        try
        {
            // Sanitize filename to prevent path traversal
            filename = Path.GetFileName(filename);

            var demoDir = "C:/demo_files";
            var filePath = Path.Combine(demoDir, filename);
            
            if (!File.Exists(filePath))
            {
                return $"⚠️ File '{filename}' not found in demo directory";
            }
            
            try
            {
                File.Delete(filePath);
                return $"🗑️ File '{filename}' deleted successfully";
            }
            catch (UnauthorizedAccessException)
            {
                return $"❌ Access denied: Cannot delete '{filename}'. File may be locked or insufficient permissions.";
            }
            catch (IOException ioEx)
            {
                return $"❌ IO Error: {ioEx.Message}. File may be in use by another process.";
            }
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return $"❌ Access denied: {uaEx.Message}. Try running as administrator or check folder permissions.";
        }
        catch (Exception ex)
        {
            return $"❌ Error deleting file: {ex.Message}";
        }
    }
}