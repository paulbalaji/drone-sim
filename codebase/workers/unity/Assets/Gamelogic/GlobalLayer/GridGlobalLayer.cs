using System;
using System.Collections.Generic;
using Assets.Gamelogic.Core;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;

[WorkerType(WorkerPlatform.UnityWorker)]
public class GridGlobalLayer : MonoBehaviour
{
    [Require]
    private BitmapComponent.Writer BitmapWriter;

    [Require]
    private GlobalLayer.Writer GlobalLayerWriter;

    Bitmap bitmap;

    IGridSearch ASearch;
    public Improbable.Collections.List<Improbable.Controller.NoFlyZone> zones;

    public void InitGlobalLayer(Improbable.Vector3f topLeft, Improbable.Vector3f bottomRight)
    {
        //Debug.LogWarning("call init bitmap " + bitmap != null);
        bitmap.InitialiseBitmap(topLeft, bottomRight);
        //Debug.LogWarning("Bitmap Ready");
        bitmap.updateWithNoFlyZones(GlobalLayerWriter.Data.zones);
        //Debug.LogWarning("Bitmap w/NFZs Ready");
    }

    private void OnEnable()
    {
        bitmap = gameObject.GetComponent<Bitmap>();
        zones = GlobalLayerWriter.Data.zones;

        //GlobalLayerWriter.ZonesUpdated.Add(HandleZonesUpdate);
    }

	private void OnDisable()
	{
        //GlobalLayerWriter.ZonesUpdated.Remove(HandleZonesUpdate);
	}

	//void HandleZonesUpdate(Improbable.Collections.List<Improbable.Controller.NoFlyZone> updatedZones)
    //{
    //    zones = updatedZones;
    //}

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
        bitmap.addNoFlyZone(zone, sendUpdate);

        if (sendUpdate)
        {
            SendZonesUpdate();
        }
    }

    private void SendZonesUpdate()
    {
        GlobalLayerWriter.Send(new GlobalLayer.Update().SetZones(zones));
    }

    // Converts a grid location back into cartesian coordinate.
    private Improbable.Vector3f convertLocation(GridLocation l)
    {
        return bitmap.getPointFromGridCoordinates(new int[] { l.x, l.z });
    }

    public Improbable.Collections.List<Improbable.Vector3f> generatePlan(List<Improbable.Vector3f> waypoints)
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

    public bool isPointInNoFlyZone(Improbable.Vector3f point)
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
            Debug.LogError("next target is in a NFZ");
            return null; // A plan can not be found
        }

        int[] coord1 = bitmap.findGridCoordinatesOfPoint(p1);
        if (coord1 == null) {
            Debug.LogError("coord1 fail");
            return null;
        }

        int[] coord2 = bitmap.findGridCoordinatesOfPoint(p2);
        if (coord1 == null)
        {
            Debug.LogError("coord2 fail");
            return null;
        }

        GridLocation l1 = new GridLocation(coord1[0], coord1[1]);
        GridLocation l2 = new GridLocation(coord2[0], coord2[1]);

        ASearch = new ThetaStarSearch(true); // Use AStarSearch or ThetaStarSearch here.

        List<GridLocation> locs = null;
        float droneHeight = SimulationSettings.SuggestedDroneHeight;
        if (Vector3.Distance(p1.ToUnityVector(), p2.ToUnityVector()) < SimulationSettings.RoutingShortCircuitThreshold) {
            //Debug.LogWarning("Global Layer: within short circuit threshold");
            droneHeight = SimulationSettings.MinimumDroneHeight;
        } else {
            locs = ASearch.run(bitmap, l1, l2);

            if (locs == null)
            { // The case that a path could not be found.
                Debug.LogError("search fail");
                return null; // Just return empty list.
            }
        }

        //Debug.LogWarning("NUM VERTICES IN PATH: " + locs.Count);

        Improbable.Collections.List<Improbable.Vector3f> result = new Improbable.Collections.List<Improbable.Vector3f>();

        // N.B.  As BitMap and A* do not give us Y (height) values,
        // we gradually step y from p1 to p2 throughout the plan.
        //float yStep = (p2.y - p1.y) / locs.Count;
        //float yCurr = p1.y; // starting Y;

        result.Add(new Vector3f(p1.x, droneHeight, p1.z));
        if (locs == null)
        {
            //if locs null at this stage that means start/end in the same 25x25 grid cell
            //then just make sure penultimate destination is p2 x/z at the required drone height
            result.Add(new Vector3f(p2.x, droneHeight, p2.z));
        }

        foreach (GridLocation l in locs)
        {
            Improbable.Vector3f convertedLocation = convertLocation(l);
            Improbable.Vector3f location = new Improbable.Vector3f(convertedLocation.x, droneHeight, convertedLocation.z);

            if (Vector3.Distance(location.ToUnityVector(), p2.ToUnityVector()) > SimulationSettings.RoutingShortCircuitThreshold)
            {
                result.Add(location);
                //yCurr += yStep;
            }
        }

        result.Add(p2);
        return result;
    }

    public double distanceToNoFlyZone(Improbable.Vector3f point)
    {
        return bitmap.distanceToNoFlyZone(point);
    }
}
