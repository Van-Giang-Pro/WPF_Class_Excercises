namespace VisionFlow.Engine.Execution;

public sealed class FlowExecutionException : Exception
{
    public FlowExecutionException(string message) : base(message) { }
}