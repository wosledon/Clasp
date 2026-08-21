# Clasp

[English](README.en.md) | **中文文档（主文档）**

Clasp 是一个跨平台的 .NET 命令行工具箱，内置网络、文本处理、系统信息等常用命令，并支持通过 `plugins` 目录加载插件 DLL 扩展命令。

## 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## 快速开始

从源码构建：

```bash
dotnet build
./src/Clasp/bin/Debug/net10.0/Clasp help
./src/Clasp/bin/Debug/net10.0/Clasp dns --host example.com
./src/Clasp/bin/Debug/net10.0/Clasp echo --msg "hello"
```

也可以直接下载 Release 压缩包，解压后直接运行 `clasp`：

```bash
./clasp help
./clasp dns --host example.com
```

## 构建与测试

```bash
dotnet build
dotnet test tests/Clasp.Tests/Clasp.Tests.csproj
```

## 内置命令

| 命令                  | 说明                     |
| --------------------- | ------------------------ |
| `b64`                 | Base64 编码/解码         |
| `cat`                 | 读取并输出文件内容       |
| `conv`                | 单位换算：字节/温度      |
| `count`               | 统计行数、词数、字符数   |
| `date`                | 显示当前日期和时间       |
| `dns`                 | 查询域名的 A/AAAA 记录   |
| `echo`                | 输出内容                 |
| `env`                 | 显示环境变量             |
| `file-download`、`fd` | 多线程下载文件           |
| `hash`                | 计算文本或文件的哈希值   |
| `help`                | 显示所有支持的工具       |
| `http`                | 发送 HTTP 请求并显示响应 |
| `ip`                  | 显示本机 IP 地址         |
| `json`                | 格式化或校验 JSON        |
| `jwt`                 | 解码 JWT，不验签         |
| `kill`                | 按端口或进程名结束进程   |
| `ls`                  | 列出当前目录文件         |
| `port`                | 检测 TCP 端口是否开放    |
| `procs`               | 列出进程信息             |
| `rand`                | 生成随机密码或随机数     |
| `speed`               | 网络下载测速             |
| `sysinfo`             | 显示系统信息             |
| `ts`                  | 时间戳与日期互转         |
| `urlenc`              | URL 编码/解码            |
| `uuid`                | 生成 UUID v4             |
| `version`             | 显示版本号               |

每个命令都支持 `--help` 或 `-h` 查看选项说明。

## 插件系统

Clasp 会自动读取程序目录下 `plugins` 文件夹中的 `.dll` 文件，并将其中的命令注册到主程序。若该目录不存在，程序会自动创建。

### 如何开发插件

1. 新建一个 .NET 10 类库项目。
2. 引用 `src/Clasp.Plugin/Clasp.Plugin.csproj`。
3. 创建继承自 `ClaspCommand` 的命令类。
4. 使用 `[ClaspCommand("name", Description = "...")]` 标记命令。
5. 使用 `[ClaspOption("--option", "-o", Description = "...")]` 标记选项属性。
6. 实现 `ValidateAsync` 和 `ExecuteAsync`。

### 最小示例

```csharp
using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace MyPlugin;

[ClaspCommand("hello", Description = "向指定名称问好")]
internal class HelloCommand : ClaspCommand
{
    [ClaspOption("--name", "-n", Description = "要问好的名称")]
    public string Name { get; set; } = "world";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"Hello, {Name}!");
        await Task.CompletedTask;
    }
}
```

### 编译与安装

```bash
dotnet build
```

然后将编译生成的插件 DLL 复制到 Clasp 程序目录下的 `plugins` 文件夹即可。

### 说明

- 插件通过反射扫描 `[ClaspCommand]` 和 `[ClaspOption]` 自动注册。
- 插件可使用 `ClaspCommandArgs`、彩色输出和进程调用等能力。
- 若多个插件定义了同名命令，后加载的会覆盖先加载的。

## License

MIT
