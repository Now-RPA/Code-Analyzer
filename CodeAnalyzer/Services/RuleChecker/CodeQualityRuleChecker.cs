using System.Text.RegularExpressions;
using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using CodeAnalyzer.Services.Config;

namespace CodeAnalyzer.Services.RuleChecker;

public class CodeQualityRuleChecker : IRuleChecker
{
    private static readonly Regex HasDigitRegex = new(@"\d", RegexOptions.Compiled);
    private readonly ConfigReader _config;
    private readonly Dictionary<string, Func<Process, Rule, List<RuleCheckResult>>> _rules;

    public CodeQualityRuleChecker()
    {
        _config = ConfigManager.CodeQualityConfigReader ??
                  throw new InvalidOperationException("CodeQuality config not initialized");
        _rules = new Dictionary<string, Func<Process, Rule, List<RuleCheckResult>>>
        {
            { "OpenCloseMethodPair", CheckOpenCloseConnectorMethods },
            { "HardcodedDelay", CheckHardcodedDelays },
            { "ModifiedDelayProperties", CheckModifiedDelayProperties },
            { "Comments", CheckNonEmptyComments },
            { "WindowsScreenRules", CheckWindowsConnectorScreenRules },
            { "WindowsElementRules", CheckWindowsScreenElementLocatorsAndMatchRules },
            { "ChromeScreenRules", CheckChromeConnectorScreenRules },
            { "ChromeElementRules", CheckChromeScreenElementLocatorsAndJsMatchRules },
            { "DataTransformUsage", CheckDataTransformUsage }
        };
    }

    public string Category => "Code Quality";

    public List<RuleCheckResult> CheckRules(Process process)
    {
        List<RuleCheckResult> results = [];
        foreach (var ruleEntry in _rules)
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

        return results;
    }

