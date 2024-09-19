using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using CodeAnalyzer.Services.Config;

namespace CodeAnalyzer.Services.RuleChecker;

public class DiagnosticsRuleChecker : IRuleChecker
{
    private readonly ConfigReader _config;
    private readonly Dictionary<string, Func<Activity, Rule, RuleCheckResult>> _rules;

    public DiagnosticsRuleChecker()
    {
        _config = ConfigManager.DiagnosticsConfigReader ??
                  throw new InvalidOperationException("CodeQuality config not initialized");
        _rules = new Dictionary<string, Func<Activity, Rule, RuleCheckResult>>
        {
            { "ActivityStartLog", HasStartLog },
            { "ActivityEndLog", HasEndLog },
            { "ExceptionLog", HasExceptionLog },
            { "ActivityErrorHandling", StartsWithErrorHandler },
            { "ErrorPortUtilization", HasErrorPortsUsed },
            { "NonEmptyLogs", HasNonEmptyLogs },
            { "ComponentErrorHandlerComment", HasComponentErrorHandlerComment }
        };
    }

    public string Category => "Diagnostics";

    public List<RuleCheckResult> CheckRules(Process process)
    {
        List<RuleCheckResult> results = [];

        foreach (var activity in process.Activities)
        foreach (var ruleEntry in _rules)
            if (_config.GetParameter(ruleEntry.Key, "Enabled", true))
            {
                var rule = new Rule
                {
                    Category = Category,
                    Name = _config.GetParameter(ruleEntry.Key, "Name", ruleEntry.Key),
                    Description = _config.GetParameter(ruleEntry.Key, "Description", "")
                };
                results.Add(ruleEntry.Value(activity, rule));
            }

        return results;
    }

    private RuleCheckResult HasStartLog(Activity activity, Rule rule)
    {
        var expectedLogMessage =
            _config.GetParameter("ActivityStartLog", "ExpectedLogMessage", "{ActivityName} started");
        var severity = _config.GetParameter("ActivityStartLog", "Severity", "Warn");
        var allowedLogLevels =
            _config.GetStringArrayParameter("ActivityStartLog", "AllowedLogLevels", ["INFO", "DEBUG"]);
        var requireExactMatch = _config.GetParameter("ActivityStartLog", "RequireExactMatch", false);
        var caseSensitive = _config.GetParameter("ActivityStartLog", "CaseSensitive", false);

        var entryPoint = activity.Items.OfType<ExecutableItem>().FirstOrDefault(item => item.Type == "EntryPoint");
        var connection = activity.GetControlConnectionWithSourcePort(entryPoint?.ControlOut?.Id);
        if (connection is null)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "Start node disconnected."
            };

        var firstExecutableItem = activity.GetExecutableItemWithControlInPort(connection.SinkPortId);

        if (firstExecutableItem?.Type == "CatchError")
        {
            var catchErrorItem = firstExecutableItem;
            connection = activity.GetControlConnectionWithSourcePort(catchErrorItem.ControlOut?.Id);
            if (connection is null)
                return new RuleCheckResult
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = ParseSeverity(severity),
                    Comments = "Try port of Try-Catch node disconnected."
                };
            var secondExecutableItem = activity.GetExecutableItemWithControlInPort(connection.SinkPortId);
            if (secondExecutableItem is not null && IsLogWriter(secondExecutableItem))
            {
                if (!allowedLogLevels.Contains(secondExecutableItem.LogMode, StringComparer.OrdinalIgnoreCase))
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments =
                            $"Activity starts with 'Try' block followed by Log but does not match allowed log levels: {string.Join(", ", allowedLogLevels)}."
                    };

