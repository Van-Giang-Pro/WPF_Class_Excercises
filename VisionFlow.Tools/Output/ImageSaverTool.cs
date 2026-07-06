using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;

namespace VisionFlow.Tools.Output;

[ToolMetadata("ImageSaver", DisplayName = "Image Saver", Category = "OutputSource", Description = "Save an image to a file (PNG/JPG/BMP/TIFF).")]
public sealed class ImageSaverTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly ToolParameter<string> _outputPath;
    private readonly ToolParameter<string> _fileName;
    private readonly ToolParameter<string> _format;
    private readonly ToolParameter<int> _quality;
    private readonly ToolParameter<bool> _overwrite;
    private readonly OutputPort<string> _savedPath;
    private readonly OutputPort<bool> _success;
    private readonly OutputPort<string> _imageSize;

    public ImageSaverTool()
    {
        _input = AddInput<IVisionImage>("Image", "Image");
        _outputPath = AddParameter("OutputPath", DefaultDir(), "Output Folder", category: "Output", order: 1);
        _fileName = AddParameter("FileName", "saved_image", "File Name", category: "Output", order: 2);
        _format = AddChoiceParameter("ImageFormat", "PNG", new[] { "PNG", "JPG", "BMP", "TIFF" }, "Format", category: "Output", order: 3);
        _quality = AddParameter("Quality", 95, "JPEG Quality", 0, 100, category: "Processing", order: 1);
        _overwrite = AddParameter("OverwriteExisting", true, "Overwrite Existing", category: "Processing", order: 2);
        _savedPath = AddOutput<string>("SavedFilePath", "Saved Path");
        _success = AddOutput<bool>("Success", "Success");
        _imageSize = AddOutput<string>("ImageSize", "Image Size");
    }

    private static string DefaultDir()
    {
        try { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        catch { return Path.GetTempPath(); }
    }

    protected override void OnExecute(IToolContext context)
    {
        var mat = _input.Value!.AsMat();
        if (mat.Empty()) throw new ToolExecutionException("Input image is empty.");

        var dir = _outputPath.Value;
        if (string.IsNullOrWhiteSpace(dir)) throw new ToolExecutionException("Output Folder is empty.");
        Directory.CreateDirectory(dir);

        var ext = _format.Value.ToUpperInvariant() switch
        {
            "JPG" or "JPEG" => ".jpg",
            "BMP" => ".bmp",
            "TIFF" => ".tiff",
            _ => ".png"
        };
        var path = Path.Combine(dir, _fileName.Value + ext);
        if (!_overwrite.Value && File.Exists(path)) path = UniquePath(path);

        int[] pars = ext == ".jpg" ? new[] { (int)ImwriteFlags.JpegQuality, Math.Clamp(_quality.Value, 0, 100) } : Array.Empty<int>();

        var ok = Cv2.ImWrite(path, mat, pars);
        _success.Value = ok;
        if (!ok) throw new ToolExecutionException($"Failed to save image to: {path}");

        _savedPath.Value = path;
        _imageSize.Value = $"{mat.Width}x{mat.Height}";
        context.Log($"ImageSaver: saved {path}");
    }

    private static string UniquePath(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var i = 1;
        var p = path;
        while (File.Exists(p)) p = Path.Combine(dir, $"{name}_{i++}{ext}");
        return p;
    }
}