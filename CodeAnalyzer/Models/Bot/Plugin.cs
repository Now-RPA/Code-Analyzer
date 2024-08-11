namespace CodeAnalyzer.Models.Bot;

public abstract record AbstractPlugin
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PluginId { get; set; }
    public string Version { get; set; } = string.Empty;
}

public record SystemPlugin : AbstractPlugin
{
    public string Signature { get; set; } = string.Empty;
}

public record UserPlugin : AbstractPlugin
{
    public string AssemblyPath { get; set; } = string.Empty;
}