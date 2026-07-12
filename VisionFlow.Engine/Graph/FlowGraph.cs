using VisionFlow.Core.Tools;

namespace VisionFlow.Engine.Graph;

public sealed class FlowGraph
{
    private readonly List<FlowNode> _nodes = new();
    private readonly List<FlowConnection> _connections = new();

    public string Name { get; set; } = "Untitled";
    
    public double PixelSize { get; set; } = 1.0;

    public IReadOnlyList<FlowNode> Nodes => _nodes;
    public IReadOnlyList<FlowConnection> Connections => _connections;

    public FlowNode AddNode(VisionTool tool, double x = 0, double y = 0)
    {
        var node = new FlowNode(tool) { X = x, Y = y };
        _nodes.Add(node);
        return node;
    }

    public void AddNode(FlowNode node) => _nodes.Add(node);

    public void RemoveNode(FlowNode node)
    {
        _connections.RemoveAll(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id);
        _nodes.Remove(node);
    }

    public FlowNode? GetNode(string id) => _nodes.FirstOrDefault(n => n.Id == id);

    public FlowConnection Connect(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
    {
        _connections.RemoveAll(c => c.TargetNodeId == targetNodeId && c.TargetPort == targetPort);
        var conn = new FlowConnection(sourceNodeId, sourcePort, targetNodeId, targetPort);
        _connections.Add(conn);
        return conn;
    }

    public void AddConnection(FlowConnection connection) => _connections.Add(connection);

    public void Disconnect(FlowConnection connection) => _connections.Remove(connection);
    
    public IEnumerable<FlowConnection> ConnectionsInto(string nodeId, string inputPort) 
        => _connections.Where(c => c.TargetNodeId == nodeId && c.TargetPort == inputPort);
    
    public IEnumerable<FlowConnection> ConnectionsFrom(string nodeId, string outputPort)
        => _connections.Where(c => c.SourceNodeId == nodeId && c.SourcePort == outputPort);
}