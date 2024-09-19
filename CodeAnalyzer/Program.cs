using System.CommandLine;
using CodeAnalyzer.Services.Bot;
using CodeAnalyzer.Services.Config;
using CodeAnalyzer.Services.Display;
using CodeAnalyzer.Utilities;
using Spectre.Console;

namespace CodeAnalyzer;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return args.Length > 0 ? await RunCommandLineMode(args) : RunInteractiveMode();
    }

    private static async Task<int> RunCommandLineMode(string[] args)
    {
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Now RPA Code Analyzer");
        var inputOption = CreateInputOption();
        var outputOption = CreateOutputOption();

        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler(HandleCommandLineAnalysis, inputOption, outputOption);

        return rootCommand;
    }

    private static Option<string> CreateInputOption()
    {
        var option = new Option<string>(
            "--input",
            "The input .iBot file to analyze.")
        {
            IsRequired = true
        };
        option.AddAlias("-i");
        return option;
    }

    private static Option<string> CreateOutputOption()
    {
        var option = new Option<string>(
            "--output",
            "The output CSV file path. If not specified, it will be created in the same directory as the input file.");
        option.AddAlias("-o");
        return option;
    }

    private static Task<int> HandleCommandLineAnalysis(string inputPath, string? outputPath)
    {
        try
        {
            if (!ConfigManager.SuccessfullyInitialized)
                throw new SystemException("Unable to read config.", ConfigManager.InitializationError);

            if (!FileValidator.IsValidIBotFile(inputPath))
            {
                ConsoleOutputHandler.DisplayError("Error: Invalid input file path. Please provide a valid .iBot file.");
                return Task.FromResult(1);
            }

            //output path is provided but is invalid
            if (outputPath != null && !FileValidator.IsValidFilePath(outputPath))
            {
                ConsoleOutputHandler.DisplayError("Error: Invalid output file path. Please provide a valid file path.");
                return Task.FromResult(1);
            }

            var (_, _, reportFilePath) = BotAnalyzer.PerformAnalysis(inputPath, outputPath);

            ConsoleOutputHandler.DisplayEscapedInfo(
                $"💾 Analysis report saved:[blue]{Markup.Escape(reportFilePath)}[/]");
            return Task.FromResult(0);
        }
        catch (Exception? ex)
        {
            ConsoleOutputHandler.DisplayStackTrace(ex);
            return Task.FromResult(0);
        }
    }

    private static int RunInteractiveMode()
    {
        while (true)
        {
            ConsoleOutputHandler.DisplayTitle("Main Menu");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .PageSize(10)
                    .HighlightStyle(new Style(Color.SteelBlue1))
                    .AddChoices("🔍 Analyze iBot file", "⏹️ Exit"));

            switch (choice)
            {
                case "🔍 Analyze iBot file":
                    HandleInteractiveAnalysis();
                    break;
                case "⏹️ Exit":
                    AnsiConsole.Clear();
                    return 0;
            }
        }
    }

    private static void HandleInteractiveAnalysis()
    {
        try
        {
            if (!ConfigManager.SuccessfullyInitialized)
                throw new SystemException("Unable to read config.", ConfigManager.InitializationError);
            ConsoleOutputHandler.DisplayTitle("Analyze File");
            var filePath = PromptForFilePath();

            if (!FileValidator.IsValidIBotFile(filePath))
            {
                ConsoleOutputHandler.DisplayError(
                    "Invalid input file. Please provide a valid .iBot file path.", true);
                return;
            }

            var (results, process, outputPath) = BotAnalyzer.PerformAnalysis(filePath);

            DisplayAnalysisResults(outputPath);
            MenuHandler.DisplayMainMenu(results, process);
        }
        catch (Exception? ex)
        {
            ConsoleOutputHandler.DisplayStackTrace(ex, true);
        }
    }

    private static string PromptForFilePath()
    {
        ConsoleOutputHandler.DisplayEscapedLabel("Enter complete path to[bold green] .iBot[/] file: ");
        return Console.ReadLine() ?? string.Empty;
    }

    private static void DisplayAnalysisResults(string outputPath)
    {
        ConsoleOutputHandler.DisplayEscapedInfo($"💾 Analysis report saved:[blue]{Markup.Escape(outputPath)}[/]");
        ConsoleOutputHandler.WaitForKeyPress("⌨️ Press any key to view summary...");
    }
}