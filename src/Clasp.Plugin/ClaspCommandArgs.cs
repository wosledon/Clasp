namespace Clasp.Plugin;

public class ClaspCommandArgs
{
    public string Command { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> Options => _options;
    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Values => _values;
    private readonly List<string> _values = new();

    public void AddValue(string value)
    {
        _values.Add(value);
    }

    public bool TryGetOption(string name, out string? value)
    {
        return _options.TryGetValue(name, out value);
    }
}
