using System.Reflection;

using Clasp.Plugin;

namespace Clasp.Tests;

internal static class CommandTestHelper
{
    public static T CreateCommand<T>() where T : ClaspCommand
    {
        return (T)Activator.CreateInstance(typeof(T))!;
    }

    public static void SetOption(ClaspCommand command, string propertyName, object? value)
    {
        var property = command.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on {command.GetType().Name}");

        property.SetValue(command, value);
    }

    public static async Task<bool> ValidateThrowsAsync(ClaspCommand command)
    {
        try
        {
            await command.ValidateAsync(new ClaspCommandArgs { Command = command.GetType().Name }, CancellationToken.None);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    public static async Task<bool> ValidateThrowsAsync(ClaspCommand command, ClaspCommandArgs args)
    {
        try
        {
            await command.ValidateAsync(args, CancellationToken.None);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    public static async Task RunExecuteAsync(ClaspCommand command)
    {
        await command.ExecuteAsync(new ClaspCommandArgs { Command = command.GetType().Name }, CancellationToken.None);
    }
}
