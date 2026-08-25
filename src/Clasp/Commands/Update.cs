using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System.Reflection;

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

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            var remoteVersion = (tagName ?? "").TrimStart('v');

            WriteLine($"当前版本：{currentVersion}");
            WriteLine($"最新版本：{name ?? tagName}");

            if (!IsNewerVersion(remoteVersion, currentVersion))
            {
                WriteLine("当前已是最新版本。");
                return;
            }

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

            await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await downloadResponse.Content.CopyToAsync(fs, cancellationToken);
            await fs.FlushAsync(cancellationToken);
            downloadResponse.Dispose();

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
            var exePath = Environment.ProcessPath ?? Path.Combine(targetDir, "clasp.exe");
            var updaterPath = Path.Combine(tempDir, "clasp-update.cmd");
            var updaterContent =
$@"@echo off
setlocal

set ""SOURCE={extractDir}""
set ""TARGET={targetDir}""
set ""EXE={exePath}""

:waitloop
timeout /t 1 /nobreak >nul
tasklist /fi ""imagename eq {Path.GetFileName(exePath)}"" 2>nul | find /i ""{Path.GetFileName(exePath)}"" >nul
if not errorlevel 1 goto waitloop

xcopy /E /H /Y /I ""%SOURCE%\*"" ""%TARGET%"" >nul
start """" ""%EXE%""

del ""%~f0""
";
            File.WriteAllText(updaterPath, updaterContent);

            WriteLine($"已生成更新脚本：{updaterPath}");
            WriteLine("即将退出并执行更新...");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = updaterPath,
                UseShellExecute = true,
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WriteLine($"更新失败：{ex.Message}");
        }
    }

    private static bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        var remoteParts = remoteVersion.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var currentParts = currentVersion.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var max = Math.Max(remoteParts.Length, currentParts.Length);
        Span<int> remoteNums = stackalloc int[max];
        Span<int> currentNums = stackalloc int[max];

        for (int i = 0; i < max; i++)
        {
            remoteNums[i] = i < remoteParts.Length && int.TryParse(remoteParts[i], out var rv) ? rv : 0;
            currentNums[i] = i < currentParts.Length && int.TryParse(currentParts[i], out var cv) ? cv : 0;
        }

        for (int i = 0; i < max; i++)
        {
            if (remoteNums[i] > currentNums[i])
                return true;
            if (remoteNums[i] < currentNums[i])
                return false;
        }

        return false;
    }
}
