using System.IO.Compression;
using System.Formats.Tar;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("zip", Description = "创建/查看/解压压缩文件 (支持 zip/tar/tar.gz/tar.br)")]
internal class Zip : ClaspCommand
{
    [ClaspOption("--extract", "-x", Description = "解压模式")]
    public bool Extract { get; set; }

    [ClaspOption("--create", "-c", Description = "创建模式")]
    public bool Create { get; set; }

    [ClaspOption("--list", "-l", Description = "列出压缩文件内容")]
    public bool List { get; set; }

    [ClaspOption("--output", "-o", Description = "解压输出目录 (默认当前目录)")]
    public string OutputDir { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var modeCount = (Create ? 1 : 0) + (Extract ? 1 : 0) + (List ? 1 : 0);
        if (modeCount > 1)
            ValidationError("--create / --extract / --list 不能同时使用");

        if (args.Values.Count == 0)
            ValidationError("请提供压缩文件路径");

        var archive = args.Values[0];
        if (string.IsNullOrWhiteSpace(archive))
            ValidationError("压缩文件路径不能为空");

        if (!Create && !File.Exists(archive))
            ValidationError($"文件不存在: {archive}");

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var archive = args.Values[0];

        if (List || (!Create && !Extract))
        {
            await ListArchiveAsync(archive, cancellationToken);
        }
        else if (Extract)
        {
            await ExtractArchiveAsync(archive, cancellationToken);
        }
        else if (Create)
        {
            var files = args.Values.Skip(1).ToList();
            if (files.Count == 0)
            {
                WriteLine("请提供要添加的文件", ClaspColorType.Yellow);
                return;
            }
            await CreateArchiveAsync(archive, files, cancellationToken);
        }
    }

    private static (string Format, string ActualPath) DetectFormat(string path)
    {
        var lower = path.ToLowerInvariant();
        if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz"))
            return ("tar.gz", path);
        if (lower.EndsWith(".tar.br"))
            return ("tar.br", path);
        if (lower.EndsWith(".tar"))
            return ("tar", path);
        if (lower.EndsWith(".zip") || !lower.Contains('.'))
            return ("zip", lower.Contains('.') ? path : path + ".zip");

        return ("zip", path);
    }

    private async Task ListArchiveAsync(string path, CancellationToken cancellationToken)
    {
        var (format, actualPath) = DetectFormat(path);
        var exists = File.Exists(actualPath);
        if (!exists && actualPath != path)
            exists = File.Exists(path);

        if (!exists)
        {
            WriteLine($"文件不存在: {actualPath}", ClaspColorType.BrightRed);
            return;
        }

        var resolved = exists && actualPath != path ? path : actualPath;

        WriteLine($"文件: {resolved}", ClaspColorType.Cyan);

        switch (format)
        {
            case "zip":
                await ListZipAsync(resolved, cancellationToken);
                break;
            case "tar":
                await ListTarAsync(resolved, null, cancellationToken);
                break;
            case "tar.gz":
                await using (var fs = File.OpenRead(resolved))
                await using (var gz = new GZipStream(fs, CompressionMode.Decompress))
                    await ListTarAsync(resolved, gz, cancellationToken);
                break;
            case "tar.br":
                await using (var fs = File.OpenRead(resolved))
                await using (var br = new BrotliStream(fs, CompressionMode.Decompress))
                    await ListTarAsync(resolved, br, cancellationToken);
                break;
        }
    }

    private async Task ListZipAsync(string path, CancellationToken cancellationToken)
    {
        await using var fs = File.OpenRead(path);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

        long totalBytes = 0;
        var entries = archive.Entries
            .OrderBy(e => e.FullName)
            .ToList();

        foreach (var entry in entries)
        {
            var marker = entry.FullName.EndsWith('/') ? "d" : " ";
            var size = entry.Length;
            totalBytes += size;
            WriteLine($"  [{marker}] {size,12}  {entry.FullName}");
        }

        WriteLine($"共 {entries.Count} 项, {FormatSize(totalBytes)}", ClaspColorType.Cyan);
        await Task.CompletedTask;
    }

    private async Task ListTarAsync(string path, Stream? decompressor, CancellationToken cancellationToken)
    {
        var source = decompressor;
        await using var fs = decompressor is null ? File.OpenRead(path) : null;
        source ??= fs;

        await using var reader = new TarReader(source!);
        long totalBytes = 0;
        var count = 0;

        TarEntry? entry;
        while ((entry = await reader.GetNextEntryAsync(false, cancellationToken)) is not null)
        {
            var marker = entry.EntryType == TarEntryType.Directory ? "d" : " ";
            var size = entry.Length;
            totalBytes += size;
            count++;
            WriteLine($"  [{marker}] {size,12}  {entry.Name}");
        }

        WriteLine($"共 {count} 项, {FormatSize(totalBytes)}", ClaspColorType.Cyan);
    }

