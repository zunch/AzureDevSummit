namespace MAF.Configuration;

public class AzureAISettings
{
    public const string SectionName = "AzureAI";
    
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string VectorStoreId { get; set; } = string.Empty;
}

public class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";
    
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-07-01-preview";
}

public class  GitHubMCPSettings
{
    public const string SectionName = "GitHubMCP";

    public string GitHubPersonalAccessToken { get; set; } = string.Empty;

}