namespace CodeAnalyzer.Models.Bot;

public record Process
{
    public Guid Id { get; set; }
    public Guid StartupActivityId { get; set; }
    public List<SystemPlugin> Plugins { get; set; } = [];
    public List<UserPlugin> UserPlugins { get; set; } = [];
    public List<Activity> Activities { get; set; } = [];
    public List<GlobalVariable> Variables { get; set; } = [];
    public string Description { get; set; } = string.Empty;
}