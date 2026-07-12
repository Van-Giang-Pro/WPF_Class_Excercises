using VisionFlow.Core.Registry;
using VisionFlow.Engine.Graph;

namespace VisionFlow.Engine.Persistence;

public interface IFlowRepository
{
    void Save(FlowGraph graph, string path);
    FlowGraph Load(string path);
}
public sealed class JsonFlowRepository : IFlowRepository
{
    private readonly IToolRegistry _registry;

    public JsonFlowRepository(IToolRegistry registry)
    {
        _registry = registry;
    }
    public void Save(FlowGraph graph, string path)
        => File.WriteAllText(path, FlowSerializer.Serialize(graph));
    public FlowGraph Load(string path)
        => FlowSerializer.Deserialize(File.ReadAllText(path), _registry);
}