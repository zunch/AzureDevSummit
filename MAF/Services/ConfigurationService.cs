using MAF.Configuration;
using Microsoft.Extensions.Configuration;

namespace MAF.Services;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public AzureAISettings GetAzureAISettings()
    {
        var settings = new AzureAISettings();
        _configuration.GetSection(AzureAISettings.SectionName).Bind(settings);
        ValidateAzureAISettings(settings);
        return settings;
    }
    
    public AzureOpenAISettings GetAzureOpenAISettings()
    {
        var settings = new AzureOpenAISettings();
        _configuration.GetSection(AzureOpenAISettings.SectionName).Bind(settings);
        ValidateAzureOpenAISettings(settings);
        return settings;
    }
    
    private static void ValidateAzureAISettings(AzureAISettings settings)
    {
        var missingSettings = new List<string>();
        
        if (string.IsNullOrWhiteSpace(settings.ProjectEndpoint))
            missingSettings.Add("ProjectEndpoint");
        if (string.IsNullOrWhiteSpace(settings.ModelDeploymentName))
            missingSettings.Add("ModelDeploymentName");
        
        if (missingSettings.Any())
        {
            throw new InvalidOperationException(
                $"Missing Azure AI configuration: {string.Join(", ", missingSettings)}. " +
                "Please check your appsettings.json file.");
        }
    }
    
    private static void ValidateAzureOpenAISettings(AzureOpenAISettings settings)
    {
        var missingSettings = new List<string>();
        
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            missingSettings.Add("Endpoint");
        if (string.IsNullOrWhiteSpace(settings.ModelName))
            missingSettings.Add("ChatDeploymentName");
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            missingSettings.Add("ApiKey");
        
        if (missingSettings.Any())
        {
            throw new InvalidOperationException(
                $"Missing Azure OpenAI configuration: {string.Join(", ", missingSettings)}. " +
                "Please check your appsettings.json file.");
        }
    }
}