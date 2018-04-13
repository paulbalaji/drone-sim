using System;
using System.Collections.Generic;
using UnityEngine;

public static class GridSearch
{
    public delegate double ComputeCost(GridLocation start, GridLocation end,
        Dictionary<GridLocation, GridLocation> cameFrom,
        Dictionary<GridLocation, double> costSoFar, ref bool usingLOS, Bitmap bitmap = null);


    public static List<GridLocation> run(Bitmap bitmap, GridLocation start, GridLocation goal,
        ComputeCost computeCost)
    {
        if (start.Equals(goal))
        {
            Debug.LogError("GS: start == goal");
            // Return start and goal here so that we interpolate z values
            return new List<GridLocation> { start, goal };
        }

        Debug.LogError("GS: setup dictionaries");
        Dictionary<GridLocation, GridLocation> cameFrom = new Dictionary<GridLocation, GridLocation>();
        Dictionary<GridLocation, double> costSoFar = new Dictionary<GridLocation, double>();

        Debug.LogError("GS: setup interval heap");
        var frontier = new C5.IntervalHeap<GridLocation>();
        start.priority = 0;
        frontier.Add(start);
        cameFrom[start] = null;
        costSoFar[start] = 0;
        Debug.LogError("GS: while loop BEGIN");
        while (!frontier.IsEmpty)
        {
            Debug.LogError("GS: while loop entered");
            var current = frontier.DeleteMin();
            if (current.Equals(goal))
            {
                Debug.LogError("GS: current == goal");
                return RebuildPath(goal, cameFrom);
            }
            foreach (GridLocation next in bitmap.Neighbours(current))
            {
                bool usingLOS = false;
                double computedCost = computeCost(current, next, cameFrom, costSoFar, ref usingLOS, bitmap);
                if (!costSoFar.ContainsKey(next) || computedCost < costSoFar[next])
                {
                    if (usingLOS)
                    {
                        cameFrom[next] = cameFrom[current];
                    }
                    else
                    {
                        cameFrom[next] = current;
                    }
                    costSoFar[next] = computedCost;
                    double p = computedCost + next.distanceTo(goal);
                    next.priority = p;
                    frontier.Add(next);
                }
            }
        }
        Debug.LogError("GS: returning null");
        return null;
    }

    public static List<GridLocation> RebuildPath(GridLocation goal, Dictionary<GridLocation, GridLocation> cameFrom)
    {
        Debug.LogError("GS: rebuild path");
        GridLocation end = goal;
        List<GridLocation> path = new List<GridLocation>();
        Debug.LogError("TS: rebuild while BEGIN");
        while (end != null)
        {
            path.Add(end);
            end = cameFrom[end];
        }
        Debug.LogError("TS: reverse path");
        path.Reverse();
        Debug.LogError("TS: return path");
        return path;
    }

}
