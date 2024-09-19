namespace CodeAnalyzer.Models.Bot;

public record ControlIn
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Visibility { get; set; }
    public bool AllowDelete { get; set; }
}

public record ControlOut
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Visibility { get; set; }
    public bool AllowDelete { get; set; }
}