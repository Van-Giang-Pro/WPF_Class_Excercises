namespace VisionFlow.Core.Models;

public enum Judge
{ 
    None, 
    OK, 
    NG
}

public readonly record struct Point2d(double X, double Y);

public readonly record struct Circle(Point2d Center, double Radius);

public readonly record struct LineSegment(Point2d Start, Point2d End);

public readonly record struct RectRegion(double X, double Y, double Width, double Height);

public readonly record struct XYTOffset(double X, double Y, double Theta);