namespace CodeAnalyzer.Models.Bot;

public record GlobalVariable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public record ActivityVariable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ActivityId { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public record MappedVariable
{
    public Guid Id { get; set; }
    public bool IsGlobal { get; set; }
    public DataIn? DataIn { get; set; }
    public DataOut? DataOut { get; set; }
}

public record DataIn
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record DataOut
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}