                if (MatchesExpectedLogMessage(secondExecutableItem,
                        expectedLogMessage.Replace("{ActivityName}", activity.Name), requireExactMatch, caseSensitive))
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = RuleCheckStatus.Pass,
                        Comments =
                            $"Activity starts with 'Try' block followed by Log: '{secondExecutableItem.LogMessage}'."
                    };
            }
        }

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = ParseSeverity(severity),
            Comments =
                $"Activity should start with 'Try' block followed by Log containing: '{expectedLogMessage.Replace("{ActivityName}", activity.Name)}' with allowed log levels: {string.Join(", ", allowedLogLevels)}."
        };
    }

    private RuleCheckResult HasEndLog(Activity activity, Rule rule)
    {
        var expectedLogMessage =
            _config.GetParameter("ActivityEndLog", "ExpectedLogMessage", "{ActivityName} completed");
        var severity = _config.GetParameter("ActivityEndLog", "Severity", "Warn");
        var allowedLogLevels = _config.GetStringArrayParameter("ActivityEndLog", "AllowedLogLevels", ["INFO", "DEBUG"]);
        var requireExactMatch = _config.GetParameter("ActivityEndLog", "RequireExactMatch", false);
        var caseSensitive = _config.GetParameter("ActivityEndLog", "CaseSensitive", false);

        var exitPoint = activity.Items.OfType<ExecutableItem>().FirstOrDefault(item => item.Type == "ExitPoint");
        var connections = activity.Items.OfType<ControlConnection>()
            .Where(connection => connection.SinkPortId == exitPoint?.ControlIn?.Id)
            .ToList();

        if (connections.Count == 0)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "End node disconnected."
            };

        foreach (var connection in connections)
        {
            var currentExecutableItem = activity.GetExecutableItemWithControlOutPort(connection.SourcePortId);

            if (currentExecutableItem is not null && IsLogWriter(currentExecutableItem) &&
                MatchesExpectedLogMessage(currentExecutableItem,
                    expectedLogMessage.Replace("{ActivityName}", activity.Name), requireExactMatch, caseSensitive) &&
                allowedLogLevels.Contains(currentExecutableItem.LogMode, StringComparer.OrdinalIgnoreCase))
                continue;

            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments =
                    $"Activity paths should end with Log containing: '{expectedLogMessage.Replace("{ActivityName}", activity.Name)}' with allowed log levels: {string.Join(", ", allowedLogLevels)}."
            };
        }

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = RuleCheckStatus.Pass,
            Comments =
                $"Activity paths end with Log containing: '{expectedLogMessage.Replace("{ActivityName}", activity.Name)}'."
        };
    }


    private RuleCheckResult HasExceptionLog(Activity activity, Rule rule)
    {
        var severity = _config.GetParameter("ExceptionLog", "Severity", "Fail");
        var allowedLogLevels =
            _config.GetStringArrayParameter("ExceptionLog", "AllowedLogLevels", ["WARN", "ERROR", "EXCEPTION"]);

        var catchErrorItems = activity.Items.OfType<ExecutableItem>()
            .Where(item => item.Type == "CatchError")
            .ToList();

        if (catchErrorItems.Count == 0)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "No Try-Catch block found."
            };

        foreach (var catchErrorItem in catchErrorItems)
        {
            var errorOutId = catchErrorItem.ErrorOutPortId;
            if (errorOutId.HasValue && errorOutId != Guid.Empty)
            {
                var connection = activity.GetControlConnectionWithSourcePort(errorOutId);
                if (connection is null)
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments = "'On Error' port of 'Try-Catch' not used"
                    };
                var logWriterItem = activity.GetExecutableItemWithControlInPort(connection.SinkPortId);

                if (!IsLogWriter(logWriterItem))
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments = "Log message not used immediately after 'On Error' port of 'Try-Catch'"
                    };
                if (!allowedLogLevels.Contains(logWriterItem?.LogMode, StringComparer.OrdinalIgnoreCase))
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments =
                            $"Incorrect log mode: {logWriterItem?.LogMode} for exception log message. Allowed levels: {string.Join(", ", allowedLogLevels)}"
                    };
            }
        }

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = RuleCheckStatus.Pass,
            Comments =
                "Log used immediately after 'On Error' port of 'Try-Catch' with appropriate log level and content"
        };
    }

    private RuleCheckResult StartsWithErrorHandler(Activity activity, Rule rule)
    {
        var severity = _config.GetParameter("ActivityErrorHandling", "Severity", "Fail");
        var entryPoint = activity.Items.OfType<ExecutableItem>().FirstOrDefault(item => item.Type == "EntryPoint");
        var connection = activity.GetControlConnectionWithSourcePort(entryPoint?.ControlOut?.Id);
        if (connection is null)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "Start node disconnected."
            };

        var firstExecutableItem = activity.GetExecutableItemWithControlInPort(connection.SinkPortId);
        if (firstExecutableItem?.Type == "CatchError")
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = RuleCheckStatus.Pass,
                Comments = "Activity starts with 'Try-Catch' block."
            };

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = ParseSeverity(severity),
            Comments = "Activity should start with 'Try-Catch' block."
        };
    }

    private RuleCheckResult HasErrorPortsUsed(Activity activity, Rule rule)
    {
        var severity = _config.GetParameter("ErrorPortUtilization", "Severity", "Fail");
        var requireErrorMessagePortMapping =
            _config.GetParameter("ErrorPortUtilization", "RequireErrorMessagePortMapping", true);

        var catchErrorItems = activity.Items.OfType<ExecutableItem>()
            .Where(item => item.Type == "CatchError")
            .ToList();

        if (catchErrorItems.Count == 0)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "No Try-Catch block found."
            };

        foreach (var catchErrorItem in catchErrorItems)
        {
            var errorOutId = catchErrorItem.ErrorOutPortId;
            var controlConnection = activity.GetControlConnectionWithSourcePort(errorOutId);
            if (controlConnection is null)
                return new RuleCheckResult
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = ParseSeverity(severity),
                    Comments = "'On Error' port of Try-Catch node disconnected"
                };

            var errorOutPortConnectedItem = activity.GetExecutableItemWithControlInPort(controlConnection.SinkPortId);

            if (errorOutPortConnectedItem is null || errorOutPortConnectedItem.Type == "ExitPoint")
                return new RuleCheckResult
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = ParseSeverity(severity),
                    Comments = "'On Error' port of Try-Catch node connected to invalid node"
                };

            if (requireErrorMessagePortMapping)
            {
                //check if data transformed and saved to variable
                var mappedVariables = catchErrorItem.MappedVariables;
                if (mappedVariables.Count > 0) continue;

                var errorMessageOutId = catchErrorItem.ErrorMessagePortId;
                var dataConnection = activity.GetDataConnectionWithSourcePort(errorMessageOutId);
                if (dataConnection is null)
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments = "'Error Message' port of Try-Catch node disconnected"
                    };
            }
        }

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = RuleCheckStatus.Pass,
            Comments = "On Error and Error Message port of Try-Catch nodes are used correctly"
        };
    }

    private RuleCheckResult HasNonEmptyLogs(Activity activity, Rule rule)
    {
        var severity = _config.GetParameter("NonEmptyLogs", "Severity", "Warn");
        var minLogLength = _config.GetParameter("NonEmptyLogs", "MinLogLength", 10);

        var logItems = activity.Items.OfType<ExecutableItem>()
            .Where(item => item.Type == "LogWriter")
            .ToList();

        if (logItems.Count == 0)
            return new RuleCheckResult
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = ParseSeverity(severity),
                Comments = "No log message found."
            };

        foreach (var logItem in logItems)
            if (logItem?.LogMessage is null || string.IsNullOrWhiteSpace(logItem.LogMessage) ||
                logItem.LogMessage.Length < minLogLength)
                return new RuleCheckResult
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = ParseSeverity(severity),
                    Comments = $"Log message is empty or too short (minimum length: {minLogLength})."
                };

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = RuleCheckStatus.Pass,
            Comments = "Non-empty log messages used."
        };
    }

    private RuleCheckResult HasComponentErrorHandlerComment(Activity activity, Rule rule)
    {
        var severity = _config.GetParameter("ComponentErrorHandlerComment", "Severity", "Warn");

        var executableItems = activity.Items.OfType<ExecutableItem>().ToList();

        foreach (var item in executableItems)
            if (item.OnErrorAction != OnErrorAction.Inherit || item.OnErrorActionAfterRetry != OnErrorAction.Inherit)
            {
                // Check if the component has a comment
                var connection = activity.GetCommentConnectionWithSourcePort(item.CommentPortId);
                if (connection is null)
                    return new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments =
                            $"Component '{(string.IsNullOrEmpty(item.Name) ? item.Type : item.Name)}' has modified error handling property but lacks a comment."
                    };
            }

        return new RuleCheckResult
        {
            Rule = rule,
            Source = $"{activity.RootPath}/{activity.Name}",
            Status = RuleCheckStatus.Pass,
            Comments = "Components with modified error handling have appropriate comments."
        };
    }

    private static bool IsLogWriter(ExecutableItem? item)
    {
        return item?.Type == "LogWriter";
    }

    private static bool MatchesExpectedLogMessage(ExecutableItem? item, string expectedMessage, bool requireExactMatch,
        bool caseSensitive)
    {
        static bool Contains(string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        if (item?.LogMessage == null)
            return false;

        if (requireExactMatch)
            return caseSensitive
                ? item.LogMessage == expectedMessage
                : string.Equals(item.LogMessage, expectedMessage, StringComparison.OrdinalIgnoreCase);

        return caseSensitive
            ? item.LogMessage.Contains(expectedMessage)
            : Contains(item.LogMessage, expectedMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static RuleCheckStatus ParseSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "fail" => RuleCheckStatus.Fail,
            "warn" => RuleCheckStatus.Warn,
            _ => RuleCheckStatus.Warn
        };
    }
}