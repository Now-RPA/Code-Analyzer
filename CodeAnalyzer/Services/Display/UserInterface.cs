using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using Spectre.Console;

namespace CodeAnalyzer.Services.Display;

public static class UserInterface
{

    public static void DisplayAnalysisResults(List<RuleCheckResult> results)
    {
        var table = AnalysisResultsFormatter.FormatAnalysisResults(results);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(AnalysisResultsFormatter.GetCodeQualityRating(CalculateOverallScore(results)));
    }

    public static void DisplayBotStructure(Process process)
    {
        var tree = BotStructureFormatter.FormatBotStructure(process);
        AnsiConsole.Write(tree);
    }

    public static void DisplayIssueTable(List<RuleCheckResult> results)
    {
        var tables = IssueTableFormatter.FormatIssueTables(results);
        foreach (var table in tables)
        {
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
        AnsiConsole.MarkupLine(IssueTableFormatter.GetIssueSummary(results));
    }

    private static double CalculateOverallScore(List<RuleCheckResult> results)
    {
        var groupedResults = results.GroupBy(r => r.Rule.Category);
        double overallScore = 0;
        int categoryCount = 0;

        foreach (var group in groupedResults)
        {
            int pass = group.Count(r => r.Status == RuleCheckStatus.Pass);
            int warn = group.Count(r => r.Status == RuleCheckStatus.Warn);
            int fail = group.Count(r => r.Status == RuleCheckStatus.Fail);
            int total = pass + warn + fail;
            double categoryScore = (pass * 1.0 + warn * 0.5) / total * 100;
            overallScore += categoryScore;
            categoryCount++;
        }

        return overallScore / categoryCount;
    }
}