    private List<RuleCheckResult> CheckOpenCloseConnectorMethods(Process process, Rule rule)
    {
        var severity = _config.GetParameter("OpenCloseMethodPair", "Severity", "Warn");
        var openMethodPrefixes =
            _config.GetStringArrayParameter("OpenCloseMethodPair", "OpenMethodPrefixes", ["Open", "Load", "SetAccount"]);
        var closeMethodPrefixes =
            _config.GetStringArrayParameter("OpenCloseMethodPair", "CloseMethodPrefixes", ["Close"]);

        List<RuleCheckResult> ruleCheckResults = [];

        foreach (var activity in process.Activities)
        {
            var openMethods = activity.Items.OfType<ExecutableItem>()
                .Where(item => item.Type.Equals("AutxMethod") && item.MethodName != null && openMethodPrefixes.Any(
                    prefix =>
                        item.MethodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var closeMethods = activity.Items.OfType<ExecutableItem>()
                .Where(item => item.Type.Equals("AutxMethod") && item.MethodName != null && closeMethodPrefixes.Any(
                    prefix =>
                        item.MethodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Check for Open methods without corresponding Close methods
            foreach (var openMethod in openMethods)
            {
                var connectorInfo = GetConnectorInfo(openMethod.ObjectId);
                var hasMatchingCloseMethod = closeMethods.Any(item => item.ObjectId == openMethod.ObjectId);
                RuleCheckResult ruleCheckResult = new()
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = hasMatchingCloseMethod ? RuleCheckStatus.Pass : ParseSeverity(severity),
                    Comments = hasMatchingCloseMethod
                        ? $"Activity '{activity.Name}' has a matching 'Close' method for the 'Open' method of {connectorInfo}."
                        : $"Activity '{activity.Name}' is missing a 'Close' method for the 'Open' method of {connectorInfo}."
                };
                ruleCheckResults.Add(ruleCheckResult);
            }

            // Check for Close methods without corresponding Open methods
            foreach (var closeMethod in closeMethods)
            {
                var connectorInfo = GetConnectorInfo(closeMethod.ObjectId);
                var hasMatchingOpenMethod = openMethods.Any(item => item.ObjectId == closeMethod.ObjectId);
                if (!hasMatchingOpenMethod)
                {
                    RuleCheckResult ruleCheckResult = new()
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments =
                            $"Activity '{activity.Name}' has a 'Close' method without a corresponding 'Open' method for {connectorInfo}."
                    };
                    ruleCheckResults.Add(ruleCheckResult);
                }
            }
        }

        return ruleCheckResults;

        string GetConnectorInfo(Guid? objectId)
        {
            var connector = process.Variables.FirstOrDefault(v => v.Id == objectId);
            return connector != null ? $"{connector.RootPath}/{connector.Name}" : $"Unknown connector (ID: {objectId})";
        }
    }

    private List<RuleCheckResult> CheckHardcodedDelays(Process process, Rule rule)
    {
        var severity = _config.GetParameter("HardcodedDelay", "Severity", "Fail");
        var prohibitedTypes = _config.GetStringArrayParameter("HardcodedDelay", "ProhibitedTypes", ["WaitForTime"]);

        List<RuleCheckResult> ruleCheckResults = [];

        foreach (var activity in process.Activities)
        {
            var hasHardcodedDelay = activity.Items.OfType<ExecutableItem>()
                .Any(item => prohibitedTypes.Contains(item.Type));

            RuleCheckResult ruleCheckResult = new()
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = hasHardcodedDelay ? ParseSeverity(severity) : RuleCheckStatus.Pass,
                Comments = hasHardcodedDelay
                    ? $"Activity '{activity.Name}' uses a hardcoded delay ({string.Join(", ", prohibitedTypes)})."
                    : $"Activity '{activity.Name}' does not use hardcoded delays."
            };

            ruleCheckResults.Add(ruleCheckResult);
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckModifiedDelayProperties(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ModifiedDelayProperties", "Severity", "Fail");
        var allowedBeforeDelay = _config.GetParameter("ModifiedDelayProperties", "AllowedBeforeDelay", 0);
        var allowedAfterDelay = _config.GetParameter("ModifiedDelayProperties", "AllowedAfterDelay", 0);
        var allowedEnableTimeout = _config.GetParameter("ModifiedDelayProperties", "AllowedEnableTimeout", false);

        List<RuleCheckResult> ruleCheckResults = [];

        foreach (var activity in process.Activities)
        {
            foreach (var designItem in activity.Items.OfType<ExecutableItem>())
            {
                var isModified = designItem.BeforeDelay > allowedBeforeDelay ||
                                 designItem.AfterDelay > allowedAfterDelay ||
                                 designItem.EnableTimeout != allowedEnableTimeout;
                var name = string.IsNullOrWhiteSpace(designItem.Name) ? designItem.Type : designItem.Name;
                if (isModified)
                {
                    RuleCheckResult ruleCheckResult = new()
                    {
                        Rule = rule,
                        Source = $"{activity.RootPath}/{activity.Name}",
                        Status = ParseSeverity(severity),
                        Comments = $"{name} in activity '{activity.Name}' has modified delay properties: " +
                                   $"AfterDelay={designItem.AfterDelay}, " +
                                   $"BeforeDelay={designItem.BeforeDelay}, " +
                                   $"EnableTimeout={designItem.EnableTimeout}."
                    };
                    ruleCheckResults.Add(ruleCheckResult);
                }
            }

            if (ruleCheckResults.All(r => !r.Source.StartsWith($"{activity.RootPath}/{activity.Name}")))
            {
                RuleCheckResult passResult = new()
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = RuleCheckStatus.Pass,
                    Comments = $"All DesignItems in activity '{activity.Name}' have default delay properties."
                };
                ruleCheckResults.Add(passResult);
            }
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckNonEmptyComments(Process process, Rule rule)
    {
        var severity = _config.GetParameter("Comments", "Severity", "Fail");
        var minCommentLength = _config.GetParameter("Comments", "MinCommentLength", 3);

        List<RuleCheckResult> ruleCheckResults = [];

        foreach (var activity in process.Activities)
        {
            var commentItems = activity.Items.OfType<GenericDesignItem>()
                .Where(item => item.Type == "CommentBox")
                .ToList();

            if (commentItems.Count == 0)
            {
                ruleCheckResults.Add(new RuleCheckResult
                {
                    Rule = rule,
                    Source = $"{activity.RootPath}/{activity.Name}",
                    Status = ParseSeverity(severity),
                    Comments = "Comment not used."
                });
                continue;
            }

            var hasNonEmptyComment = commentItems.All(commentItem =>
                commentItem.Name != null && !string.IsNullOrWhiteSpace(commentItem.Name) &&
                commentItem.Name.Length >= minCommentLength);

            RuleCheckResult ruleCheckResult = new()
            {
                Rule = rule,
                Source = $"{activity.RootPath}/{activity.Name}",
                Status = hasNonEmptyComment ? RuleCheckStatus.Pass : ParseSeverity(severity),
                Comments = hasNonEmptyComment
                    ? "Non-empty comments used."
                    : $"Comments are empty or too short. Minimum length: {minCommentLength} characters."
            };

            ruleCheckResults.Add(ruleCheckResult);
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckWindowsConnectorScreenRules(Process process, Rule rule)
    {
        var severity = _config.GetParameter("WindowsScreenRules", "Severity", "Warn");

        List<RuleCheckResult> ruleCheckResults = [];
        var uavs = process.Variables.OfType<UniversalAppConnector>();
        foreach (var uav in uavs)
        {
            var windowsScreens = uav.Screens.OfType<WindowsConnectorScreen>();
            foreach (var screen in windowsScreens)
            {
                List<RuleCheckResult> screenViolations = [];
                var enabledMatchRules = screen.MatchRules.Where(l => l.Enabled).ToList();
                if (enabledMatchRules.Count == 0)
                    screenViolations.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Fail,
                        Comments = $"Windows Screen '{screen.Name}' has no match rules enabled."
                    });
                foreach (var matchRule in enabledMatchRules)
                    if (matchRule is IndexMatchRule indexRule)
                    {
                        screenViolations.Add(new RuleCheckResult
                        {
                            Rule = rule,
                            Source = $"{screen.RootPath}/{screen.Name}",
                            Status = ParseSeverity(severity),
                            Comments = $"Windows Screen '{screen.Name}' uses index property, Index = {indexRule.Index}."
                        });
                    }
                    else if (matchRule is StringComparerMatchRule stringRule && stringRule.Comparer != null)
                    {
                        if (stringRule.Comparer.Type.Equals("Equals", StringComparison.InvariantCultureIgnoreCase))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Windows Screen '{screen.Name}' uses strict 'Equals' comparison:" +
                                    $" {GetRuleType(stringRule.Type)} {stringRule.Comparer.Type} '{stringRule.Comparer.ComparisonValue}'."
                            });
                        if (ContainsDigit(stringRule.Comparer.ComparisonValue))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Windows Screen '{screen.Name}' match rule contains number:" +
                                    $" {GetRuleType(stringRule.Type)} {stringRule.Comparer.Type} '{stringRule.Comparer.ComparisonValue}'."
                            });
                    }

                if (screenViolations.Count > 0)
                    ruleCheckResults.AddRange(screenViolations);
                else
                    ruleCheckResults.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Pass,
                        Comments = $"Windows Screen '{screen.Name}' follows all match rule best practices."
                    });
            }
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckWindowsScreenElementLocatorsAndMatchRules(Process process, Rule rule)
    {
        var severity = _config.GetParameter("WindowsElementRules", "Severity", "Warn");
        var prohibitedLocators = _config.GetStringArrayParameter("WindowsElementRules", "ProhibitedLocators", ["Path"]);

        List<RuleCheckResult> ruleCheckResults = [];
        var uavs = process.Variables.OfType<UniversalAppConnector>();
        foreach (var uav in uavs)
        {
            var windowsScreens = uav.Screens.OfType<WindowsConnectorScreen>();
            foreach (var screen in windowsScreens)
            {
                List<RuleCheckResult> screenViolations = [];

                foreach (var element in screen.Elements)
                {
                    // Check locators
                    var selectedLocator = element.SelectedLocator;
                    if (selectedLocator != null)
                    {
                        if (prohibitedLocators.Contains(selectedLocator.LocateBy, StringComparer.OrdinalIgnoreCase))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Window element '{element.Name}' uses prohibited locator, {selectedLocator.LocateBy} = '{selectedLocator.Value}'."
                            });
                        else if (ContainsDigit(selectedLocator.Value))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Window element '{element.Name}' locator contains digit, {selectedLocator.LocateBy} = '{selectedLocator.Value}'."
                            });
                    }

