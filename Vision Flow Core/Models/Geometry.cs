namespace Vision_Flow_Core.Models;

public class Geometry
{
    public enum Judge
    {
        None,
        OK,
        NG
    }
}

public readonly record struct Point2d(double X, double Y);

