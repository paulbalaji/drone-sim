using System.Collections.Generic;

namespace AssemblyCSharp.Gamelogic.GlobalLayer
{
    public interface IGridSearch
    {
        double ComputeCost(GridLocation start, GridLocation end,
            Dictionary<GridLocation, GridLocation> cameFrom,
            Dictionary<GridLocation, double> costSoFar, ref bool usingLOS, Bitmap bitmap = null);

        List<GridLocation> run(Bitmap bitmap, GridLocation start, GridLocation end);
    }
}
