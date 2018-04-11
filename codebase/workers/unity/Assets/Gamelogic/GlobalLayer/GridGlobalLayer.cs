﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;

public class GridGlobalLayer : MonoBehaviour
{
    [Require]
    private BitmapComponent.Writer BitmapWriter;

    [Require]
    private GlobalLayer.Writer GlobalLayerWriter;

    Bitmap bitmap;

    IGridSearch ASearch;
    public Improbable.Collections.List<Improbable.Controller.NoFlyZone> zones;

    public void InitGlobalLayer(Improbable.Vector3d topLeft, Improbable.Vector3d bottomRight)
    {
        bitmap = gameObject.GetComponent<Bitmap>();
        bitmap.InitialiseBitmap(topLeft, bottomRight);
        bitmap.updateWithNoFlyZones(GlobalLayerWriter.Data.zones);
    }

    private void OnEnable()
    {
        bitmap = gameObject.GetComponent<Bitmap>();
        zones = GlobalLayerWriter.Data.zones;

        GlobalLayerWriter.ZonesUpdated.Add(HandleZonesUpdate);
    }

    void HandleZonesUpdate(Improbable.Collections.List<Improbable.Controller.NoFlyZone> updatedZones)
    {
        zones = updatedZones;
    }

    public void AddNoFlyZones(Improbable.Controller.NoFlyZone[] noFlyZones)
    {
        foreach (Improbable.Controller.NoFlyZone zone in noFlyZones)
        {
            AddNoFlyZone(zone, false);
        }
        SendZonesUpdate();
    }

    public void AddNoFlyZone(Improbable.Controller.NoFlyZone zone, bool sendUpdate = true)
    {
        zones.Add(zone);
        if (sendUpdate)
        {
            SendZonesUpdate();
        }

        bitmap.addNoFlyZone(zone);
    }

    private void SendZonesUpdate()
    {
        GlobalLayerWriter.Send(new GlobalLayer.Update().SetZones(zones));
    }

    // Converts a grid location back into cartesian coordinate.
    private Improbable.Vector3d convertLocation(GridLocation l)
    {
        return bitmap.getPointFromCoordinates(new int[] { l.x, l.z });
    }

    public Improbable.Collections.List<Improbable.Vector3f> generatePlan(List<Improbable.Vector3d> waypoints)
    {
        // If can not plan for all the waypoints,
        // this will return null to indicate that the route is unachievable.

        Improbable.Collections.List<Improbable.Vector3f> result = new Improbable.Collections.List<Improbable.Vector3f>();
        for (int i = 1; i < waypoints.Count; i++)
        {
            Improbable.Collections.List<Improbable.Vector3f> planSection = generatePointToPointPlan(waypoints[i - 1], waypoints[i]);
            if (planSection == null)
            {
                return null; // Return null to indicate a plan is unachievable.
            }
            result.AddRange(planSection);
        }
        return result;
    }

    private bool isPointInNoFlyZone(Improbable.Vector3d point)
    {
        foreach (Improbable.Controller.NoFlyZone zone in zones)
        {
            if (NoFlyZone.hasCollidedWith(zone, point))
            {
                return true;
            }
        }
        return false;
    }

    public Improbable.Collections.List<Improbable.Vector3f> generatePointToPointPlan(Improbable.Vector3f p1, Improbable.Vector3f p2)
    {
        if (isPointInNoFlyZone(p2))
        {
            return null; // A plan can not be found
        }

        int[] coord1 = bitmap.findGridCoordinatesOfPoint(p1);
        int[] coord2 = bitmap.findGridCoordinatesOfPoint(p2);

        GridLocation l1 = new GridLocation(coord1[0], coord1[1]);
        GridLocation l2 = new GridLocation(coord2[0], coord2[1]);

        ASearch = new ThetaStarSearch(true); // Use AStarSearch or ThetaStarSearch here.
        //ASearch = new AStarSearch();

        List<GridLocation> locs = ASearch.run(bitmap, l1, l2);
        if (locs == null)
        { // The case that a path could not be found.
            return null; // Just return empty list.
        }

        // Below lines can be used debug the BitMap and search strategy.
        //Util.DebugUtil.writeStringToFile (BitMap.toString (), "/users/Sam/Desktop/bitmap.txt");
        //Util.DebugUtil.writeStringToFile (ASearch.stringifyPath (locs), "/users/Sam/Desktop/AStar.txt");

        Improbable.Collections.List<Improbable.Vector3f> result = new Improbable.Collections.List<Improbable.Vector3f>();

        // N.B.  As BitMap and A* do not give us Z (altitude) values,
        // we gradually step z from p1 to p2 throughout the plan.
        double yStep = (p2.y - p1.y) / locs.Count;
        double yCurr = p1.y; // starting Z;
        foreach (GridLocation l in locs)
        {
            Improbable.Vector3f convertedLocation = convertLocation(l);
            Improbable.Vector3f location = new Improbable.Vector3f(convertedLocation.x, yCurr,  convertedLocation.z);
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
