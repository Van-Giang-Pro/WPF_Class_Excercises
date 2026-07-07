using System.Reflection;
using VisionFlow.Core.Tools;

namespace VisionFlow.Core.Registry;

public interface IToolRegistry
{
    IReadOnlyCollection<ToolDescriptor> Descriptors { get; }
    void Register(Type toolType);
    void RegisterAssembly(Assembly assembly);
    ToolDescriptor? Find(string key);
    VisionTool Create(string key);
}