using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("ts", Description = "时间戳与日期互转")]
internal class Ts : ClaspCommand
{
    [ClaspOption("--utc", "-u", Description = "使用 UTC 时间")]
    public bool Utc { get; set; }

    [ClaspOption("--ms", Description = "毫秒时间戳")]
    public bool Milliseconds { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHelp();
            return;
        }

        if (long.TryParse(input, out var timestamp))
        {
            try
            {
                var time = Milliseconds
                    ? DateTime.UnixEpoch.AddMilliseconds(timestamp)
                    : DateTime.UnixEpoch.AddSeconds(timestamp);

                if (!Utc)
                    time = time.ToLocalTime();

                WriteLine($"{time:yyyy-MM-dd HH:mm:ss}{(Utc ? " UTC" : "")}");
            }
            catch (ArgumentOutOfRangeException)
            {
                WriteLine("时间戳超出范围", ClaspColorType.BrightRed);
            }
            return;
        }

        if (DateTime.TryParse(input, out var date))
        {
            var utc = Utc
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime();
            var diff = utc - DateTime.UnixEpoch;
            WriteLine(Milliseconds
                ? ((long)diff.TotalMilliseconds).ToString()
                : ((long)diff.TotalSeconds).ToString());
            return;
        }

        WriteLine($"无法解析: {input}", ClaspColorType.BrightRed);
    }
}
