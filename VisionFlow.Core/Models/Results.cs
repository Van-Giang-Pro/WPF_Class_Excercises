namespace VisionFlow.Core.Models;

public abstract class VisionResult
{
    public Judge Judge { get; set; } = Judge.None;
}

public sealed class CircleResult : VisionResult
{
    public Circle Circle { get; set; }
    
    public double Score { get; set; }
}

public sealed class LineResult : VisionResult
{
    public LineSegment Segment { get; set; }
    
    public double AngleDeg { get; set; }
}

public sealed class AlignResult : VisionResult
{
    public XYTOffset Offset { get; set; }
}