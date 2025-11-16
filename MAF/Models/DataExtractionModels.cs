using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MAF.Models;

public class PersonInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("age")]
    [Range(0, 150)]
    public int? Age { get; set; }
    
    [JsonPropertyName("occupation")]
    public string? Occupation { get; set; }
    
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    public Dictionary<string, string> ToDisplayDictionary()
    {
        return new Dictionary<string, string>
        {
            ["Name"] = Name ?? "Not specified",
            ["Age"] = Age?.ToString() ?? "Not specified",
            ["Occupation"] = Occupation ?? "Not specified",
            ["City"] = City ?? "Not specified"
        };
    }
    
    public bool HasAnyData()
    {
        return !string.IsNullOrEmpty(Name) || Age.HasValue || 
               !string.IsNullOrEmpty(Occupation) || !string.IsNullOrEmpty(City);
    }
}

public class CompanyInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("industry")]
    public string? Industry { get; set; }
    
    [JsonPropertyName("founded_year")]
    [Range(1800, 2025)]
    public int? FoundedYear { get; set; }
    
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    
    [JsonPropertyName("employees")]
    [Range(0, int.MaxValue)]
    public int? Employees { get; set; }
    
    public Dictionary<string, string> ToDisplayDictionary()
    {
        return new Dictionary<string, string>
        {
            ["Company Name"] = Name ?? "Not specified",
            ["Industry"] = Industry ?? "Not specified",
            ["Founded"] = FoundedYear?.ToString() ?? "Not specified",
            ["Location"] = Location ?? "Not specified",
            ["Employees"] = Employees?.ToString() ?? "Not specified"
        };
    }
    
    public bool HasAnyData()
    {
        return !string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(Industry) || 
               FoundedYear.HasValue || !string.IsNullOrEmpty(Location) || Employees.HasValue;
    }
}

public class ProductInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("price")]
    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }
    
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    public Dictionary<string, string> ToDisplayDictionary()
    {
        var priceStr = Price.HasValue ? $"${Price:F2}" : "Not specified";
        return new Dictionary<string, string>
        {
            ["Product Name"] = Name ?? "Not specified",
            ["Category"] = Category ?? "Not specified",
            ["Price"] = priceStr,
            ["Brand"] = Brand ?? "Not specified",
            ["Description"] = Description ?? "Not specified"
        };
    }
    
    public bool HasAnyData()
    {
        return !string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(Category) || 
               Price.HasValue || !string.IsNullOrEmpty(Brand) || !string.IsNullOrEmpty(Description);
    }
}