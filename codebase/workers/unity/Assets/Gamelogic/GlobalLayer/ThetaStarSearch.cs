using System;
using System.Collections.Generic;
using UnityEngine;

public class ThetaStarSearch : IGridSearch
{
    private bool lazy;
    public int LOSchecks;
    private HashSet<GridLocation> closedSet;

    public ThetaStarSearch(bool lazy = false)
    {
        this.lazy = lazy;
        if (lazy)
        {
            closedSet = new HashSet<GridLocation>();
        }
    }

    public void SetVertex(Bitmap bitmap, Dictionary<GridLocation, GridLocation> cameFrom,
        Dictionary<GridLocation, double> costSoFar, GridLocation current)
    {
        GridLocation parent = cameFrom[current];
        if (parent == null || (parent != null && !bitmap.lineOfSight(parent, current)))
        {
            ++LOSchecks;
            HashSet<GridLocation> neighbours = bitmap.Neighbours(current);
            neighbours.IntersectWith(closedSet);
            GridLocation bestLocation = null;
            double bestCost = int.MaxValue;
            foreach (GridLocation n in neighbours)
            {
                double nCost = costSoFar[n] + n.distanceTo(current);
                if (bestLocation == null || nCost < bestCost)
                {
                    bestLocation = n;
                    bestCost = nCost;
                }
            }
            cameFrom[current] = bestLocation;
            costSoFar[current] = bestCost;
        }
    }

    public double lazyComputeCost(Dictionary<GridLocation, GridLocation> cameFrom,
        Dictionary<GridLocation, double> costSoFar, GridLocation start, GridLocation end)
    {
        var parent = cameFrom[start];
        if (parent != null)
        {
            return costSoFar[parent] + parent.distanceTo(end);
        }
        return start.distanceTo(end);
    }


    public List<GridLocation> run(Bitmap bitmap, GridLocation start, GridLocation end)
    {
        if (lazy)
        {
            return LazyRun(bitmap, start, end);
        }
        return GridSearch.run(bitmap, start, end, ComputeCost);
    }

    private List<GridLocation> LazyRun(Bitmap bitmap, GridLocation start, GridLocation goal)
    {
        //Debug.LogWarning("TS: setup dictionaries");
        Dictionary<GridLocation, GridLocation> cameFrom = new Dictionary<GridLocation, GridLocation>();
        Dictionary<GridLocation, double> costSoFar = new Dictionary<GridLocation, double>();

        //Debug.LogWarning("TS: setup interval heap");
        var frontier = new C5.IntervalHeap<GridLocation>();
        start.priority = 0;
        frontier.Add(start);
        cameFrom[start] = null;
        costSoFar[start] = 0;

        //Debug.LogWarning("TS: while loop BEGIN");
        float exitLoopTime = Time.time + 5f;
        while (!frontier.IsEmpty)
        {
            if (Time.time > exitLoopTime)
            {
                Debug.LogError("TS: theta star timeout");
                return null;
            }

            //Debug.LogWarning("TS: while loop entered");
            GridLocation current = frontier.DeleteMin();
            //Debug.LogWarning("TS: while loop SetVertex");
            SetVertex(bitmap, cameFrom, costSoFar, current);
            if (current.Equals(goal))
            {
                //Debug.LogWarning("TS: current == goal");
                return GridSearch.RebuildPath(goal, cameFrom);
            }

            closedSet.Add(current);
            foreach (GridLocation next in bitmap.Neighbours(current))
            {
                double computedCost = lazyComputeCost(cameFrom, costSoFar, current, next);
                if (!costSoFar.ContainsKey(next) || computedCost < costSoFar[next])
                {
                    cameFrom[next] = cameFrom[current];
                    costSoFar[next] = computedCost;
                    double p = computedCost + next.distanceTo(goal);
                    next.priority = p;
                    frontier.Add(next);
                }
            }
        }

        Debug.LogError("TS: returning null");
        return null;
    }


    public double ComputeCost(GridLocation start, GridLocation end,
        Dictionary<GridLocation, GridLocation> cameFrom,
        Dictionary<GridLocation, double> costSoFar, ref bool usingLOS, Bitmap bitmap)
    {
        Debug.Assert(bitmap != null);
        var parent = cameFrom[start];
        if (parent != null && bitmap.lineOfSight(parent, end))
        {
            ++LOSchecks;
            usingLOS = true;
            return costSoFar[parent] + parent.distanceTo(end);
        }
        double c = (Math.Abs(start.x - end.x) == 1 && Math.Abs(start.z - end.z) == 1) ? Math.Sqrt(2) : 1;
        return costSoFar[start] + c;
    }
}
