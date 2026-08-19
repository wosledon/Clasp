namespace Clasp.Plugin.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ClaspCommandAttribute : Attribute
{
    /// <summary>
    /// 命令名
    /// </summary>
    public string[] Names { get; }

    /// <summary>
    /// 命令描述
    /// </summary>
    public string? Description { get; set; }

    public ClaspCommandAttribute(params string[] names)
    {
        Names = names;
    }
}
