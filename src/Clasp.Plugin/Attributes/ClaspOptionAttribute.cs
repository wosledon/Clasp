using System;

namespace Clasp.Plugin.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ClaspOptionAttribute : Attribute
{
    /// <summary>
    /// 选项名
    /// </summary>
    public string[] Names { get; }

    /// <summary>
    /// 选项描述
    /// </summary>
    public string? Description { get; set; }

    public ClaspOptionAttribute(params string[] names)
    {
        Names = names;
    }
}
