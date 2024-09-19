namespace CodeAnalyzer.Utilities;

public static class FileValidator
{
    public static bool IsValidIBotFile(string filePath)
    {
        try
        {
            var normalizedPath = GetSanitizedPath(filePath);
            return File.Exists(normalizedPath)
                   && Path.GetExtension(normalizedPath).Equals(".ibot", StringComparison.OrdinalIgnoreCase)
                   && normalizedPath.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsValidFilePath(string filePath)
    {
        try
        {
            var normalizedPath = GetSanitizedPath(filePath);
            return normalizedPath.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string GetSanitizedPath(string filePath)
    {
        filePath = filePath.Trim().Trim('\'', '\"');
        return Path.GetFullPath(filePath);
    }

    public static string EnsureCsvExtension(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        var extension = Path.GetExtension(filePath);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            filePath = Path.ChangeExtension(filePath, ".csv");
        return filePath;
    }
}