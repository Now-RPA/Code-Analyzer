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

    public CommentConnection? GetCommentConnectionWithSourceComponent(Guid? sourceComponentId, Guid? sourcePortId)
    {
        return Items.OfType<CommentConnection>()
            .FirstOrDefault(connection => connection.SourceComponentId == sourceComponentId &&
                                          connection.SourcePortId == sourcePortId);
    }

    public ControlConnection? GetControlConnectionWithSourceComponent(Guid? sourceComponentId, Guid? sourcePortId)
    {
        return Items.OfType<ControlConnection>()
            .FirstOrDefault(connection => connection.SourceComponentId == sourceComponentId &&
                                          connection.SourcePortId == sourcePortId);
    }

    public DataConnection? GetDataConnectionWithSourceComponent(Guid? sourceComponentId, Guid? sourcePortId)
    {
        return Items.OfType<DataConnection>()
            .FirstOrDefault(connection => connection.SourceComponentId == sourceComponentId &&
                                          connection.SourcePortId == sourcePortId);
    }

    public List<ControlConnection> GetControlConnectionsWithSinkComponent(Guid? sinkComponentId, Guid? sinkPortId)
    {
        return Items.OfType<ControlConnection>()
            .Where(connection => connection.SinkComponentId == sinkComponentId &&
                                 connection.SinkPortId == sinkPortId)
            .ToList();
    }

    public ExecutableItem? GetExecutableItemWithControlInPort(Guid? itemId, Guid? itemControlInPortId)
    {
        return Items.OfType<ExecutableItem>()
            .FirstOrDefault(item => item.Id == itemId &&
                                    item.ControlIn?.Id == itemControlInPortId);
    }

    public ExecutableItem? GetExecutableItemWithControlOutPort(Guid? itemId, Guid? itemControlOutPortId)
    {
        return Items.OfType<ExecutableItem>()
            .FirstOrDefault(item => item.Id == itemId &&
                                    item.ControlOut?.Id == itemControlOutPortId);
    }
}