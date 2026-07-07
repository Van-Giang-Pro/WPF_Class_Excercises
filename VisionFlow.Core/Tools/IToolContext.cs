namespace VisionFlow.Core.Tools;

public interface IToolContext
{
    CancellationToken CancellationToken { get; }
    
    double PixelSize { get; }
    
    void Log(string message);
}