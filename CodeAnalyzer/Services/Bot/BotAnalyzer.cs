using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using CodeAnalyzer.Services.Parser;
using CodeAnalyzer.Services.RuleChecker;
using CodeAnalyzer.Utilities;
using Spectre.Console;

namespace CodeAnalyzer.Services.Bot
{
    public static class BotAnalyzer
    {
        private static readonly List<IRuleChecker> RuleCheckers =
        [
            new DiagnosticsRuleChecker(),
            new FrameworkRuleChecker(),
            new CodeQualityRuleChecker()
        ];

        private static (List<RuleCheckResult>, Process) AnalyzeBotFile(string filePath)
        {
            var results = new List<RuleCheckResult>();
            var process = BotXmlParser.Parse(filePath);
            foreach (var checker in RuleCheckers)
            {
                results.AddRange(checker.CheckRules(process));
            }
            return (results, process);
        }

        private static string GetOutputPath(string inputFilePath)
        {
            string? directoryName = Path.GetDirectoryName(inputFilePath);
            if (string.IsNullOrEmpty(directoryName))
            {
                throw new Exception("Unable to determine the directory for the output file.");
            }

            return Path.Combine(
                directoryName,
                $"{Path.GetFileNameWithoutExtension(inputFilePath)}_analysis.csv"
            );
        }

        public static (List<RuleCheckResult> results, Process process, string outputFilePath) PerformAnalysis(string botFilePath, string? outputFilePath = "")
        {
            List<RuleCheckResult> results = [];
            Process process = new();

            botFilePath = FileValidator.GetSanitizedPath(botFilePath);

            if (outputFilePath is null || string.IsNullOrWhiteSpace(outputFilePath))
            {
                outputFilePath = GetOutputPath(botFilePath);
            }
            outputFilePath = FileValidator.GetSanitizedPath(outputFilePath);
            outputFilePath = FileValidator.EnsureCsvExtension(outputFilePath);

            AnsiConsole.Status()
                .Start("Analyzing file...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Toggle9);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    (results, process) = AnalyzeBotFile(botFilePath);
                    Csv.WriteDataTableToCsv(results, outputFilePath);
                });

            return (results, process, outputFilePath);
        }
    }
}