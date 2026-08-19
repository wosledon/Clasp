using System.Reflection;
using System.IO;

var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
Directory.CreateDirectory(pluginsPath);

var registry = CommandRegistry.Scan(Assembly.GetExecutingAssembly(), pluginsPath);
await registry.DispatchAsync(args);
return 0;
