using System;
using System.Collections.Generic;

namespace AssemblyCSharp.Gamelogic.GlobalLayer
{
    public class GridGlobalLayer
    {
        public Bitmap BitMap;
        IGridSearch ASearch;

        public GridGlobalLayer(Improbable.Vector3d topLeft, Improbable.Vector3d bottomRight)
        {
            BitMap = new Bitmap(topLeft, bottomRight);
        }

        public void AddNoFlyZone(NoFlyZone zone)
        {
            BitMap.addNoFlyZone(zone);
        }

        // Converts a grid location back into cartesian coordinate.
        private Improbable.Vector3d convertLocation(GridLocation l)
        {
            return BitMap.getPointFromCoordinates(new int[] { l.x, l.z });
        }

        public List<Improbable.Vector3d> generatePlan(List<Improbable.Vector3d> waypoints)
        {
            // If can not plan for all the waypoints,
            // this will return null to indicate that the route is unachievable.

            List<Improbable.Vector3d> result = new List<Improbable.Vector3d>();
            for (int i = 1; i < waypoints.Count; i++)
            {
                List<Improbable.Vector3d> planSection = generatePointToPointPlan(waypoints[i - 1], waypoints[i]);
                if (planSection == null)
                {
                    return null; // Return null to indicate a plan is unachievable.
                }
                result.AddRange(planSection);
            }
            return result;
        }

        public List<Improbable.Vector3d> generatePointToPointPlan(Improbable.Vector3d p1, Improbable.Vector3d p2)
        {
            //FIND SOME OTHER WAY OF CHECKING IF IN NO FLY ZONE
            if (Environment.GetInstance().isPointInNoFlyZone(p2))
            {
                return null; // A plan can not be found
            }

            int[] coord1 = BitMap.findGridCoordinatesOfPoint(p1);
            int[] coord2 = BitMap.findGridCoordinatesOfPoint(p2);

            GridLocation l1 = new GridLocation(coord1[0], coord1[1]);
            GridLocation l2 = new GridLocation(coord2[0], coord2[1]);

            ASearch = new ThetaStarSearch(true); // Use AStarSearch or ThetaStarSearch here.
            //ASearch = new AStarSearch();

            List<GridLocation> locs = ASearch.run(BitMap, l1, l2);
            if (locs == null)
            { // The case that a path could not be found.
                return null; // Just return empty list.
            }

            // Below lines can be used debug the BitMap and search strategy.
            //Util.DebugUtil.writeStringToFile (BitMap.toString (), "/users/Sam/Desktop/bitmap.txt");
            //Util.DebugUtil.writeStringToFile (ASearch.stringifyPath (locs), "/users/Sam/Desktop/AStar.txt");

            List<Improbable.Vector3d> result = new List<Improbable.Vector3d>();

            // N.B.  As BitMap and A* do not give us Z (altitude) values,
            // we gradually step z from p1 to p2 throughout the plan.
            double yStep = (p2.y - p1.y) / locs.Count;
            double yCurr = p1.y; // starting Z;
            foreach (GridLocation l in locs)
            {
                Improbable.Vector3d convertedLocation = convertLocation(l);
                Improbable.Vector3d location = new Improbable.Vector3d(convertedLocation.x, yCurr,  convertedLocation.z);
                result.Add(location);
                yCurr += yStep;
            }
            return result;
        }

        public double distanceToNoFlyZone(Improbable.Vector3d point)
        {
            // TODO: Implement this
            return 0;
        }
    }
}
