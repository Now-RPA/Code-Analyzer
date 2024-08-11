namespace CodeAnalyzer.Models.Rule;

public enum RuleCheckStatus
{
    Pass,
    Fail,
    Warn
}

public record RuleCheckResult
{

    public Rule Rule { get; set; } = new Rule();
    public RuleCheckStatus Status { get; set; }
    public string Source { get; set; } = String.Empty;
    public string Comments { get; set; } = String.Empty;
}