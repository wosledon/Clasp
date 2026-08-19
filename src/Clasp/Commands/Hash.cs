using System.Security.Cryptography;
using System.Text;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("hash", Description = "计算文本或文件的哈希值")]
internal class Hash : ClaspCommand
{
    [ClaspOption("--algo", "-a", Description = "算法: md5/sha1/sha256/sha512 (默认 sha256)")]
    public string Algo { get; set; } = "sha256";

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            input = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHelp();
            return;
        }

        var algo = Algo.Trim().ToLowerInvariant() switch
        {
            "md5" => HashAlgorithmName.MD5,
            "sha1" => HashAlgorithmName.SHA1,
            "sha512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        byte[] digest;
        if (File.Exists(input))
        {
            await using var stream = File.OpenRead(input);
            digest = await CryptographicOperations.HashDataAsync(algo, stream, cancellationToken);
        }
        else
        {
            digest = CryptographicOperations.HashData(algo, Encoding.UTF8.GetBytes(input));
        }

        WriteLine($"{Convert.ToHexStringLower(digest)}  {(File.Exists(input) ? Path.GetFileName(input) : "")}");
    }
}
