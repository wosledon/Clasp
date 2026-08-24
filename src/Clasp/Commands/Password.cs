using Clasp.Plugin;
using System.Text;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("password", Description = "生成强密码（可定制规则）")]
internal class Password : ClaspCommand
{
    public enum CharSetEnum { Upper, Lower, Digit, Symbol, All }

    [ClaspOption("--length", "-l", Description = "密码长度（默认16）")]
    public int Length { get; set; } = 16;

    [ClaspOption("--count", "-c", Description = "生成数量（默认1）")]
    public int Count { get; set; } = 1;

    [ClaspOption("--charset", "-t", Description = "字符集：upper, lower, digit, symbol, all（默认all）")]
    public CharSetEnum Charset { get; set; } = CharSetEnum.All;

    [ClaspOption("--no-ambiguous", Description = "排除易混淆字符")]
    public bool NoAmbiguous { get; set; }

    [ClaspOption("--separator", "-s", Description = "多个密码之间的分隔符（默认换行）")]
    public string Separator { get; set; } = "\n";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Length < 4 || Length > 256)
            ValidationError("密码长度必须在 4-256 之间");
        if (Count < 1 || Count > 100)
            ValidationError("生成数量必须在 1-100 之间");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var charSets = GetCharSets();
        if (charSets.Count == 0)
        {
            WriteLine("错误: 未指定任何字符集", ClaspColorType.Red);
            return;
        }

        var random = new Random();
        var passwords = new List<string>();

        for (int i = 0; i < Count; i++)
        {
            var password = GeneratePassword(random, charSets);
            passwords.Add(password);
        }

        WriteLine(string.Join(Separator, passwords), ClaspColorType.Green);
    }

    private List<char> GetCharSets()
    {
        var charSets = new List<char>();

        switch (Charset)
        {
            case CharSetEnum.Upper:
                charSets.AddRange("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                break;
            case CharSetEnum.Lower:
                charSets.AddRange("abcdefghijklmnopqrstuvwxyz");
                break;
            case CharSetEnum.Digit:
                charSets.AddRange("0123456789");
                break;
            case CharSetEnum.Symbol:
                charSets.AddRange("!@#$%^&*()_+-=[]{}|;:,.<>?");
                break;
            case CharSetEnum.All:
                charSets.AddRange("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                charSets.AddRange("abcdefghijklmnopqrstuvwxyz");
                charSets.AddRange("0123456789");
                charSets.AddRange("!@#$%^&*()_+-=[]{}|;:,.<>?");
                break;
        }

        if (NoAmbiguous)
        {
            var ambiguous = "0O1lI".ToCharArray();
            charSets = charSets.Except(ambiguous).ToList();
        }

        return charSets;
    }

    private string GeneratePassword(Random random, List<char> charSets)
    {
        var password = new StringBuilder();
        var usedChars = new HashSet<char>();

        for (int i = 0; i < Length; i++)
        {
            char ch;
            do
            {
                ch = charSets[random.Next(charSets.Count)];
            } while (NoAmbiguous && usedChars.Contains(ch));

            password.Append(ch);
            if (NoAmbiguous)
                usedChars.Add(ch);
        }

        return password.ToString();
    }
}
