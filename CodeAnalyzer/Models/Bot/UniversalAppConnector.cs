namespace CodeAnalyzer.Models.Bot;

public record UniversalAppConnector : GlobalVariable
{
    public Guid ProcessId { get; set; }
    public bool IsRemoteExecutionEnabled { get; set; }
    public string IsolationPlatform { get; set; } = string.Empty;
    public string IsolationSessionType { get; set; } = string.Empty;
    public List<BaseScreen> Screens { get; set; } = [];
}

public abstract record BaseScreen
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public List<BaseMatchRule> MatchRules { get; set; } = [];
    public List<ScreenElement> Elements { get; set; } = [];
    public List<Locator> Locators { get; set; } = [];
    public Locator? SelectedLocator { get; set; }
}

public record ChromeConnectorScreen : BaseScreen
{
    public string BrowserType { get; set; } = string.Empty;
}

public record WindowsConnectorScreen : BaseScreen;

public record GenericScreen : BaseScreen;

public record ScreenElement
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public List<BaseMatchRule> MatchRules { get; set; } = [];
    public List<JsMatchRule> JsMatchRules { get; set; } = [];
    public List<Locator> Locators { get; set; } = [];
    public Locator? SelectedLocator { get; set; }
    public string MatchCriteria { get; set; } = string.Empty;
}

public abstract record BaseMatchRule
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public string Type { get; set; } = string.Empty;
}

public record StringComparerMatchRule : BaseMatchRule
{
    public StringCompare? Comparer { get; set; }
}

public record IndexMatchRule : BaseMatchRule
{
    public int Index { get; set; }
}

public record ElementMatchRule : BaseMatchRule
{
    public string ElementId { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
}

public record GenericMatchRule : BaseMatchRule;

public record JsMatchRule
{
    public Guid Id { get; set; }
    public Guid JId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Comparer { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IgnoreCase { get; set; }
    public bool Escape { get; set; }
    public bool Trim { get; set; }
    public bool Enabled { get; set; }
}

public record StringCompare
{
    public string ComparisonValue { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public record Locator
{
    public Guid Id { get; set; }
    public string LocateBy { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Selected { get; set; }
}