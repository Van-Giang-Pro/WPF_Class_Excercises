namespace VisionFlow.Core.Imaging;

public interface IVisionImage : IDisposable
{
    int Width { get; }
    int Height { get; }
    int Channels { get; }
    bool IsDisposed { get; }
    IVisionImage Clone();
}