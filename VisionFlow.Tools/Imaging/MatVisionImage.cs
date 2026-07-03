using OpenCvSharp;
using VisionFlow.Core.Imaging;

namespace VisionFlow.Tools.Imaging;

public class MatVisionImage : IVisionImage
{
    public MatVisionImage(Mat mat)
    {
        Mat = mat ?? throw new ArgumentNullException(nameof(mat));
    }
    
    public Mat Mat { get; }

    public int Width => Mat.Width;
    public int Height => Mat.Height;
    public int Channels => Mat.Channels();
    public bool IsDisposed => Mat.IsDisposed;

    public IVisionImage Clone() => new MatVisionImage(Mat.Clone());

    public void Dispose()
    {
        if (!Mat.IsDisposed) Mat.Dispose();
    }
}

public static class VisionImageExtensions
{
    public static Mat AsMat(this IVisionImage image)
    {
        if (image is MatVisionImage m) return m.Mat;
        throw new InvalidOperationException($"Ảnh không phải MatVisionImage (thực tế: {image.GetType().Name})");
    }
}