                    // Check match rules
                    foreach (var matchRule in element.MatchRules.Where(r => r.Enabled))
                        if (matchRule is IndexMatchRule indexRule)
                        {
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Window Element '{element.Name}' uses an index match rule, Index = {indexRule.Index}."
                            });
                        }
                        else if (matchRule is StringComparerMatchRule stringRule && stringRule.Comparer is not null)
                        {
                            var matchRuleType = GetRuleType(stringRule.Type);
                            if (matchRuleType.Equals("PathMatchRule", StringComparison.OrdinalIgnoreCase))
                                screenViolations.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{screen.RootPath}/{screen.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Window element '{element.Name}' uses Path match rule, Path = '{stringRule.Comparer.ComparisonValue}'."
                                });
                            else if (matchRuleType.Equals("NameMatchRule", StringComparison.OrdinalIgnoreCase) &&
                                     ContainsDigit(stringRule.Comparer.ComparisonValue))
                                screenViolations.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{screen.RootPath}/{screen.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Window element '{element.Name}' contains digit in match rule, Name = '{stringRule.Comparer.ComparisonValue}'."
                                });
                        }
                        else if (matchRule is ElementMatchRule elementRule)
                        {
                            if (ContainsDigit(elementRule.ElementType))
                                screenViolations.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{screen.RootPath}/{screen.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Window element '{element.Name}' contains digit in match rule, Type = '{elementRule.ElementType}'."
                                });
                            if (ContainsDigit(elementRule.ElementId))
                                screenViolations.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{screen.RootPath}/{screen.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Window element '{element.Name}' contains digit in match rule, ID = '{elementRule.ElementId}'."
                                });
                        }
                }

                if (screenViolations.Count > 0)
                    ruleCheckResults.AddRange(screenViolations);
                else
                    ruleCheckResults.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Pass,
                        Comments =
                            $"All elements in Windows Screen '{screen.Name}' follow locator and match rule best practices."
                    });
            }
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckChromeConnectorScreenRules(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ChromeScreenRules", "Severity", "Warn");
        List<RuleCheckResult> ruleCheckResults = [];
        var uavs = process.Variables.OfType<UniversalAppConnector>();
        foreach (var uav in uavs)
        {
            var chromeScreens = uav.Screens.OfType<ChromeConnectorScreen>();
            foreach (var screen in chromeScreens)
            {
                List<RuleCheckResult> screenViolations = [];
                var enabledMatchRules = screen.MatchRules.Where(l => l.Enabled).ToList();
                if (enabledMatchRules.Count == 0)
                    screenViolations.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Fail,
                        Comments = $"Chrome Screen '{screen.Name}' has no match rules enabled."
                    });
                foreach (var matchRule in enabledMatchRules)
                    if (matchRule is IndexMatchRule indexRule)
                    {
                        screenViolations.Add(new RuleCheckResult
                        {
                            Rule = rule,
                            Source = $"{screen.RootPath}/{screen.Name}",
                            Status = ParseSeverity(severity),
                            Comments = $"Chrome Screen '{screen.Name}' uses index property, Index = {indexRule.Index}."
                        });
                    }
                    else if (matchRule is StringComparerMatchRule stringRule && stringRule.Comparer is not null)
                    {
                        if (stringRule.Comparer.Type.Equals("Equals", StringComparison.OrdinalIgnoreCase))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Chrome Screen '{screen.Name}' uses strict 'Equals' comparison, {GetRuleType(stringRule.Type)} {stringRule.Comparer.Type} '{stringRule.Comparer.ComparisonValue}'."
                            });
                        if (stringRule.Comparer.ComparisonValue.Any(char.IsDigit))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Chrome Screen '{screen.Name}' match rule contains number: {GetRuleType(stringRule.Type)} {stringRule.Comparer.Type} '{stringRule.Comparer.ComparisonValue}'."
                            });
                    }

                if (screenViolations.Count > 0)
                    ruleCheckResults.AddRange(screenViolations);
                else
                    ruleCheckResults.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Pass,
                        Comments = $"Chrome Screen '{screen.Name}' follows all match rule best practices."
                    });
            }
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckChromeScreenElementLocatorsAndJsMatchRules(Process process, Rule rule)
    {
        var severity = _config.GetParameter("ChromeElementRules", "Severity", "Warn");
        var prohibitedJsMatchRuleProperties =
            _config.GetStringArrayParameter("ChromeElementRules", "ProhibitedJsMatchRuleProperties", ["Index"]);

        List<RuleCheckResult> ruleCheckResults = [];
        var uavs = process.Variables.OfType<UniversalAppConnector>();
        foreach (var uav in uavs)
        {
            var chromeScreens = uav.Screens.OfType<ChromeConnectorScreen>();
            foreach (var screen in chromeScreens)
            {
                List<RuleCheckResult> screenViolations = [];

                foreach (var element in screen.Elements)
                {
                    // Check locators
                    var selectedLocator = element.SelectedLocator;
                    if (selectedLocator != null && ContainsDigit(selectedLocator.Value))
                        screenViolations.Add(new RuleCheckResult
                        {
                            Rule = rule,
                            Source = $"{screen.RootPath}/{screen.Name}",
                            Status = ParseSeverity(severity),
                            Comments =
                                $"Chrome element '{element.Name}' locator contains digit, {selectedLocator.LocateBy} = '{selectedLocator.Value}'."
                        });

                    // Check JS match rules
                    foreach (var jsMatchRule in element.JsMatchRules.Where(r => r.Enabled))
                        if (jsMatchRule.Type.Equals("Property", StringComparison.OrdinalIgnoreCase) &&
                            prohibitedJsMatchRuleProperties.Contains(jsMatchRule.Name,
                                StringComparer.OrdinalIgnoreCase))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Chrome element '{element.Name}' uses prohibited JS match rule property: {jsMatchRule.Name} = {jsMatchRule.Value}."
                            });
                        else if (ContainsDigit(jsMatchRule.Value))
                            screenViolations.Add(new RuleCheckResult
                            {
                                Rule = rule,
                                Source = $"{screen.RootPath}/{screen.Name}",
                                Status = ParseSeverity(severity),
                                Comments =
                                    $"Chrome element '{element.Name}' JS match rule contains digit: {jsMatchRule.Name} {jsMatchRule.Comparer} '{jsMatchRule.Value}'."
                            });
                }

                if (screenViolations.Count > 0)
                    ruleCheckResults.AddRange(screenViolations);
                else
                    ruleCheckResults.Add(new RuleCheckResult
                    {
                        Rule = rule,
                        Source = $"{screen.RootPath}/{screen.Name}",
                        Status = RuleCheckStatus.Pass,
                        Comments =
                            $"All elements in Chrome Screen '{screen.Name}' follow locator and JS match rule best practices."
                    });
            }
        }

        return ruleCheckResults;
    }

    private List<RuleCheckResult> CheckDataTransformUsage(Process process, Rule rule)
    {
        var severity = _config.GetParameter("DataTransformUsage", "Severity", "Warn");
        List<RuleCheckResult> ruleCheckResults = [];
        var foundDataTransform = false;

        foreach (var activity in process.Activities)
        {
            var executableItems = activity.Items.OfType<ExecutableItem>();
            foreach (var item in executableItems)
                if (item.DataTransforms.Count > 0)
                {
                    foundDataTransform = true;
                    foreach (var dataTransform in item.DataTransforms)
                        if (dataTransform.HasModifiedScript)
                        {
                            if (dataTransform.Enabled)
                                ruleCheckResults.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{activity.RootPath}/{activity.Name}",
                                    Status = RuleCheckStatus.Pass,
                                    Comments =
                                        $"Data transform in '{(string.IsNullOrEmpty(item.Name) ? item.Type : item.Name)}' has a valid script and is enabled."
                                });
                            else
                                ruleCheckResults.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{activity.RootPath}/{activity.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Data transform in '{(string.IsNullOrEmpty(item.Name) ? item.Type : item.Name)}' has a valid script but is not enabled."
                                });
                        }
                        else
                        {
                            if (dataTransform.Enabled)
                                ruleCheckResults.Add(new RuleCheckResult
                                {
                                    Rule = rule,
                                    Source = $"{activity.RootPath}/{activity.Name}",
                                    Status = ParseSeverity(severity),
                                    Comments =
                                        $"Data transform in '{(string.IsNullOrEmpty(item.Name) ? item.Type : item.Name)}' has invalid/unmodified script and is enabled."
                                });
                        }
                }
        }

        // If no design items with data transforms were found, add a passing result
        if (!foundDataTransform)
            ruleCheckResults.Add(new RuleCheckResult
            {
                Rule = rule,
                Source = "Process",
                Status = RuleCheckStatus.Pass,
                Comments = "No design items with data transforms found in the process"
            });

        return ruleCheckResults;
    }

    private static bool ContainsDigit(string value)
    {
        return HasDigitRegex.IsMatch(value);
    }

    private static string GetRuleType(string ruleType)
    {
        const string prefix = ".";

        var lastIndex = ruleType.ToLower().LastIndexOf(prefix, StringComparison.InvariantCultureIgnoreCase);
        if (lastIndex != -1)
        {
            var result = ruleType.Substring(lastIndex + prefix.Length);
            return result.Trim();
        }

        return ruleType;
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