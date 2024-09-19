using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Models.Rule;
using Spectre.Console;

namespace CodeAnalyzer.Services.Display;

public static class MenuHandler
{
    public static void DisplayMainMenu(List<RuleCheckResult> results, Process process)
    {
        var exit = false;
        while (!exit)
        {
            ConsoleOutputHandler.DisplayTitle("Analysis Results");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to view?")
                    .HighlightStyle(new Style(Color.SteelBlue1))
                    .PageSize(10)
                    .AddChoices("📊 Summary", "🗂️ Issues Report", "🏗️ Bot Structure", "🔙 Main Menu"));

            switch (choice)
            {
                case "📊 Summary":
                    UserInterface.DisplayAnalysisResults(results);
                    ConsoleOutputHandler.WaitForKeyPress();
                    break;
                case "🗂️ Issues Report":
                    UserInterface.DisplayIssueTable(results);
                    ConsoleOutputHandler.WaitForKeyPress();
                    break;
                case "🏗️ Bot Structure":
                    UserInterface.DisplayBotStructure(process);
                    ConsoleOutputHandler.WaitForKeyPress();
                    break;
                case "🔙 Main Menu":
                    exit = true;
                    break;
            }
        }
    }
}