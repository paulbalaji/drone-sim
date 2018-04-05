using System;

namespace AssemblyCSharp.Gamelogic.GlobalLayer
{
    public class GridLocation : IComparable<GridLocation>
    {
        public readonly int x, z;
        public double priority;

        // N.B: GridLocation x represents the first index into the 2D bitmap.  GirdLocation y represents second index into the 2D bitmap.
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
            return string.Format("Location: {0} {1}", x, z);
        }

        public double distanceTo(GridLocation b)
        {
            return Math.Sqrt(Math.Pow((Math.Abs(x - b.x)), 2) + Math.Pow((Math.Abs(z - b.z)), 2));
        }

    }
}