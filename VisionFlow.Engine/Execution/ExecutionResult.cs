using VisionFlow.Core.Tools;

namespace VisionFlow.Engine.Execution;

public sealed record NodeExecutionResult(string NodeId, string DisplayName, ToolState State, long ElapsedMs, string? Error);

public sealed class ExecutionResult
{
    public ExecutionResult(IReadOnlyList<NodeExecutionResult> nodes, long totalMs)
    {
        Nodes = nodes;
        TotalMs = totalMs;
    }

    public IReadOnlyList<NodeExecutionResult> Nodes { get; }
    public long TotalMs { get; }
    
    public bool Success => Nodes.All(n => n.State != ToolState.Failed);
}