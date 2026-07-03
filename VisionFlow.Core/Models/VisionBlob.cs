namespace VisionFlow.Core.Models;

public sealed class VisionBlob
{
    public int Id { get; set; }
    public Point2d CenterOfMass { get; set; }
    public RectRegion BoundingBox { get; set; }
    public double Area { get; set; }
    public double Perimeter { get; set; }
    
    public double Orientation { get; set; }
    public double MajorAxisLength { get; set; }
    public double MinorAxisLength { get; set; }
    public double AspectRatio { get; set; }
    
    public double Roundness { get; set; }
    
    public double Solidity { get; set; }
    
    public IReadOnlyList<Point2d> Contour { get; set; } = Array.Empty<Point2d>();
}

