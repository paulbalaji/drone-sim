using System;

public class GridLocation : IComparable<GridLocation>
{
    public readonly int x, z;
    public double priority;

    // x represents the logical x, z represents the logical z
    // (x, z) would be a valid point in the bitmap
    // but would need to be converted to Cartesian before being used outside of the Bitmap
    public GridLocation(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override bool Equals(object obj)
    {
        return obj != null && obj.GetType() == typeof(GridLocation) && ((GridLocation)obj).x == x && ((GridLocation)obj).z == z;
    }

    public override int GetHashCode()
    {
        return ((7 * x + 11 * z) / 51) << 8;
    }

    public int CompareTo(GridLocation other)
    {
        if (other.Equals(this))
        {
            return 0;
        }
        else if (priority < other.priority)
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

    public override string ToString()
    {
        return string.Format("Grid Location: {0} {1}", x, z);
    }

    public double distanceTo(GridLocation b)
    {
        return Math.Sqrt(Math.Pow((Math.Abs(x - b.x)), 2) + Math.Pow((Math.Abs(z - b.z)), 2));
    }

}
