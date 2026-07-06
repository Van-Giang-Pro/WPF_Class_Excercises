using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Models;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;

namespace VisionFlow.Tools.Utility;

[ToolMetadata("RegionSelector", DisplayName = "Region Selector", Category = "Utility", Description = "Crop a rotated rectangular ROI; draw the region on the image in the editor.")]
public sealed class RegionSelectorTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly OutputPort<IVisionImage> _output;
    private readonly ToolParameter<RotatedRectRegion> _region;

    public RegionSelectorTool()
    {
        _input = AddInput<IVisionImage>("Image", "Image");
        _output = AddOutput<IVisionImage>("Image", "Cropped");
        _region = AddParameter("Region", new RotatedRectRegion(new VisionFlow.Core.Models.Point2d(150, 120), 200, 150, 0), "ROI", category: "Region", order: 1, interaction: ParameterInteraction.RotatedRectRegion);
    }

    protected override void OnExecute(IToolContext context)
    {
        var src = _input.Value!.AsMat();
        var r = _region.Value;

        var center = new Point2f((float)r.Center.X, (float)r.Center.Y);
        var size = new Size((int)Math.Max(1, r.Width), (int)Math.Max(1, r.Height));

        Mat source = src;
        var rotated = false;
        if (Math.Abs(r.AngleDeg) > 1e-3)
        {
            using var rot = Cv2.GetRotationMatrix2D(center, r.AngleDeg, 1.0);
            source = new Mat();
            Cv2.WarpAffine(src, source, rot, src.Size(), InterpolationFlags.Linear, BorderTypes.Replicate);
            rotated = true;
        }

        var dst = new Mat();
        Cv2.GetRectSubPix(source, size, center, dst);
        if (rotated) source.Dispose();
        _output.Value = new MatVisionImage(dst);
    }
}