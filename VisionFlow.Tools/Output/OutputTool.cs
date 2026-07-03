using VisionFlow.Core.Imaging;
using VisionFlow.Core.Tools;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Models;

namespace VisionFlow.Tools.Output;

[ToolMetadata("Output", DisplayName = "Output", Category = "OutputSource", Description = "Flow endpoint: receives the result image and OK/NG judment")]
public sealed class OutputTool : VisionTool
{
    private readonly InputPort<IVisionImage> _image;
    private readonly InputPort<CircleResult> _circle;

    public OutputTool()
    {
        _image = AddInput<IVisionImage>("Image", "Image");
        _circle = AddInput<CircleResult>("Circle", "Circle", optional: true);
    }

    public Judge LastJudge { get; private set; } = Judge.None;

    protected override void OnExecute(IToolContext context)
    {
        LastJudge = _circle.Value?.Judge ?? Judge.None;
        context.Log($"Output: Judge={LastJudge}");
    }
}