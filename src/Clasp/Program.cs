using System.Reflection;
using System.Collections.Generic;

var registry = CommandRegistry.Scan(Assembly.GetExecutingAssembly());
await registry.DispatchAsync(args);
return 0;
