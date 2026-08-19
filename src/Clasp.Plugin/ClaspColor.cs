namespace Clasp.Plugin;

public readonly struct ClaspColor
{
    private const string AnsiReset = "\u001b[0m";
    private const string AnsiRed = "\u001b[31m";
    private const string AnsiGreen = "\u001b[32m";
    private const string AnsiYellow = "\u001b[33m";
    private const string AnsiBlue = "\u001b[34m";
    private const string AnsiMagenta = "\u001b[35m";
    private const string AnsiCyan = "\u001b[36m";
    private const string AnsiWhite = "\u001b[37m";
    private const string AnsiBrightRed = "\u001b[91m";
    private const string AnsiBrightGreen = "\u001b[92m";
    private const string AnsiBrightYellow = "\u001b[93m";

    public string AnsiCode { get; }

    private ClaspColor(string ansiCode)
    {
        AnsiCode = ansiCode;
    }

    public static ClaspColor FromHex(string hex)
    {
        var sanitized = hex.TrimStart('#');
        if (sanitized.Length != 6)
            throw new ArgumentException("颜色值必须是 #RRGGBB 格式。", nameof(hex));

        return new ClaspColor($"\u001b[38;2;{int.Parse(sanitized.Substring(0, 2), System.Globalization.NumberStyles.HexNumber)};{int.Parse(sanitized.Substring(2, 2), System.Globalization.NumberStyles.HexNumber)};{int.Parse(sanitized.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)}m");
    }

    public static ClaspColor FromEnum(ClaspColorType color)
    {
        return color switch
        {
            ClaspColorType.Red => new ClaspColor(AnsiRed),
            ClaspColorType.Green => new ClaspColor(AnsiGreen),
            ClaspColorType.Yellow => new ClaspColor(AnsiYellow),
            ClaspColorType.Blue => new ClaspColor(AnsiBlue),
            ClaspColorType.Magenta => new ClaspColor(AnsiMagenta),
            ClaspColorType.Cyan => new ClaspColor(AnsiCyan),
            ClaspColorType.White => new ClaspColor(AnsiWhite),
            ClaspColorType.BrightRed => new ClaspColor(AnsiBrightRed),
            ClaspColorType.BrightGreen => new ClaspColor(AnsiBrightGreen),
            ClaspColorType.BrightYellow => new ClaspColor(AnsiBrightYellow),
            _ => new ClaspColor(string.Empty)
        };
    }

    public string Apply(string text)
    {
        return string.IsNullOrEmpty(AnsiCode) ? text : $"{AnsiCode}{text}{AnsiReset}";
    }
}

public enum ClaspColorType
{
    Default,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    White,
    BrightRed,
    BrightGreen,
    BrightYellow
}
