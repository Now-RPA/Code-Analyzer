using System.Reflection;

namespace CodeAnalyzer.Services.Config;

public static class ConfigManager
{
    private static readonly string ConfigFolderPath = Path.Combine(GetApplicationRootPath(), "Config");
    public static ConfigReader? DiagnosticsConfigReader { get; private set; }
    public static ConfigReader? FrameworkConfigReader { get; private set; }
    public static ConfigReader? CodeQualityConfigReader { get; private set; }
    public static bool SuccessfullyInitialized { get; private set; }
    public static Exception? InitializationError { get; private set; }

    static ConfigManager()
    {
        try
        {
            if (!Directory.Exists(ConfigFolderPath))
            {
                throw new DirectoryNotFoundException($"Config folder not found at: {ConfigFolderPath}");
            }
            DiagnosticsConfigReader = InitializeConfig("Diagnostics");
            FrameworkConfigReader = InitializeConfig("Framework");
            CodeQualityConfigReader = InitializeConfig("CodeQuality");
            SuccessfullyInitialized = true;
        }
        catch (Exception ex)
        {
            SuccessfullyInitialized = false;
            InitializationError = ex;
        }
    }
    private static ConfigReader InitializeConfig(string configName)
    {
        string fullPath = Path.Combine(ConfigFolderPath, $"{configName}.json");
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Required config file not found: {fullPath}");
        }
        return new ConfigReader(fullPath);
    }

    private static string GetApplicationRootPath()
    {
        var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(exePath))
        {
            exePath = AppContext.BaseDirectory;
        }
        return exePath ?? throw new InvalidOperationException("Unable to determine the application root path.");
    }
}