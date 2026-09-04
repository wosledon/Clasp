using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

sealed class CommandRegistry
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    private CommandRegistry() { }

    private static string? GetFrameworkRefDirectory()
    {
        try
        {
            var coreLibPath = typeof(object).Assembly.Location;
            if (string.IsNullOrEmpty(coreLibPath) || !File.Exists(coreLibPath))
                return null;

            var runtimeDir = Path.GetDirectoryName(coreLibPath)!;
            var packsDir = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", "..", "packs", "Microsoft.NETCore.App.Ref"));
            if (!Directory.Exists(packsDir))
                return null;

            var refDir = Directory.GetDirectories(packsDir)
                .OrderByDescending(d => d)
                .FirstOrDefault(d => Directory.Exists(Path.Combine(d, "ref", "net10.0")));

            if (refDir is null)
                return null;

            return Path.Combine(refDir, "ref", "net10.0");
        }
        catch
        {
            return null;
        }
    }

    public static CommandRegistry Scan(Assembly assembly)
    {
        var registry = new CommandRegistry();
        registry.LoadAssembly(assembly);
        return registry;
    }

    public static CommandRegistry Scan(Assembly assembly, string pluginsPath)
    {
        var registry = Scan(assembly);

        if (!string.IsNullOrWhiteSpace(pluginsPath) && Directory.Exists(pluginsPath))
        {
            var cacheDir = Path.Combine(pluginsPath, ".clasp-cache");
            Directory.CreateDirectory(cacheDir);

            // 先加载所有 DLL 插件，并统一注入依赖解析
            foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    LoadPluginAssembly(registry, dll, cacheDir);
                }
                catch
                {
                    // ignore unloadable plugin assemblies
                }
            }

            // 再处理 CS 源码插件：编译为 DLL 缓存后，走和 DLL 一样的加载路径
            foreach (var cs in Directory.GetFiles(pluginsPath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var cachedDll = CompileSourcePlugin(cs, pluginsPath, cacheDir);
                    if (!string.IsNullOrEmpty(cachedDll))
                        LoadPluginAssembly(registry, cachedDll, cacheDir);
                }
                catch
                {
                    // ignore unloadable source plugins
                }
            }
        }

        return registry;
    }

    private void LoadAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(Clasp.Plugin.ClaspCommand).IsAssignableFrom(type) || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspCommandAttribute>();
            if (attr is null)
                continue;

            foreach (var name in attr.Names)
                _commands[name] = type;
        }
    }

    private static void LoadSourcePlugin(CommandRegistry registry, string csPath)
    {
        try
        {
            var code = File.ReadAllText(csPath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var refs = new List<MetadataReference>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddReference(string? path)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                if (!addedPaths.Add(path))
                    return;

                try
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
                catch
                {
                    // ignore files that cannot be used as references
                }
            }

            AddReference(typeof(Clasp.Plugin.ClaspCommand).Assembly.Location);

            var baseDir = AppContext.BaseDirectory;
            if (Directory.Exists(baseDir))
            {
                foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
                    AddReference(dll);
            }

            var refDir = GetFrameworkRefDirectory();
            if (!string.IsNullOrEmpty(refDir))
            {
                foreach (var dll in Directory.GetFiles(refDir, "System.Runtime.dll"))
                    AddReference(dll);
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: Path.GetRandomFileName(),
                syntaxTrees: new[] { syntaxTree },
                references: refs,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithMetadataImportOptions(MetadataImportOptions.All));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = string.Join(
                    Environment.NewLine,
                    emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

                Console.WriteLine($"插件编译失败: {csPath}{Environment.NewLine}{errors}");
                return;
            }

            ms.Seek(0, SeekOrigin.Begin);
            var pluginAssembly = Assembly.Load(ms.ToArray());
            registry.LoadAssembly(pluginAssembly);
        }
        catch
        {
            // ignore unloadable source plugins
        }
    }

    private static string? CompileSourcePlugin(string csPath, string pluginsPath, string cacheDir)
    {
        try
        {
            var code = File.ReadAllText(csPath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var refs = new List<MetadataReference>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddReference(string? path)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                if (!addedPaths.Add(path))
                    return;

                try
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
                catch
                {
                    // ignore files that cannot be used as references
                }
            }

            AddReference(typeof(Clasp.Plugin.ClaspCommand).Assembly.Location);

            var baseDir = AppContext.BaseDirectory;
            if (Directory.Exists(baseDir))
            {
                foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
                    AddReference(dll);
            }

            if (Directory.Exists(pluginsPath))
            {
                foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly))
                    AddReference(dll);
            }

            var refDir = GetFrameworkRefDirectory();
            if (!string.IsNullOrEmpty(refDir))
            {
                foreach (var dll in Directory.GetFiles(refDir, "System.Runtime.dll"))
                    AddReference(dll);
            }

            var assemblyName = Path.GetFileNameWithoutExtension(csPath);
            var dllPath = Path.Combine(cacheDir, $"{assemblyName}.dll");
            var metaPath = dllPath + ".meta";

            var sourceTime = File.GetLastWriteTimeUtc(csPath);
            if (File.Exists(dllPath) && File.Exists(metaPath))
            {
                var cachedTime = File.ReadAllText(metaPath);
                if (long.TryParse(cachedTime, out var cachedTicks) && cachedTicks == sourceTime.Ticks)
                    return dllPath;
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: refs,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithMetadataImportOptions(MetadataImportOptions.All));

            using var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var emitResult = compilation.Emit(fs);

            if (!emitResult.Success)
            {
                var errors = string.Join(
                    Environment.NewLine,
                    emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

                Console.WriteLine($"插件编译失败: {csPath}{Environment.NewLine}{errors}");
                return null;
            }

            File.WriteAllText(metaPath, sourceTime.Ticks.ToString());
            return dllPath;
        }
        catch
        {
            return null;
        }
    }

    private static void LoadPluginAssembly(CommandRegistry registry, string assemblyPath, string cacheDir)
    {
        var baseDir = AppContext.BaseDirectory;
        var pluginDir = Path.GetDirectoryName(assemblyPath)!;

        Assembly? ResolveDependency(object? sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ?? false)
                name = name.Substring(0, name.Length - 4);

            string[] searchPaths =
            {
                Path.Combine(pluginDir, $"{name}.dll"),
                Path.Combine(cacheDir, $"{name}.dll"),
                Path.Combine(baseDir, $"{name}.dll"),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return Assembly.LoadFrom(path);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            return null;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveDependency;
        try
        {
            var pluginAssembly = Assembly.LoadFrom(assemblyPath);
            registry.LoadAssembly(pluginAssembly);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependency;
        }
    }

    public async Task<int> DispatchAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var commandName = args[0];
        if (!_commands.TryGetValue(commandName, out var commandType))
        {
            Console.WriteLine($"未知命令: {commandName}");
            PrintUsage();
            return 1;
        }

        var parsed = ParseOptions(args.Skip(1).ToArray(), commandType);
        if (parsed.Options.ContainsKey("--help") || parsed.Options.ContainsKey("-h"))
        {
            PrintHelp(commandType);
            return 0;
        }

        var commandArgs = new Clasp.Plugin.ClaspCommandArgs { Command = commandName };

        var optionsField = typeof(Clasp.Plugin.ClaspCommandArgs).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
        var optionsDict = (Dictionary<string, string>)optionsField!.GetValue(commandArgs)!;
        foreach (var (key, value) in parsed.Options)
            optionsDict[key] = value;

        foreach (var positional in parsed.Positionals)
            commandArgs.AddValue(positional);

        if (parsed.Positionals.Count > 0)
            commandArgs.Value = parsed.Positionals[0];

        var command = (Clasp.Plugin.ClaspCommand)Activator.CreateInstance(commandType)!;
        InjectOptions(command, parsed.Options);

        try
        {
            await command.ValidateAsync(commandArgs, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            await command.ExecuteAsync(commandArgs, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }

    private static (Dictionary<string, string> Options, List<string> Positionals) ParseOptions(string[] args, Type commandType)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        var boolOptions = new HashSet<string>(StringComparer.Ordinal);
        var valueOptions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in commandType.GetProperties())
        {
            var optionAttr = property.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspOptionAttribute>();
            if (optionAttr is null)
                continue;

            foreach (var name in optionAttr.Names)
            {
                if (property.PropertyType == typeof(bool))
                    boolOptions.Add(name);
                else
                    valueOptions.Add(name);
            }
        }

        var optionsEnded = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (optionsEnded || arg == "-" || !arg.StartsWith("-"))
            {
                positionals.Add(arg);
                continue;
            }

            if (arg == "--")
            {
                optionsEnded = true;
                continue;
            }

            if (arg.StartsWith("--"))
            {
                if (boolOptions.Contains(arg))
                {
                    options[arg] = "true";
                }
                else if (arg.Contains("="))
                {
                    var eq = arg.IndexOf('=');
                    options[arg.Substring(0, eq)] = arg.Substring(eq + 1);
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[arg] = args[i + 1];
                    i++;
                }
                continue;
            }

            var shortOptions = arg.Substring(1);
            if (shortOptions.Length == 1)
            {
                var key = $"-{shortOptions}";
                if (boolOptions.Contains(key))
                {
                    options[key] = "true";
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[key] = args[i + 1];
                    i++;
                }
                else
                {
                    options[key] = "true";
                }
            }
            else
            {
                var valueIndex = -1;
                for (var j = shortOptions.Length - 1; j >= 0; j--)
                {
                    var key = $"-{shortOptions[j]}";
                    if (valueOptions.Contains(key))
                    {
                        valueIndex = j;
                        break;
                    }
                }

                if (valueIndex >= 0)
                {
                    var valuePart = shortOptions.Substring(valueIndex + 1);
                    if (valuePart.Length > 0)
                    {
                        options[$"-{shortOptions[valueIndex]}"] = valuePart;

                        for (var j = 0; j < valueIndex; j++)
                            options[$"-{shortOptions[j]}"] = "true";
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        options[$"-{shortOptions[valueIndex]}"] = args[i + 1];
                        i++;

                        for (var j = 0; j < valueIndex; j++)
                            options[$"-{shortOptions[j]}"] = "true";
                    }
                    else
                    {
                        for (var j = 0; j <= valueIndex; j++)
                            options[$"-{shortOptions[j]}"] = "true";
                    }
                }
                else
                {
                    foreach (var ch in shortOptions)
                        options[$"-{ch}"] = "true";
                }
            }
        }

        return (options, positionals);
    }

    private static void InjectOptions(Clasp.Plugin.ClaspCommand command, Dictionary<string, string> parsedOptions)
    {
        var commandType = command.GetType();
        foreach (var property in commandType.GetProperties())
        {
            var optionAttr = property.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspOptionAttribute>();
            if (optionAttr is null)
                continue;

            foreach (var name in optionAttr.Names)
            {
                string? rawValue = null;
                foreach (var kvp in parsedOptions)
                {
                    if (string.Equals(kvp.Key, name, StringComparison.Ordinal))
                    {
                        rawValue = kvp.Value;
                        break;
                    }
                }

                if (rawValue != null)
                {
                    var converted = ConvertValue(rawValue, property.PropertyType);
                    property.SetValue(command, converted);
                    break;
                }
            }
        }
    }

    private static object ConvertValue(string raw, Type targetType)
    {
        if (targetType.IsEnum)
            return Enum.TryParse(targetType, raw, ignoreCase: true, out var result)
                ? result
                : Activator.CreateInstance(targetType)!;

        if (targetType == typeof(bool))
            return bool.Parse(raw);

        return Convert.ChangeType(raw, targetType);
    }

    public IEnumerable<(string Names, string? Description)> GetCommands()
    {
        return _commands
            .GroupBy(pair => pair.Value)
            .Select(group =>
            {
                var names = string.Join(", ", group.Select(pair => pair.Key).Distinct());
                var desc = group.Key.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspCommandAttribute>()?.Description;
                return (Names: names, Description: desc);
            })
            .OrderBy(item => item.Names);
    }

    private void PrintUsage()
    {
        foreach (var line in Clasp.Plugin.ClaspHelp.RenderCommandList(GetCommands()))
            Console.WriteLine(line);
    }

    private void PrintHelp(Type commandType)
    {
        foreach (var line in Clasp.Plugin.ClaspHelp.RenderCommandHelp(commandType))
            Console.WriteLine(line);
    }
}