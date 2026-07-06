using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Models;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;
using P2 = VisionFlow.Core.Models.Point2d;

namespace VisionFlow.Tools.Finding;

[ToolMetadata("FindCircle", DisplayName = "Find Circle", Category = "Detection", Description = "Caliper based circle finder : radial calipers + edge detection + circle fit")]
public sealed class FindCircleTool : VisionTool
{
    
}