    private async Task ExtractArchiveAsync(string path, CancellationToken cancellationToken)
    {
        var (format, actualPath) = DetectFormat(path);

        if (!File.Exists(actualPath))
        {
            WriteLine($"文件不存在: {actualPath}", ClaspColorType.BrightRed);
            return;
        }

        var output = string.IsNullOrWhiteSpace(OutputDir)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(OutputDir);

        Directory.CreateDirectory(output);

        switch (format)
        {
            case "zip":
                await ExtractZipAsync(actualPath, output, cancellationToken);
                break;
            case "tar":
                await ExtractTarAsync(actualPath, null, output, cancellationToken);
                break;
            case "tar.gz":
                await using (var fs = File.OpenRead(actualPath))
                await using (var gz = new GZipStream(fs, CompressionMode.Decompress))
                    await ExtractTarAsync(actualPath, gz, output, cancellationToken);
                break;
            case "tar.br":
                await using (var fs = File.OpenRead(actualPath))
                await using (var br = new BrotliStream(fs, CompressionMode.Decompress))
                    await ExtractTarAsync(actualPath, br, output, cancellationToken);
                break;
        }

        WriteLine($"已解压到: {output}", ClaspColorType.Green);
    }

    private async Task ExtractZipAsync(string path, string outputDir, CancellationToken cancellationToken)
    {
        await using var fs = File.OpenRead(path);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
        var count = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dest = Path.GetFullPath(Path.Combine(outputDir, entry.FullName));
            if (!dest.StartsWith(outputDir, StringComparison.Ordinal))
                continue; // prevent path traversal

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
            count++;
        }

        WriteLine($"解压了 {count} 个文件", ClaspColorType.Cyan);
        await Task.CompletedTask;
    }

    private async Task ExtractTarAsync(string path, Stream? decompressor, string outputDir, CancellationToken cancellationToken)
    {
        var source = decompressor;
        await using var fs = decompressor is null ? File.OpenRead(path) : null;
        source ??= fs;

        await using var reader = new TarReader(source!);
        var count = 0;

        TarEntry? entry;
        while ((entry = await reader.GetNextEntryAsync(false, cancellationToken)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dest = Path.GetFullPath(Path.Combine(outputDir, entry.Name));
            if (!dest.StartsWith(outputDir, StringComparison.Ordinal))
                continue;

            if (entry.EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            await entry.ExtractToFileAsync(dest, overwrite: true, cancellationToken);
            count++;
        }

        WriteLine($"解压了 {count} 个文件", ClaspColorType.Cyan);
    }

    private async Task CreateArchiveAsync(string path, List<string> files, CancellationToken cancellationToken)
    {
        var (format, actualPath) = DetectFormat(path);

        // resolve all files
        var resolvedFiles = new List<string>();
        foreach (var f in files)
        {
            var full = Path.GetFullPath(f);
            if (File.Exists(full))
                resolvedFiles.Add(full);
            else if (File.Exists(f))
                resolvedFiles.Add(Path.GetFullPath(f));
            else
                WriteLine($"文件不存在，跳过: {f}", ClaspColorType.Yellow);
        }

        if (resolvedFiles.Count == 0)
        {
            WriteLine("没有可添加的文件", ClaspColorType.BrightRed);
            return;
        }

        switch (format)
        {
            case "zip":
                await CreateZipAsync(actualPath, resolvedFiles, cancellationToken);
                break;
            case "tar":
                await CreateTarAsync(actualPath, null, resolvedFiles, cancellationToken);
                break;
            case "tar.gz":
                await using (var fs = File.Create(actualPath))
                await using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
                    await CreateTarAsync(actualPath, gz, resolvedFiles, cancellationToken);
                break;
            case "tar.br":
                await using (var fs = File.Create(actualPath))
                await using (var br = new BrotliStream(fs, CompressionLevel.Optimal))
                    await CreateTarAsync(actualPath, br, resolvedFiles, cancellationToken);
                break;
        }

        WriteLine($"已创建: {actualPath}", ClaspColorType.Green);
    }

    private async Task CreateZipAsync(string path, List<string> files, CancellationToken cancellationToken)
    {
        await using var fs = File.Create(path);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryName = Path.GetFileName(file);
            var entry = archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
            WriteLine($"  添加: {entryName}");
        }

        await Task.CompletedTask;
    }

    private async Task CreateTarAsync(string path, Stream? compressor, List<string> files, CancellationToken cancellationToken)
    {
        var dest = compressor;
        await using var fs = compressor is null ? File.Create(path) : null;
        dest ??= fs;

        await using var writer = new TarWriter(dest!, leaveOpen: compressor is not null);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryName = Path.GetFileName(file);
            await writer.WriteEntryAsync(file, entryName, cancellationToken);
            WriteLine($"  添加: {entryName}");
        }
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1048576 => $"{bytes / 1024.0:N1} KB",
            < 1073741824 => $"{bytes / 1048576.0:N1} MB",
            _ => $"{bytes / 1073741824.0:N2} GB"
        };
    }
}