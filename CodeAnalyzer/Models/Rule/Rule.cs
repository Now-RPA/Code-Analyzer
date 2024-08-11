namespace CodeAnalyzer.Models.Rule;

public record Rule
{
    public string Category { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;

}