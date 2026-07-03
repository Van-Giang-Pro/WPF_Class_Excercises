using VisionFlow.Core.Ports;

namespace VisionFlow.Core.Tools;

public interface ITool
{
    string Id { get; set; }
    string TypeKey { get; }
    string DisplayName { get; }
    string Category { get; }
    
    IReadOnlyList<IInputPort> Inputs { get; }
    IReadOnlyList<IOutputPort> Outputs { get; }
    IReadOnlyList<IToolParameter> Parameters { get; }
    
    ToolState State { get; }
    long ElapsedMs { get; }
    string? ErrorMessage { get; }
    void Execute(IToolContext context);
}