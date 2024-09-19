using CodeAnalyzer.Models.Bot;
using CodeAnalyzer.Services.RuleChecker;
using Spectre.Console;

namespace CodeAnalyzer.Services.Display;

public static class BotStructureFormatter
{
    public static Tree FormatBotStructure(Process process)
    {
        var root = new Tree("[yellow]Bot Structure[/]")
            .Style("blue")
            .Guide(TreeGuide.Line);

        var nodesByPath = new Dictionary<string, TreeNode>();

        if (!string.IsNullOrWhiteSpace(process.Description))
            root.AddNode(new Markup($"[bold]Description:[/] {Markup.Escape(process.Description)}"));

        AddPlugins(process.Plugins, "Plugins", root);
        AddPlugins(process.UserPlugins, "User Plugins", root);
        BuildTreeStructure(process.Activities, root, nodesByPath);
        BuildTreeStructure(process.Variables, root, nodesByPath);

        return root;
    }

    private static void BuildTreeStructure<T>(IEnumerable<T> items, Tree root, Dictionary<string, TreeNode> nodesByPath)
        where T : class
    {
        foreach (var item in items)
        {
            var itemPath = GetItemPath(item);
            var segments = itemPath.Split('/');
            TreeNode? parentNode = null;

            for (var i = 0; i < segments.Length; i++)
            {
                var currentPath = string.Join("/", segments.Take(i + 1));
                if (!nodesByPath.TryGetValue(currentPath, out var currentNode))
                {
                    currentNode = new TreeNode(new Markup(segments[i]));
                    nodesByPath[currentPath] = currentNode;
                    if (i == 0)
                        root.AddNode(currentNode);
                    else
                        parentNode?.AddNode(currentNode);
                }

                parentNode = currentNode;
            }

            var itemNode = new TreeNode(new Markup($"[{GetNodeColor(item)}]{Markup.Escape(GetItemName(item))}[/]"));
            parentNode?.AddNode(itemNode);
            AddItemDetails(itemNode, item);
        }
    }

    private static void AddItemDetails<T>(TreeNode itemNode, T item) where T : class
    {
        switch (item)
        {
            case Activity activity:
                itemNode.AddNode(new Markup($"[gray]Variables: {activity.Variables.Count}[/]"));
                itemNode.AddNode(
                    new Markup($"[gray]Executable Items: {activity.Items.OfType<ExecutableItem>().Count()}[/]"));
                break;
            case GlobalVariable variable:
                var dataType = FrameworkRuleChecker.GetConnectorType(variable.DataType);
                itemNode.AddNode(new Markup($"[gray]Type: {dataType}[/]"));
                break;
        }
    }

    private static void AddPlugins<T>(IEnumerable<T> plugins, string title, Tree root) where T : AbstractPlugin
    {
        var pluginsNode = root.AddNode(new Markup($"[magenta]{title}[/]"));
        foreach (var plugin in plugins)
            pluginsNode.AddNode(new Markup($"[cyan]{Markup.Escape(plugin.Name)}[/] (v{plugin.Version})"));
    }

    private static string GetItemName<T>(T item) where T : class
    {
        return item switch
        {
            Activity activity => activity.Name,
            GlobalVariable variable => variable.Name,
            _ => item.GetType().Name
        };
    }

    private static string GetItemPath<T>(T item) where T : class
    {
        return item switch
        {
            Activity activity => activity.RootPath,
            GlobalVariable variable => variable.RootPath,
            _ => string.Empty
        };
    }

    private static string GetNodeColor<T>(T item) where T : class
    {
        return item switch
        {
            Activity _ => "white",
            GlobalVariable variable => variable.DataType switch
            {
                "AutxVariable" => "green",
                _ => "yellow"
            },
            _ => "white"
        };
    }
}