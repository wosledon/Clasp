using System.Security.Cryptography;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("rand", Description = "生成随机密码或随机数")]
internal class Rand : ClaspCommand
{
    private const string LetterChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.?";

    [ClaspOption("--type", "-t", Description = "类型: password/string/int (默认 password)")]
    public string Type { get; set; } = "password";

    [ClaspOption("--length", "-l", Description = "密码/字符串长度 (默认 16)")]
    public int Length { get; set; } = 16;

    [ClaspOption("--min", Description = "随机整数最小值 (默认 0)")]
    public int Min { get; set; }

    [ClaspOption("--max", Description = "随机整数最大值 (默认 100)")]
    public int Max { get; set; } = 100;

    [ClaspOption("--count", "-n", Description = "生成数量 (默认 1)")]
    public int Count { get; set; } = 1;

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var type = Type.Trim().ToLowerInvariant();
        var count = Math.Clamp(Count, 1, 100);

        if (type == "int")
        {
            var (lo, hi) = Min <= Max ? (Min, Max) : (Max, Min);
            for (var i = 0; i < count; i++)
                WriteLine(RandomNumberGenerator.GetInt32(lo, hi + 1).ToString());
        }
        else
        {
            var charset = type == "string" ? LetterChars + DigitChars : LetterChars + DigitChars + SymbolChars;
            var length = Math.Clamp(Length, 1, 4096);
            for (var i = 0; i < count; i++)
                WriteLine(new string(RandomNumberGenerator.GetItems(charset.ToCharArray(), length)));
        }

        await Task.CompletedTask;
    }
}
