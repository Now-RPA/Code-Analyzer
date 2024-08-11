namespace CodeAnalyzer.Models.Bot;

public abstract record AbstractDesignItem
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string Type { get; set; } = string.Empty;
}

public record GenericDesignItem : AbstractDesignItem
{
    public string? Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public record ExecutableItem : AbstractDesignItem
{
    public string? Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool Breakpoint { get; set; }
    public ControlIn? ControlIn { get; set; }
    public ControlOut? ControlOut { get; set; }
    public OnErrorAction OnErrorAction { get; set; }
    public int MaxRetries { get; set; }
    public int RetryDelay { get; set; }
    public OnErrorAction OnErrorActionAfterRetry { get; set; }
    public int BeforeDelay { get; set; }
    public int AfterDelay { get; set; }
    public bool EnableTimeout { get; set; }
    public int Timeout { get; set; }
    public Guid CommentPortId { get; set; }
    public string? ClassName { get; set; }
    public string? MethodName { get; set; }
    public Guid? ObjectId { get; set; }
    public Guid? ErrorOutPortId { get; set; }
    public Guid? ErrorMessagePortId { get; set; }
    public string? LogMessage { get; set; }
    public string? LogMode { get; set; }
    public List<DataTransform> DataTransforms { get; set; } = [];
    public List<MappedVariable> MappedVariables { get; set; } = [];
}

public record ControlConnection : AbstractDesignItem
{
    public Guid SourceComponentId { get; set; }
    public Guid SourcePortId { get; set; }
    public Guid SinkComponentId { get; set; }
    public Guid SinkPortId { get; set; }
}

public record DataConnection : AbstractDesignItem
{
    public Guid SourceComponentId { get; set; }
    public Guid SourcePortId { get; set; }
    public Guid SinkComponentId { get; set; }
    public Guid SinkPortId { get; set; }
}

public record CommentConnection : AbstractDesignItem
{
    public Guid SourceComponentId { get; set; }
    public Guid SourcePortId { get; set; }
    public Guid SinkComponentId { get; set; }
    public Guid SinkPortId { get; set; }
}
public record DataTransform
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public string Script { get; set; } = string.Empty;
    public string ScriptLanguage { get; set; } = string.Empty;
    public string AttributeType { get; set; } = string.Empty;
    public bool HasModifiedScript => !string.IsNullOrEmpty(Script) && !Script.Equals("Return Value");
}