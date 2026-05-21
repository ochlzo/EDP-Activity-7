namespace edp_gui_app;

public static class LocalEnvironmentLoader
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = line[..separatorIndex].Trim();
            var value = TrimQuotes(line[(separatorIndex + 1)..].Trim());
            if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public static void LoadFromCurrentDirectory() => Load(Path.Combine(Environment.CurrentDirectory, ".env"));

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
