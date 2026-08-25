# Clasp

[English](README.en.md) | **中文文档**

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

| 命令                  | 说明                                    | 别名 |
| --------------------- | --------------------------------------- | ---- |
| `bar`                 | 进度条工具                              |      |
| `b64`                 | Base64 编码或解码                       |      |
| `cat`                 | 读取并输出文件内容                      |      |
| `conv`                | 单位换算：字节/温度                     |      |
| `count`               | 统计文本的行数、词数、字符数            |      |
| `date`                | 显示当前日期和时间                      |      |
| `dns`                 | 查询域名的 A/AAAA 记录                  |      |
| `echo`                | 输出内容                                |      |
| `env`                 | 显示环境变量                            |      |
| `file-download`、`fd` | 多线程下载文件                          | `fd` |
| `gittag`、`gt`        | Git 标签管理：列出/查看/创建/删除标签   | `gt` |
| `grep`                | 在文件中搜索文本模式（支持正则）        |      |
| `hash`                | 计算文本或文件的哈希值                  |      |
| `help`                | 显示所有支持的工具                      |      |
| `http`                | 发送 HTTP 请求并显示响应                |      |
| `ip`                  | 显示本机 IP 地址                        |      |
| `json`                | 增强 JSON 工具（格式化、查询、转换）    |      |
| `jwt`                 | 解码 JWT（不验签）                      |      |
| `kill`                | 干掉占用端口的程序                      |      |
| `ls`                  | 列出当前目录文件                        |      |
| `password`            | 生成强密码（可定制规则）                |      |
| `path`                | 路径处理工具（拼接、规范化、获取信息）  |      |
| `port`                | 检测 TCP 端口是否开放                   |      |
| `procs`               | 列出进程信息                            |      |
| `proxy`               | 启动本地 HTTP 代理（正向代理/反向代理） |      |
| `rand`                | 生成随机密码或随机数                    |      |
| `scan`                | 扫描主机的开放端口                      |      |
| `serve`               | 启动静态文件服务器（开发用）            |      |
| `speed`               | 网络下载测速                            |      |
| `spinner`             | 加载动画工具                            |      |
| `sysinfo`             | 显示系统信息                            |      |
| `table`               | 将 JSON/CSV/文本输出为表格              |      |
| `ts`                  | 时间戳与日期互转                        |      |
| `urlenc`              | URL 编码或解码                          |      |
| `uuid`                | 生成 UUID (v4)                          |      |
| `version`             | 显示版本号                              |      |
| `watch`               | 监听文件/目录变化并执行命令             |      |
| `zip`                 | 创建/查看/解压压缩文件                  |      |

每个命令都支持 `--help` 或 `-h` 查看选项说明。

## 插件系统

Clasp 会自动读取程序目录下 `plugins` 文件夹中的 `.dll` 和 `.cs` 文件。`.dll` 插件通过反射加载；`.cs` 源码插件会在启动时通过 Roslyn 动态编译后加载。

### 如何开发 DLL 插件

1. 新建一个 .NET 10 类库项目。
2. 引用 `src/Clasp.Plugin/Clasp.Plugin.csproj`。
3. 创建继承自 `ClaspCommand` 的命令类。
4. 使用 `[ClaspCommand("name", Description = "...")]` 标记命令。
5. 使用 `[ClaspOption("--option", "-o", Description = "...")]` 标记选项属性。
6. 实现 `ValidateAsync` 和 `ExecuteAsync`。

### 如何开发 C# 源码插件

1. 在 `plugins` 目录下新建 `.cs` 文件。
2. 引用 `Clasp.Plugin` 命名空间。
3. 创建继承自 `ClaspCommand` 的命令类。
4. 使用 `[ClaspCommand]` 和 `[ClaspOption]` 标记命令与选项。
5. 实现 `ValidateAsync` 和 `ExecuteAsync`。

源码插件无需单独编译，Clasp 会在启动时自动编译并加载。

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

若放置的是 `.cs` 源码文件，则无需编译，直接放在 `plugins` 目录下即可。

### 说明

- 插件通过反射扫描 `[ClaspCommand]` 和 `[ClaspOption]` 自动注册。
- 插件可使用 `ClaspCommandArgs`、彩色输出和进程调用等能力。
- 若多个插件定义了同名命令，后加载的会覆盖先加载的。
- `.cs` 源码插件依赖 Roslyn 动态编译，需要 .NET 10 SDK 运行时环境。

## License

MIT
