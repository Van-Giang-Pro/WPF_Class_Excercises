namespace VisionFlow.Core.Tools;

public class IToolContext
{
    CancellationToken CancellationToken { get; }
    
    double PixelSize { get; }
    
    void Log(string message);
}