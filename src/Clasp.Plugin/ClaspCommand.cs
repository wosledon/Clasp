using System.Diagnostics;
using System.Text;

namespace Clasp.Plugin;

public abstract class ClaspCommand
{
    public abstract Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default);

    public abstract Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default);

    protected void ValidationError(string message)
    {
        throw new InvalidOperationException(message);
    }

    public virtual void ShowHelp()
    {
        foreach (var line in ClaspHelp.RenderCommandHelp(GetType()))
            WriteLine(line);
    }
    protected virtual async Task<string> ReadStandardInputAsync(CancellationToken cancellationToken = default)
    {
        if (!Console.IsInputRedirected)
            return string.Empty;

        return await Console.In.ReadToEndAsync(cancellationToken);
    }

    protected virtual void Write(string message, ClaspColorType colorType = ClaspColorType.Default)
    {
        var color = ClaspColor.FromEnum(colorType);
        if (string.IsNullOrEmpty(color.AnsiCode))
        {
            Console.Write(message);
            return;
        }

        Console.Write(color.Apply(message));
    }

    protected virtual void Write(string message, string colorHex)
    {
        var color = ClaspColor.FromHex(colorHex);
        if (string.IsNullOrEmpty(color.AnsiCode))
        {
            Console.Write(message);
            return;
        }

        Console.Write(color.Apply(message));
    }

    protected virtual void WriteLine(string message, ClaspColorType colorType = ClaspColorType.Default)
    {
        Write($"{message}{Environment.NewLine}", colorType);
    }

    protected virtual void WriteLine(string message, string colorHex)
    {
        Write($"{message}{Environment.NewLine}", colorHex);
    }

    protected async Task<CommandResult> CmdAsync(
        string fileName,
        string? arguments = null,
        string? workingDirectory = null,
        string? input = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("命令路径不能为空", nameof(fileName));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = !string.IsNullOrEmpty(input),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };

        if (!string.IsNullOrEmpty(input))
            process.StartInfo.StandardInputEncoding = Encoding.UTF8;

        if (!process.Start())
            throw new InvalidOperationException($"无法启动进程: {fileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!string.IsNullOrEmpty(input))
        {
            await process.StandardInput.WriteAsync(input).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new CommandResult(process.ExitCode, stdout, stderr);
    }

    protected bool IsWindows()
    {
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
    }
}

public sealed class CommandResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;

    public CommandResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }
}
