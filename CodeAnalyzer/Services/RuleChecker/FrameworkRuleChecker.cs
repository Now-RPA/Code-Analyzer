using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using CodeAnalyzer.Services.Config;
using System.Text.RegularExpressions;

namespace CodeAnalyzer.Services.RuleChecker;

public class FrameworkRuleChecker : IRuleChecker
{
    public string Category => "Framework";
    private readonly ConfigReader _config;
    private readonly Dictionary<string, Func<Process, Rule, List<RuleCheckResult>>> _rules;

    public FrameworkRuleChecker()
    {
        _config = ConfigManager.FrameworkConfigReader ??
                  throw new InvalidOperationException("CodeQuality config not initialized");
        _rules = new Dictionary<string, Func<Process, Rule, List<RuleCheckResult>>>
        {
            { "StartupActivity", CheckMainActivity },
            { "FrameworkActivities", CheckFrameworkActivities },
            { "ActivityNamingConvention", CheckActivityNamingConvention },
            { "GlobalVariableNamingConvention", CheckGlobalVariableNamingConvention },
            { "ActivityVariableNamingConvention", CheckActivityVariableNamingConvention },
            { "GlobalVariablePlacement", CheckGlobalVariablePlacement },
            { "ExecutableComponentCount", CheckExecutableComponentCount },
            { "ConnectorGrouping", CheckConnectorGrouping },
            { "QueueUtilization", CheckQueueUtilization },
            { "PickWorkitem", CheckPickWorkitem },
            { "UpdateWorkitem", CheckUpdateWorkitem }
        };
    }

    public List<RuleCheckResult> CheckRules(Process process)
    {
        List<RuleCheckResult> results = [];
        foreach (var ruleEntry in _rules)
        {
            if (_config.GetParameter(ruleEntry.Key, "Enabled", true))
            {
                var rule = new Rule
                {
                    Category = Category,
                    Name = _config.GetParameter(ruleEntry.Key, "Name", ruleEntry.Key),
                    Description = _config.GetParameter(ruleEntry.Key, "Description", "")
                };
                results.AddRange(ruleEntry.Value(process, rule));
            }
        }
        return results;
    }

    private List<RuleCheckResult> CheckMainActivity(Process process, Rule rule)
    {
        var expectedName = _config.GetParameter("StartupActivity", "ExpectedName", "Main");
        var severity = _config.GetParameter("StartupActivity", "Severity", "Fail");

        List<RuleCheckResult> results = [];
        RuleCheckResult ruleCheckResult = new()
        {
            Rule = rule,
            Source = "Activities",
            Status = ParseSeverity(severity),
            Comments = $"Main activity should be present at the root level and marked as startup activity."
        };

        foreach (var activity in process.Activities)
        {
            if (activity.RootPath == "Activities" && activity.Name == expectedName && process.StartupActivityId == activity.Id)
            {
                ruleCheckResult.Status = RuleCheckStatus.Pass;
                ruleCheckResult.Comments = $"{expectedName} activity is present at the root level and is startup activity.";
                break;
            }
        }
        results.Add(ruleCheckResult);
        return results;
    }

    private List<RuleCheckResult> CheckFrameworkActivities(Process process, Rule rule)
    {
        var requiredActivities = _config.GetStringArrayParameter("FrameworkActivities", "RequiredActivities", ["Initialize Workflow", "Get Workitem", "Process Workitem", "Exit Workflow"]);
        var severity = _config.GetParameter("FrameworkActivities", "Severity", "Fail");

        List<RuleCheckResult> results = [];
        foreach (var activityName in requiredActivities)
        {
            string activityPath = $"Activities/Framework/{activityName}";
            string rootPath = Path.GetDirectoryName(activityPath) ?? string.Empty;
            bool activityExists = process.Activities.Any(activity =>
                activity.Name == activityName && NormalizePath(activity.RootPath) == NormalizePath(rootPath));

            RuleCheckResult ruleCheckResult = new()
            {
                Rule = rule,
                Source = $"{activityPath}",
                Status = activityExists ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = activityExists
                    ? $"Activity '{activityPath}' is present."
                    : $"Activity '{activityPath}' is missing."
            };
            results.Add(ruleCheckResult);
        }
        return results;
    }

