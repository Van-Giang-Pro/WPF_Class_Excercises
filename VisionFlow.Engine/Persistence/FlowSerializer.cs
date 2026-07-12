using System.Text.Json;
using System.Text.Json.Serialization;
using VisionFlow.Core.Registry;
using VisionFlow.Engine.Graph;

namespace VisionFlow.Engine.Persistence;

public static class FlowSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    public static string Serialize(FlowGraph graph)
    {
        var dto = new FlowDto { Name = graph.Name, PixelSize = graph.PixelSize };
        foreach (var node in graph.Nodes)
        {
            var nodeDto = new NodeDto
            {
                Id = node.Id,
                TypeKey = node.Tool.TypeKey,
                X = node.X,
                Y = node.Y
            };
            foreach (var p in node.Tool.Parameters) nodeDto.Parameters[p.Name] = p.Value;
            dto.Nodes.Add(nodeDto);
        }
        foreach (var c in graph.Connections)
        {
            dto.Connections.Add(new ConnectionDto
            {
                SourceNodeId = c.SourceNodeId,
                SourcePort = c.SourcePort,
                TargetNodeId = c.TargetNodeId,
                TargetPort = c.TargetPort
            });
        }
        return JsonSerializer.Serialize(dto, Options);
    }
    public static FlowGraph Deserialize(string json, IToolRegistry registry)
    {
        var dto = JsonSerializer.Deserialize<FlowDto>(json, Options)
                  ?? throw new InvalidOperationException("Nội dung flow JSON rỗng hoặc không hợp lệ.");
        var graph = new FlowGraph { Name = dto.Name, PixelSize = dto.PixelSize };
        foreach (var n in dto.Nodes)
        {
            var tool = registry.Create(n.TypeKey);
            tool.Id = n.Id;
            foreach (var (name, raw) in n.Parameters)
            {
                var param = tool.FindParameter(name);
                if (param is null) continue;
                param.Value = ExtractJson(raw, param.ValueType);
            }
            graph.AddNode(tool, n.X, n.Y);
        }
        foreach (var c in dto.Connections)
        {
            if (graph.GetNode(c.SourceNodeId) is null || graph.GetNode(c.TargetNodeId) is null) continue;
            graph.AddConnection(new FlowConnection(c.SourceNodeId, c.SourcePort, c.TargetNodeId, c.TargetPort));
        }
        return graph;
    }
    private static object? ExtractJson(object? raw, Type targetType)
    {
        if (raw is not JsonElement el) return raw;
        var t = Nullable.GetUnderlyingType(targetType) ?? targetType;
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                if (t.IsEnum || t == typeof(int)) return el.GetInt32();
                if (t == typeof(long)) return el.GetInt64();
                if (t == typeof(float)) return el.GetSingle();
                return el.GetDouble();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return el.GetBoolean();
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                return el.Deserialize(t, Options);
            default:
                return el.GetRawText();
        }
    }
}