using Spectre.Console;

namespace CodeAnalyzer.Services.Display;

public static class ConsoleOutputHandler
{
    private static readonly Color TitleColor = new(128, 182, 161);

    public static void DisplayTitle(string title)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Code Analyzer").Centered().Color(TitleColor));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold blue]{title}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    public static void DisplayError(string message, bool waitForKey = false)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($":stop_sign:[red] {Markup.Escape(message)}[/]");
        if (waitForKey) Console.ReadKey(true);
    }

    public static void DisplayStackTrace(Exception ex, bool waitForKey = false)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.WriteException(ex);
        if (waitForKey) Console.ReadKey(true);
    }

    public static void DisplayInfo(string message, int delay = 1000)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
        Thread.Sleep(delay);
    }

    public static void DisplayEscapedInfo(string escapedMessage, int delay = 1000)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]{escapedMessage}[/]");
        Thread.Sleep(delay);
    }

    public static void DisplayLabel(string message)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[SteelBlue1]{Markup.Escape(message)}[/]");
    }

    public static void DisplayEscapedLabel(string escapedMessage)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[SteelBlue1]{escapedMessage}[/]");
    }

    public static void WaitForKeyPress(string message = "⌨️ Press any key to return to the main menu...")
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
        Console.ReadKey(true);
    }
}