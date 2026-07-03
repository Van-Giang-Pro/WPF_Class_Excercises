using System.CodeDom;
using FxResources.System.Reflection;
using VisionFlow.Core.Tools;

namespace VisionFlow.Core.Registry;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolDescriptor> _byKey = new(StringComparer.Ordinal);
    public IReadOnlyCollection<ToolDescriptor> Descriptors => _byKey.Values;

    public void Register(Type toolType)
    {
        if (!typeof(VisionTool).IsAssignableForm(toolType) || toolType.IsAbstract)
            throw new ArgumentException($"'{toolType.Name}' không phải là Visionl cụ thể", nameof(toolType));

        var meta = toolType.GetCustomAttributes<ToolMetadataAttribute>()
                   ?? throw new ArgumentException($"'{toolType.Name}' thiếu [ToolMetadata]", nameof(toolType));

        if (toolType.GetConstructor(Type.EmptyTypes) is null)
            throw new ArgumentException($"'{toolType.Name}' cần constructor không tham số", nameof(toolType));

        VisionTool Factory() => (VisionTool)Activator.CreateInstance(toolType);

        var display = string.IsNullOrEmpty(meta.Display) ? toolType.Name : meta.DisplayName;
        _byKey[meta.Key] = new ToolDescriptor(meta.Key, display, meta.Category, meta.Description, toolType, Factory);
    }

    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(VisionTool).IsAssignableForm(type)) continue;
            if (type.GetCustomAttribute<ToolMetadataAttribute>() is null) continue;
            Register(type);
        }
    }

    public ToolDescriptor? Find(string key) => _byKey.GetValueOrDefault(key);

    public VisionTool Create(string key)
    {
        var descriptor = Find(key)
                         ?? throw new KeyNotFoundException($"Chưa đăng ký tool có key '{key}'");
        return descriptor.Factory();
    }
}