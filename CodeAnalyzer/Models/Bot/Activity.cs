namespace CodeAnalyzer.Models.Bot;

public record Activity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public List<ActivityVariable> Variables { get; set; } = [];
    public List<AbstractDesignItem> Items { get; set; } = [];
    public OnErrorAction OnErrorAction { get; set; }
    public int MaxRetries { get; set; }
    public int RetryDelay { get; set; }
    public OnErrorAction OnErrorActionAfterRetry { get; set; }
    public string RootPath { get; set; } = string.Empty;

    public CommentConnection? GetCommentConnectionWithSourcePort(Guid? portId)
    {
        return Items.OfType<CommentConnection>()
            .FirstOrDefault(connection => connection.SourcePortId == portId);
    }

    public ControlConnection? GetControlConnectionWithSourcePort(Guid? portId)
    {
        return Items.OfType<ControlConnection>()
            .FirstOrDefault(connection => connection.SourcePortId == portId);
    }

    public DataConnection? GetDataConnectionWithSourcePort(Guid? portId)
    {
        return Items.OfType<DataConnection>()
            .FirstOrDefault(connection => connection.SourcePortId == portId);
    }

    public List<DataConnection> GetDataConnectionsWithSourcePort(Guid? portId)
    {
        return Items.OfType<DataConnection>()
            .Where(connection => connection.SourcePortId == portId)
            .ToList();
    }

    public ControlConnection? GetControlConnectionWithSinkPort(Guid? portId)
    {
        return Items.OfType<ControlConnection>()
            .FirstOrDefault(connection => connection.SinkPortId == portId);
    }

    public List<ControlConnection> GetControlConnectionsWithSinkPort(Guid? portId)
    {
        return Items.OfType<ControlConnection>()
            .Where(connection => connection.SinkPortId == portId)
            .ToList();
    }

    public DataConnection? GetDataControlConnectionWithSinkPort(Guid? portId)
    {
        return Items.OfType<DataConnection>()
            .FirstOrDefault(connection => connection.SinkPortId == portId);
    }

    public ExecutableItem? GetExecutableItemWithControlInPort(Guid? portId)
    {
        return Items.OfType<ExecutableItem>()
            .FirstOrDefault(item => item.ControlIn?.Id == portId);
    }

    public ExecutableItem? GetExecutableItemWithControlOutPort(Guid? portId)
    {
        return Items.OfType<ExecutableItem>()
            .FirstOrDefault(item => item.ControlOut?.Id == portId);
    }
}