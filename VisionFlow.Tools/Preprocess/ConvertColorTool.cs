using System.ComponentModel;
using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Core.Imaging;

namespace VisionFlow.Tools.Preprocess;

[ToolMetadata("ConvertColor", DisplayName = "Convert Color"), Category = "Preprocess", Description = "Color space conversion via Cv2.CvtColor")]
public sealed class ConvertColorTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly OutputPort<IVisionImage> _output;
    private readonly ToolParameter<ColoConversionCodes> _code;

    public ConvertColorTool()
    {
        _input = AddInput<IVisionImage>("Image", "Image");
        _output = AddOutput<IVisionImage>("Image", "Image");
        _code = addParameter("Code", ColorConversionCodes.BGR2GRAY, "Conversion Code", category: "Coversion", order: 1);
    }

    protected override void OnExecute(IToolContext context)
    {
        var src = _input.Value!.AsMat();
        var dst = new Mat();
        Cv2.CvtColor(src, dst, _code.Value);
        _output.Value = new VisionImage(dst);
    }
}