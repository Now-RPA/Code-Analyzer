namespace CodeAnalyzer.Models.Rule;

public record Rule
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}