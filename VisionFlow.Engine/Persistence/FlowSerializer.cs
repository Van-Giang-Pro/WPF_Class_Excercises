using System.Text.Json; // Thư viên Json có săn của .NET
using System.Text.Json.Serialization; // Khai báo phần tùy chỉnh nâng cao của Json để làm việc với JSon Serialization
// Ở đây cần cho JsonStringEnumConverter (bộ chuyển enum sang chuỗi và ngược lại)
using VisionFlow.Core.Registry; // IToolRegistry nhà máy tạo tool từ tên (dùng ở hàm Deserialize)
using VisionFlow.Engine.Graph; // FlowGraph, FlowNode, FlowConnection

namespace VisionFlow.Engine.Persistence;

public static class FlowSerializer // Ta có static là không tạo instance (new FlowSerializer()) được
{
    private static readonly JsonSerializerOptions Options = new()
    // Gán một lần lúc khởi tạo, sau đó không cho gán lại (tránh sửa nhầm)
    // Nghĩa là chỉ có 1 bản duy nhất cho toàn bộ chương trình
    // Không cần tạo object mới cũng dùng được
    // Tất cả các nơi trong class đều dùng chung 1 option
    {
        WriteIndented = true, // là một tùy chọn trong System.Text.Json để định dạng JSON cho dễ đọc
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // là tùy chọn đổi tên property thành camelCase khi serialize JSON
        Converters = { new JsonStringEnumConverter() } // Enum được lưu dưới dạng chuỗi, dễ đọc, rõ ràng hơn nhiều
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
        var dto = JsonSerializer.Deserialize<FlowDto>(json, Options) ?? throw new InvalidOperationException("Nội dung flow JSON rỗng hoặc không hợp lệ.");
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
            case JsonValueKind.String: return el.GetString();
            
            case JsonValueKind.Number:
                if (t.IsEnum || t == typeof(int)) return el.GetInt32();
                if (t == typeof(long)) return el.GetInt64();
                if (t == typeof(float)) return el.GetSingle();
                return el.GetDouble();
            
            case JsonValueKind.True:
                
            case JsonValueKind.False : return el.GetBoolean();
            
            case JsonValueKind.Null : return null;
            
            case JsonValueKind.Object:
                
            case JsonValueKind.Array: return el.Deserialize(t, Options);
            
            default: return el.GetRawText();
        }
    }
}