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
        Debug.LogWarning("GS: setup dictionaries");
        Dictionary<GridLocation, GridLocation> cameFrom = new Dictionary<GridLocation, GridLocation>();
        Dictionary<GridLocation, double> costSoFar = new Dictionary<GridLocation, double>();

        Debug.LogWarning("GS: setup interval heap");
        var frontier = new C5.IntervalHeap<GridLocation>();
        start.priority = 0;
        frontier.Add(start);
        cameFrom[start] = null;
        costSoFar[start] = 0;

        Debug.LogWarning("GS: while loop BEGIN");
        float exitLoopTime = Time.time + 5f;
        while (!frontier.IsEmpty)
        {
            if (Time.time > exitLoopTime)
            {
                Debug.LogError("GS: grid search timeout");
                return null;
            }

            Debug.LogWarning("GS: while loop entered");
            var current = frontier.DeleteMin();
            if (current.Equals(goal))
            {
                Debug.LogWarning("GS: current == goal");
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
        Debug.LogWarning("GS: rebuild path");
        GridLocation end = goal;
        List<GridLocation> path = new List<GridLocation>();
        Debug.LogWarning("TS: rebuild while BEGIN");
        while (end != null)
        {
            path.Add(end);
            end = cameFrom[end];
        }
        Debug.LogWarning("TS: reverse path");
        path.Reverse();
        Debug.LogWarning("TS: return path");
        return path;
    }

}
