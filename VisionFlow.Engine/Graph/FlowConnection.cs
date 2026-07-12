namespace VisionFlow.Engine.Graph;

public sealed class FlowConnection
{
    public FlowConnection(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
    {
        SourceNodeId = sourceNodeId;
        SourcePort = sourcePort;
        TargetNodeId = targetNodeId;
        TargetPort = targetPort;
    }
    
    public string SourceNodeId { get; }
    public string SourcePort { get; }
    public string TargetNodeId { get; }
    public string TargetPort { get; }
}