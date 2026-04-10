namespace InSpectra.Gen.Execution;

public static class CliInvocationEnvironmentFactory
{
    public static IReadOnlyDictionary<string, string> CreateCurrentProcessSnapshot()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is not string key || entry.Value is not string value)
            {
                continue;
            }

            values[key] = value;
        }

        values["NO_COLOR"] = "1";
        values["FORCE_COLOR"] = "0";
        values["TERM"] = "dumb";
        return values;
    }
}
