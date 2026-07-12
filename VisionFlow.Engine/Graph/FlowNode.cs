using VisionFlow.Core.Tools;

namespace VisionFlow.Engine.Graph;

public sealed class FlowNode
{
    public FlowNode(VisionTool tool)
    {
        Tool = tool ?? throw new ArgumentNullException(nameof(tool));
    }

    public string Id => Tool.Id;
    public VisionTool Tool { get; }
    public double X { get; set; }
    public double Y { get; set; }
}
