namespace VisionFlow.Engine.Persistence;

internal sealed class FlowDto
{
    public string Name { get; set; } = "Untitled";
    public double PixelSize { get; set; } = 1.0;
    public List<NodeDto> Nodes { get; set; } = new();
    public List<ConnectionDto> Connections { get; set; } = new();
}

internal sealed class NodeDto
{
    public string Id { get; set; } = string.Empty;
    public string TypeKey { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

internal sealed class ConnectionDto
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourcePort { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetPort { get; set; } = string.Empty;
}