    private List<RuleCheckResult> CheckActivityNamingConvention(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ActivityNamingConvention", "Severity", "Warn");
        var minLength = _config.GetParameter("ActivityNamingConvention", "MinLength", 3);
        var regexPattern = _config.GetParameter("ActivityNamingConvention", "NamingRegex", "^[a-zA-Z0-9\\s-_]*$");

        Regex regex;
        try
        {
            regex = new Regex(regexPattern);
        }
        catch (ArgumentException)
        {
            // If the regex is invalid, use the default pattern
            regex = ActivityNamingDefaultPattern();
        }

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var activity in process.Activities)
        {
            bool isValidName = regex.IsMatch(activity.Name);
            bool isValidLength = activity.Name.Length >= minLength;
            RuleCheckResult ruleCheckResult = new()
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = isValidName && isValidLength ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = isValidName && isValidLength
                    ? $"The activity '{activity.Name}' follows the naming convention and length requirements."
                    : $"The activity '{activity.Name}' does not meet the naming requirements. It should match the pattern '{regexPattern}' and have a minimum length of {minLength} characters."
            };
            ruleCheckResults.Add(ruleCheckResult);
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckGlobalVariableNamingConvention(Process process, Rule rule)
    {
        var severity = _config.GetParameter("GlobalVariableNamingConvention", "Severity", "Warn");
        var minLength = _config.GetParameter("GlobalVariableNamingConvention", "MinLength", 3);
        var regexPattern = _config.GetParameter("GlobalVariableNamingConvention", "NamingRegex", "^[a-zA-Z_][a-zA-Z0-9_]*$");

        Regex regex;
        try
        {
            regex = new Regex(regexPattern);
        }
        catch (ArgumentException)
        {
            // If the regex is invalid, use the default pattern
            regex = VariableNamingDefaultPattern();
        }

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var variable in process.Variables)
        {
            if (variable.DataType == "AutxVariable")
            {
                bool isValidName = regex.IsMatch(variable.Name);
                bool isValidLength = variable.Name.Length >= minLength;
                RuleCheckResult ruleCheckResult = new()
                {
                    Rule = rule,
                    Source = $"{variable.RootPath}/{variable.Name}",
                    Status = isValidName && isValidLength ? RuleCheckStatus.Pass : ParseSeverity(severity),
                    Comments = isValidName && isValidLength
                        ? $"The global variable '{variable.Name}' follows the naming convention and length requirements."
                        : $"The global variable '{variable.Name}' does not meet the naming requirements. It should match the pattern '{regexPattern}' and have a minimum length of {minLength} characters."
                };
                ruleCheckResults.Add(ruleCheckResult);
            }
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckActivityVariableNamingConvention(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ActivityVariableNamingConvention", "Severity", "Warn");
        var minLength = _config.GetParameter("ActivityVariableNamingConvention", "MinLength", 3);
        var regexPattern = _config.GetParameter("ActivityVariableNamingConvention", "NamingRegex", "^[a-zA-Z_][a-zA-Z0-9_]*$");

        Regex regex;
        try
        {
            regex = new Regex(regexPattern);
        }
        catch (ArgumentException)
        {
            // If the regex is invalid, use the default pattern
            regex = VariableNamingDefaultPattern();
        }

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var activity in process.Activities)
        {
            foreach (var variable in activity.Variables)
            {
                bool isValidName = regex.IsMatch(variable.Name);
                bool isValidLength = variable.Name.Length >= minLength;
                RuleCheckResult ruleCheckResult = new()
                {
                    Rule = rule,
                    Source = $"{variable.RootPath}/{variable.Name}",
                    Status = isValidName && isValidLength ? RuleCheckStatus.Pass : ParseSeverity(severity),
                    Comments = isValidName && isValidLength
                        ? $"The activity variable '{variable.Name}' follows the naming convention and length requirements."
                        : $"The activity variable '{variable.Name}' does not meet the naming requirements. It should match the pattern '{regexPattern}' and have a minimum length of {minLength} characters."
                };
                ruleCheckResults.Add(ruleCheckResult);
            }
        }
        return ruleCheckResults;
    }
    private List<RuleCheckResult> CheckGlobalVariablePlacement(Process process, Rule rule)
    {
        var severity = _config.GetParameter("GlobalVariablePlacement", "Severity", "Warn");
        var expectedPath = _config.GetParameter("GlobalVariablePlacement", "ExpectedPath", "Global Objects/Variables");

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var variable in process.Variables)
        {
            if (variable.DataType == "AutxVariable")
            {
                bool isValidPlacement = variable.RootPath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase) ||
                                        variable.RootPath.StartsWith(expectedPath + "\\", StringComparison.OrdinalIgnoreCase);
                RuleCheckResult ruleCheckResult = new()
                {
                    Rule = rule,
                    Source = $"{variable.RootPath}/{variable.Name}",
                    Status = isValidPlacement ? RuleCheckStatus.Pass : ParseSeverity(severity),
                    Comments = isValidPlacement
                        ? $"The global variable '{variable.Name}' is placed under the correct folder."
                        : $"The global variable '{variable.Name}' should be placed under '{expectedPath}' or its subfolders.",
                };
                ruleCheckResults.Add(ruleCheckResult);
            }
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckExecutableComponentCount(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ExecutableComponentCount", "Severity", "Fail");
        var maxCount = _config.GetParameter("ExecutableComponentCount", "MaxCount", 30);

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var activity in process.Activities)
        {
            int executableComponentCount = activity.Items.OfType<ExecutableItem>().Count();
            bool isValidCount = executableComponentCount <= maxCount;
            RuleCheckResult ruleCheckResult = new()
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = isValidCount ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = isValidCount
                    ? $"The activity '{activity.Name}' has {executableComponentCount} executable components, which is within the limit."
                    : $"The activity '{activity.Name}' has {executableComponentCount} executable components, exceeding the limit of {maxCount}.",
            };
            ruleCheckResults.Add(ruleCheckResult);
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckConnectorGrouping(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ConnectorGrouping", "Severity", "Warn");
        var expectedPath = _config.GetParameter("ConnectorGrouping", "ExpectedPath", "Global Objects/{ConnectorType}");

        List<RuleCheckResult> ruleCheckResults = [];
        foreach (var variable in process.Variables)
        {
            if (variable.DataType != "AutxVariable")
            {
                string connectorType = GetConnectorType(variable.DataType);
                string expectedConnectorPath = expectedPath.Replace("{ConnectorType}", connectorType);
                bool isValidGrouping = variable.RootPath.Equals(expectedConnectorPath, StringComparison.OrdinalIgnoreCase) ||
                                       variable.RootPath.StartsWith(expectedConnectorPath + "\\", StringComparison.OrdinalIgnoreCase);
                RuleCheckResult ruleCheckResult = new()
                {
                    Rule = rule,
                    Source = $"{variable.RootPath}/{variable.Name}",
                    Status = isValidGrouping ? RuleCheckStatus.Pass : ParseSeverity(severity),
                    Comments = isValidGrouping
                        ? $"The connector '{variable.Name}' is grouped under the correct folder."
                        : $"The connector '{variable.Name}' should be grouped under '{expectedConnectorPath}' or its subfolders.",
                };
                ruleCheckResults.Add(ruleCheckResult);
            }
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckQueueUtilization(Process process, Rule rule)
    {
        var severity = _config.GetParameter("QueueUtilization", "Severity", "Fail");
        var expectedType = _config.GetParameter("QueueUtilization", "ExpectedType", "UTL.RPA.CONNECTORS.AutxQueue");

        bool queueVariableExists = process.Variables.Any(v => v.DataType == expectedType);
        RuleCheckResult ruleCheckResult = new()
        {
            Rule = rule,
            Source = "Global Objects",
            Status = queueVariableExists ? RuleCheckStatus.Pass : ParseSeverity(severity),
        };
        if (queueVariableExists)
        {
            var queueConnector = process.Variables.First(v => v.DataType == expectedType);
            ruleCheckResult.Comments = $"Queue connector {queueConnector.Name} found for transaction tracking.";
        }
        else
        {
            ruleCheckResult.Comments = "No queue connector found for transaction tracking.";
        }
        return [ruleCheckResult];
    }

    private List<RuleCheckResult> CheckPickWorkitem(Process process, Rule rule)
    {
        var severity = _config.GetParameter("PickWorkitem", "Severity", "Warn");
        var expectedName = _config.GetParameter("PickWorkitem", "ExpectedName", "PickWorkitem");

        List<RuleCheckResult> ruleCheckResults = [];
        Activity? getWorkitemActivity = process.Activities.FirstOrDefault(a => a.Name == "Get Workitem" && a.RootPath == "Activities/Framework");
        if (getWorkitemActivity == null)
        {
            ruleCheckResults.Add(new RuleCheckResult
            {
                Rule = rule,
                Source = "Activities",
                Status = ParseSeverity(severity),
                Comments = "'Activities/Framework/Get Workitem' activity is missing.",
            });
        }
        else
        {
            bool pickWorkitemExists = getWorkitemActivity.Items.OfType<ExecutableItem>().Any(i => i.Name == expectedName);
            ruleCheckResults.Add(new RuleCheckResult
            {
                Rule = rule,
                Source = $"{getWorkitemActivity.RootPath}/{getWorkitemActivity.Name}",
                Status = pickWorkitemExists ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = pickWorkitemExists
                    ? $"'{expectedName}' action is used in the 'Get Workitem' activity."
                    : $"'{expectedName}' action is missing in the 'Get Workitem' activity.",
            });
        }
        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckUpdateWorkitem(Process process, Rule rule)
    {
        var severity = _config.GetParameter("UpdateWorkitem", "Severity", "Fail");
        var expectedName = _config.GetParameter("UpdateWorkitem", "ExpectedName", "UpdateWorkitem");

        List<RuleCheckResult> ruleCheckResults = [];
        Activity? processWorkitemActivity = process.Activities.FirstOrDefault(a => a.Name == "Process Workitem" && a.RootPath == "Activities/Framework");
        if (processWorkitemActivity == null)
        {
            ruleCheckResults.Add(new RuleCheckResult
            {
                Rule = rule,
                Source = "Activities",
                Status = ParseSeverity(severity),
                Comments = "'Activities/Framework/Process Workitem' activity is missing.",
            });
        }
        else
        {
            bool updateWorkitemExists = processWorkitemActivity.Items.OfType<ExecutableItem>().Any(i => i.Name == expectedName);
            ruleCheckResults.Add(new RuleCheckResult
            {
                Rule = rule,
                Source = $"{processWorkitemActivity.RootPath}/{processWorkitemActivity.Name}",
                Status = updateWorkitemExists ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = updateWorkitemExists
                    ? $"The '{expectedName}' action is used in the 'Process Workitem' activity."
                    : $"The '{expectedName}' action is missing in the 'Process Workitem' activity.",
            });
        }
        return ruleCheckResults;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
    }

    public static string GetConnectorType(string dataType)
    {
        const string prefix = "autx";
        int lastIndex = dataType.ToLower().LastIndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (lastIndex != -1)
        {
            string result = dataType.Substring(lastIndex + prefix.Length);
            return result.Trim();
        }
        return dataType;
    }

    private static RuleCheckStatus ParseSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "fail" => RuleCheckStatus.Fail,
            "warn" => RuleCheckStatus.Warn,
            _ => RuleCheckStatus.Warn,
        };
    }

    private static readonly Regex ActivityNamingDefaultPatternRegex = new("^[a-zA-Z0-9\\s-_]*$", RegexOptions.Compiled);
    private static readonly Regex VariableNamingDefaultPatternRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static Regex ActivityNamingDefaultPattern() => ActivityNamingDefaultPatternRegex;
    private static Regex VariableNamingDefaultPattern() => VariableNamingDefaultPatternRegex;
}