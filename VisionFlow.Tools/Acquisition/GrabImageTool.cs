using System.Drawing.Imaging;
using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;

namespace VisionFlow.Tools.Acquisition;

[ToolMetadata("Grab", DisplayName = "Grab Image", Category = "InputSource", Description ="Load an image from a file path")]
public sealed class GrabImageTool : VisionTool
{
    private readonly IToolParameter<string> _path;
    private readonly ToolParameter<ImreadModes> _readMode;
    private readonly OutputPort<IVisionImage> _output;

    public GrabImageTool()
    {
        _path = AddParameter("ImagePath", string.Empty, "Image Path", category: "Source",order: 1);
        _readMode = AddParameter("ReadMode", ImreadModes.Color, "Read Mode", category: "Source", order: 2);
        _output = AddOutput<IVisionImage>("Image", "Image");
    }

    protected override void OnExecute(IToolContext context)
    {
        var path = _path.Value;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new ToolExecutionException("Image file not found: '{path}'");
        
        var mat = Cv2.ImRead(path, _readMode.Value);
        if (mat.Empty())
        {
            mat.Dispose();
            throw new ToolExecutionException($"Failed to read image: '{path}'");
        }
        _output.Value = new MatVisionImage(mat);
    }
}