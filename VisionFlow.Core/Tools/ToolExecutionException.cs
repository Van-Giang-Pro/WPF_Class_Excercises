namespace VisionFlow.Core.Tools;

public sealed class ToolExecutionException : Exception
{
    public ToolExecutionException(string message): base(message) { }
    public ToolExecutionException(string message, Exception inner): base(message, inner) { }
}