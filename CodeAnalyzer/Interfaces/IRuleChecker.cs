using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;

namespace CodeAnalyzer.Interfaces;

public interface IRuleChecker
{
    string Category { get; }
    List<RuleCheckResult> CheckRules(Process process);
}