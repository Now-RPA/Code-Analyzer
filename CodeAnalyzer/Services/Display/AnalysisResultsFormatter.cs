using CodeAnalyzer.Models.Rule;
using Spectre.Console;
using Rule = Spectre.Console.Rule;

namespace CodeAnalyzer.Services.Display;

public class AnalysisResultsFormatter
{
    public static Table FormatAnalysisResults(List<RuleCheckResult> results)
    {
        var groupedResults = results.GroupBy(r => r.Rule.Category);
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("Category").Width(20));
        table.AddColumn(new TableColumn("Pass").Width(10).Centered());
        table.AddColumn(new TableColumn("Warn").Width(10).Centered());
        table.AddColumn(new TableColumn("Fail").Width(10).Centered());
        table.AddColumn(new TableColumn("Score").Width(10).Centered());

        double overallScore = 0;
        var categoryCount = 0;
        var totalPass = 0;
        var totalWarn = 0;
        var totalFail = 0;

        foreach (var group in groupedResults)
        {
            var pass = group.Count(r => r.Status == RuleCheckStatus.Pass);
            var warn = group.Count(r => r.Status == RuleCheckStatus.Warn);
            var fail = group.Count(r => r.Status == RuleCheckStatus.Fail);
            var total = pass + warn + fail;
            var categoryScore = (pass * 1.0 + warn * 0.5) / total * 100;
            overallScore += categoryScore;
            categoryCount++;

            totalPass += pass;
            totalWarn += warn;
            totalFail += fail;

            table.AddRow(
                new Markup(Markup.Escape(group.Key)),
                new Markup($"[green]{pass}[/]"),
                new Markup($"[yellow]{warn}[/]"),
                new Markup($"[red]{fail}[/]"),
                new Markup($"[blue]{categoryScore:F1}%[/]")
            );
        }

        overallScore /= categoryCount;

        // Add a separator before the total row
        table.AddRow(new Rule().RuleStyle(Style.Parse("dim")).LeftJustified());

        // Add the total row
        table.AddRow(
            new Markup("[bold]Total[/]"),
            new Markup($"[bold green]{totalPass}[/]"),
            new Markup($"[bold yellow]{totalWarn}[/]"),
            new Markup($"[bold red]{totalFail}[/]"),
            new Markup($"[bold blue]{overallScore:F1}%[/]")
        );

        return table;
    }

    public static string GetCodeQualityRating(double overallScore)
    {
        var rating = overallScore switch
        {
            >= 95 => "Excellent",
            >= 85 => "Good",
            >= 65 => "Fair",
            >= 55 => "Needs Improvement",
            _ => "Poor"
        };

        var ratingColor = rating switch
        {
            "Excellent" => "green",
            "Good" => "blue",
            "Fair" => "yellow",
            "Needs Improvement" => "red",
            _ => "red"
        };

        return $"Rating: [{ratingColor}]{rating}[/]";
    }
}