using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;

namespace VisionFlow.Tools.Preprocess;

[ToolMetadata("Threshold", DisplayName = "Threshold", Category = "Filtering", Description = "Binary thresholding")]
public sealed class ThresholdTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly OutputPort<IVisionImage _output;
    private readonly ToolParameter<double> _threshold;
    private readonly ToolParameter<double> _maxValue;
    private readonly ToolParameter<ThresholdTypes> _type;

    public ThresholdTool()
    {
        _input = AddInput<IVisionImage>("Image", "Image");
        _output = AddOutput<IVisionImage>("Image", "Image");
        _threshold = AddParameter("Threshold", 100.0, "Threshold", 0.0, 255.0, category: "Threshold", order: 1);
        _maxValue = AddParameter("Max Value", 255.0, "Max Value", 0.0, 255.0, category: "Threshold", order: 2);
        _type = AddParameter("Type", ThresholdTypes.Binary, "Threshold Type", category: "Threshold", order: 3);
    }

    protected override void OnExecute(IToolContext context)
    {
        var src = _input.Value!.AsMat();
        var dst = new Mat();
        Cv2.Threshold(src, dst, _threshold.Value, _maxValue.Value, _type.Value);
        _output.Value = new MatVisionImage(dst);
    }
}