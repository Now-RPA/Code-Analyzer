namespace CodeAnalyzer.Models.Rule;

public enum RuleCheckStatus
{
    Pass,
    Fail,
    Warn
}

public record RuleCheckResult
{
    public Rule Rule { get; set; } = new();
    public RuleCheckStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}