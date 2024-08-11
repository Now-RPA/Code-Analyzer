namespace CodeAnalyzer.Models.Bot;

public enum OnErrorAction
{
    Stop,
    Continue,
    Retry,
    Inherit
}

public record ErrorHandler
{
    public OnErrorAction OnErrorAction { get; set; }

    public int MaxRetries { get; set; }

    public int RetryDelay { get; set; }

    public OnErrorAction OnErrorActionAfterRetry { get; set; }
}