namespace VisionFlow.Core.Tools;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ToolMetadataAttribute : Attribute
{
    public ToolMetadataAttribute(string key)
    {
        Key = key;
    }
    
    public string Key { get; }

    public string DisplayName { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Description { get; set; } = string.Empty;
}