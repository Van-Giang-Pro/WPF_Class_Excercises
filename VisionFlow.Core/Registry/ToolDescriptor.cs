using VisionFlow.Core.Tools;

namespace VisionFlow.Core.Registry;

public sealed class ToolDescriptor
{
    public ToolDescriptor(string key, string dsplayName, string category, string description, Type toolType, Func<VisionTool> factory)
    {
        Key = key;
        DisplayName = DisplayName;
        Category = category;
        ToolType = toolType;
        Factory = factory;
    }
    
    public string Key { get; }
    public string DisplayName { get; }
    public string Category { get; }
    public string Description { get; }
    public Type ToolType { get; }
    public Func<VisionTool> Factory { get; }
}