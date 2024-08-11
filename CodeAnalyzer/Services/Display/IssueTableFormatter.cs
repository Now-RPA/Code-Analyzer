using CodeAnalyzer.Models.Rule;
using Spectre.Console;

namespace CodeAnalyzer.Services.Display;

public static class IssueTableFormatter
{
    public static IEnumerable<Table> FormatIssueTables(List<RuleCheckResult> results)
    {
        var relevantResults = results.Where(r => r.Status != RuleCheckStatus.Pass)
            .GroupBy(r => r.Rule.Category)
            .OrderBy(g => g.Key)
            .ToList();

        if (relevantResults.Count == 0)
        {
            yield return new Table().AddColumn("Result").AddRow(new Markup("[green]No issues found. All rules passed![/]"));
            yield break;
        }

        foreach (var categoryGroup in relevantResults)
        {
            var table = new Table()
                .AddColumn("Source")
                .AddColumn("Name")
                .AddColumn("Status")
                .AddColumn("Comments")
                .Border(TableBorder.Rounded)
                .Title($"[bold]{categoryGroup.Key} Issues[/]")
                .Expand();

            var orderedResults = categoryGroup.OrderBy(r => r.Source.Length).ThenBy(r => r.Status);

            bool firstRow = true;
            foreach (var result in orderedResults)
            {
                if (!firstRow)
                {
                    table.AddEmptyRow();
                }

                var rowColor = result.Status switch
                {
                    RuleCheckStatus.Fail => "red",
                    RuleCheckStatus.Warn => "yellow",
                    _ => "white"
                };

                table.AddRow(
                    new Markup($"[{rowColor}]{Markup.Escape(result.Source)}[/]"),
                    new Markup($"[{rowColor}]{Markup.Escape(result.Rule.Name)}[/]"),
                    new Markup($"[bold {rowColor}]{Markup.Escape(result.Status.ToString())}[/]"),
                    new Markup($"[{rowColor}]{Markup.Escape(result.Comments)}[/]")
                );

                firstRow = false;
            }

            yield return table;
        }
    }

    public static string GetIssueSummary(List<RuleCheckResult> results)
    {
        int totalFailCount = results.Count(r => r.Status == RuleCheckStatus.Fail);
        int totalWarnCount = results.Count(r => r.Status == RuleCheckStatus.Warn);
        return $"[bold]Overall Summary:[/] [red]{totalFailCount} Failed[/], [yellow]{totalWarnCount} Warnings[/]";
    }
}