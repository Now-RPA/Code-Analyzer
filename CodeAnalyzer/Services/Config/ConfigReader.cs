using Microsoft.Extensions.Configuration;

namespace CodeAnalyzer.Services.Config;

public class ConfigReader
{
    private readonly IConfiguration _configuration;

    public ConfigReader(string configPath)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile(configPath, false, true);

        _configuration = builder.Build();
    }

    public T GetParameter<T>(string ruleName, string parameterName, T defaultValue)
    {
        var value = _configuration.GetSection(ruleName)[parameterName];
        if (value == null)
            return defaultValue;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public string[] GetStringArrayParameter(string ruleName, string parameterName, string[] defaultValue)
    {
        var section = _configuration.GetSection($"{ruleName}:{parameterName}");
        if (!section.Exists())
            return defaultValue;

        return section.GetChildren().Select(c => c.Value ?? string.Empty).ToArray();
    }
}