using System.Diagnostics;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Tools;
using VisionFlow.Engine.Graph;

namespace VisionFlow.Engine.Execution;

public sealed class FlowExecutor : IDisposable
{
    private readonly List<IVisionImage> _trackedImages = new();
    
    public ExecutionResult Run(FlowGraph graph, IToolContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        context ??= new ToolContext(graph.PixelSize);
        DisposeTracked();
        return ExecuteOrdered(graph, TopologicalSort(graph), context);
    }
    
    public ExecutionResult RunUpTo(FlowGraph graph, FlowNode target, IToolContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(target);
        context ??= new ToolContext(graph.PixelSize);
        DisposeTracked();

        var ancestors = AncestorsInclusive(graph, target);
        var ordered = TopologicalSort(graph).Where(n => ancestors.Contains(n.Id)).ToList();
        return ExecuteOrdered(graph, ordered, context);
    }

    public ExecutionResult RunSingleNode(FlowGraph graph, FlowNode node, IToolContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(node);
        context ??= new ToolContext(graph.PixelSize);
        return ExecuteOrdered(graph, new[] { node }, context, propagateSkip: false, disposeFirst: false);
    }
    
    public void PrepareInputs(FlowGraph graph, FlowNode node, IToolContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(node);
        context ??= new ToolContext(graph.PixelSize);
        DisposeTracked();

        var ancestors = AncestorsInclusive(graph, node);
        ancestors.Remove(node.Id);
        var ordered = TopologicalSort(graph).Where(n => ancestors.Contains(n.Id)).ToList();
        ExecuteOrdered(graph, ordered, context);
        FeedInputs(graph, node);
    }
    
    private ExecutionResult ExecuteOrdered(FlowGraph graph, IReadOnlyList<FlowNode> order, IToolContext context, bool propagateSkip = true, bool disposeFirst = false)
    {
        if (disposeFirst) DisposeTracked();
        var bad = new HashSet<string>(StringComparer.Ordinal); 
        var results = new List<NodeExecutionResult>(order.Count);
        var swTotal = Stopwatch.StartNew();

        foreach (var node in order)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var tool = node.Tool;

            if (propagateSkip && HasBadRequiredUpstream(graph, node, bad))
            {
                tool.MarkSkipped("Bỏ qua: tool thượng nguồn lỗi.");
                bad.Add(node.Id);
                results.Add(new NodeExecutionResult(node.Id, tool.DisplayName, tool.State, 0, tool.ErrorMessage));
                continue;
            }

            FeedInputs(graph, node);

            var sw = Stopwatch.StartNew();
            try
            {
                tool.MarkRunning();
                tool.Execute(context);
                sw.Stop();
                tool.MarkCompleted(sw.ElapsedMilliseconds);
                TrackOutputs(node);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                tool.MarkFailed(sw.ElapsedMilliseconds, ex.Message);
                bad.Add(node.Id);
            }
            results.Add(new NodeExecutionResult(node.Id, tool.DisplayName, tool.State, tool.ElapsedMs, tool.ErrorMessage));
        }
        swTotal.Stop();
        return new ExecutionResult(results, swTotal.ElapsedMilliseconds);
    }
    
    private static HashSet<string> AncestorsInclusive(FlowGraph graph, FlowNode target)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        stack.Push(target.Id);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!set.Add(id)) continue;
            foreach (var conn in graph.Connections.Where(c => c.TargetNodeId == id))
                stack.Push(conn.SourceNodeId);
        }
        return set;
    }
    
    private void FeedInputs(FlowGraph graph, FlowNode node)
    {
        foreach (var input in node.Tool.Inputs)
        {
            var conn = graph.ConnectionsInto(node.Id, input.Name).FirstOrDefault();
            if (conn is null) continue;

            var source = graph.GetNode(conn.SourceNodeId);
            var outPort = source?.Tool.FindOutput(conn.SourcePort);
            if (outPort is null) continue;

            var value = outPort.Value;
            
            if (value is IVisionImage img && graph.ConnectionsFrom(source!.Id, outPort.Name).Count() > 1)
            {
                var clone = img.Clone();
                _trackedImages.Add(clone);
                value = clone;
            }
            input.Value = value;
        }
    }

    private void TrackOutputs(FlowNode node)
    {
        foreach (var output in node.Tool.Outputs)
        {
            if (output.Value is IVisionImage img)
                _trackedImages.Add(img);
        }
    }
    
    private static bool HasBadRequiredUpstream(FlowGraph graph, FlowNode node, HashSet<string> bad)
    {
        foreach (var input in node.Tool.Inputs)
        {
            if (input.IsOptional) continue;
            foreach (var conn in graph.ConnectionsInto(node.Id, input.Name))
            {
                if (bad.Contains(conn.SourceNodeId)) return true;
            }
        }
        return false;
    }
    
    public static IReadOnlyList<FlowNode> TopologicalSort(FlowGraph graph)
    {
        var indegree = graph.Nodes.ToDictionary(n => n.Id, _ => 0, StringComparer.Ordinal);
        var adjacency = graph.Nodes.ToDictionary(n => n.Id, _ => new List<string>(), StringComparer.Ordinal);
        var map = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);

        foreach (var c in graph.Connections)
        {
            if (!indegree.ContainsKey(c.SourceNodeId) || !indegree.ContainsKey(c.TargetNodeId)) continue;
            adjacency[c.SourceNodeId].Add(c.TargetNodeId);
            indegree[c.TargetNodeId]++;
        }

        var queue = new Queue<string>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<FlowNode>(graph.Nodes.Count);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            order.Add(map[id]);
            foreach (var next in adjacency[id])
            {
                if (--indegree[next] == 0) queue.Enqueue(next);
            }
        }

        if (order.Count != graph.Nodes.Count)
        {
            var inCycle = indegree.Where(kv => kv.Value > 0).Select(kv => map[kv.Key].Tool.DisplayName);
            throw new FlowExecutionException("Đồ thị flow có chu trình, không thể thực thi. Node liên quan: " + string.Join(", ", inCycle));
        }
        return order;
    }
    
    private void DisposeTracked()
    {
        foreach (var img in _trackedImages)
        {
            try
            {
                if (!img.IsDisposed) img.Dispose();
            }
            catch
            {
            }
        }
        _trackedImages.Clear();
    }
    public void Dispose() => DisposeTracked();
}