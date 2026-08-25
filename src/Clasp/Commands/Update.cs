using System.Reflection;
using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("update", Description = "更新 Clasp 自身")]
internal class Update : ClaspCommand
{
    [ClaspOption("--check", "-c", Description = "仅检查更新，不执行更新")]
    public bool Check { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await UpdateFromGitHubReleaseAsync(cancellationToken);
    }

    private async Task UpdateFromGitHubReleaseAsync(CancellationToken cancellationToken)
    {
        const string owner = "wosledon";
        const string repo = "Clasp";
        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        WriteLine("正在检查 GitHub 最新版本...");

        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Clasp-Updater");

            using var response = await http.GetAsync(apiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                WriteLine($"检查更新失败：HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                return;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            var name = root.GetProperty("name").GetString();
            var htmlUrl = root.GetProperty("html_url").GetString();
            var body = root.GetProperty("body").GetString();

            WriteLine($"最新版本：{name ?? tagName}");
            WriteLine($"发布页面：{htmlUrl}");

            if (!string.IsNullOrWhiteSpace(body))
            {
                WriteLine("更新内容：");
                WriteLine(body!);
            }

            if (Check)
            {
                WriteLine("提示：去掉 --check 可执行下载更新。");
                await Task.CompletedTask;
                return;
            }

            if (!root.TryGetProperty("assets", out var assets) || assets.GetArrayLength() == 0)
            {
                WriteLine("该 Release 没有可用资产，请手动下载：");
                WriteLine(htmlUrl!);
                return;
            }

            string? assetUrl = null;
            string? assetName = null;

            foreach (var asset in assets.EnumerateArray())
            {
                var assetNameLower = asset.GetProperty("name").GetString() ?? "";
                var osDesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription ?? "";
                var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

                var isWindows = osDesc.Contains("Windows", StringComparison.OrdinalIgnoreCase);
                var isLinux = osDesc.Contains("Linux", StringComparison.OrdinalIgnoreCase);
                var isOsx = osDesc.Contains("Darwin", StringComparison.OrdinalIgnoreCase) || osDesc.Contains("Mac", StringComparison.OrdinalIgnoreCase);
                var isX64 = arch.Contains("X64", StringComparison.OrdinalIgnoreCase);
                var isArm64 = arch.Contains("Arm64", StringComparison.OrdinalIgnoreCase);

                if ((isWindows && assetNameLower.Contains("win")) || (isLinux && assetNameLower.Contains("linux")) || (isOsx && assetNameLower.Contains("osx") || assetNameLower.Contains("mac")))
                {
                    if ((isX64 && assetNameLower.Contains("x64")) || (isArm64 && assetNameLower.Contains("arm64")) || assetNameLower.Contains("universal"))
                    {
                        assetUrl = asset.GetProperty("browser_download_url").GetString();
                        assetName = asset.GetProperty("name").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(assetUrl))
            {
                WriteLine("未找到适合当前平台的发布资产，请手动下载：");
                WriteLine(htmlUrl!);
                return;
            }

            WriteLine($"找到资产：{assetName!}");
            WriteLine("开始下载...");

            var downloadResponse = await http.GetAsync(assetUrl!, cancellationToken);
            if (!downloadResponse.IsSuccessStatusCode)
            {
                WriteLine($"下载失败：HTTP {(int)downloadResponse.StatusCode}");
                return;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "clasp-update");
            Directory.CreateDirectory(tempDir);
            var savePath = Path.Combine(tempDir, assetName!);

            await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await downloadResponse.Content.CopyToAsync(fs, cancellationToken);

            WriteLine($"下载完成：{savePath}");
            WriteLine("正在解压...");

            var extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);

            if (assetName!.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(savePath, extractDir, true);
            }
            else if (assetName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || assetName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
            {
                WriteLine("请手动解压 tar.gz 文件并覆盖到当前目录完成更新。");
                WriteLine($"解压目录：{extractDir}");
                return;
            }
            else
            {
                WriteLine("未知的压缩格式，请手动解压并覆盖到当前目录完成更新。");
                WriteLine($"文件路径：{savePath}");
                return;
            }

            WriteLine("解压完成，准备覆盖...");
            var targetDir = AppContext.BaseDirectory;
            var copied = CopyDirectory(extractDir, targetDir);
            WriteLine($"已更新 {copied} 个文件到：{targetDir}");

            WriteLine("更新完成，即将重启...");
            RestartSelf();
        }
        catch (Exception ex)
        {
            WriteLine($"更新失败：{ex.Message}");
        }
    }

    private static int CopyDirectory(string source, string target)
    {
        var sourceDir = new DirectoryInfo(source);
        if (!sourceDir.Exists)
            return 0;

        Directory.CreateDirectory(target);
        int count = 0;

        foreach (var file in sourceDir.GetFiles("*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file.FullName);
            var dest = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file.FullName, dest, true);
            count++;
        }

        return count;
    }

    private static void RestartSelf()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
            };

            System.Diagnostics.Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch
        {
            // ignore restart failures
        }
    }
}
