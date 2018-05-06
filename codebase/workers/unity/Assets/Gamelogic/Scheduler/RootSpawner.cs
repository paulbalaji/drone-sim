using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Scheduler;
using Improbable.Metrics;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using System;
using System.Collections;
using UnityEngine;

public class RootSpawner : MonoBehaviour
{
    [Require]
    private Position.Writer PositionWriter;

    [Require]
    private Scheduler.Writer SchedulerWriter;

    [Require]
    private SchedulerMetrics.Writer MetricsWriter;

    int deliveriesRequested;

	void Start()
	{
        InvokeRepeating("RootSpawnerTick", SimulationSettings.DroneSpawnerSpacing, SimulationSettings.DroneSpawnerSpacing);
        InvokeRepeating("PrintMetrics", SimulationSettings.SchedulerMetricsInterval, SimulationSettings.SchedulerMetricsInterval);
	}

	private void OnEnable()
	{
        deliveriesRequested = MetricsWriter.Data.deliveriesRequested;
	}

	void RootSpawnerTick()
    {
        Vector3f deliveryDestination = GetNonNFZPoint();
        EntityId closestController = GetClosestController(deliveryDestination);

        SpatialOS.Commands.SendCommand(
            PositionWriter,
            DeliveryHandler.Commands.RequestDelivery.Descriptor,
            new DeliveryRequest(deliveryDestination),
            closestController)
                 .OnSuccess(HandleCommandSuccessCallback);
    }

    void HandleCommandSuccessCallback(DeliveryResponse response)
    {
        deliveriesRequested++;
        SendMetrics();
        //PrintMetrics();
    }

    void SendMetrics()
    {
        MetricsWriter.Send(new SchedulerMetrics.Update().SetDeliveriesRequested(deliveriesRequested));
    }

    void PrintMetrics()
    {
        Debug.LogWarningFormat("METRICS Scheduler_{0} Deliveries_Requested {1}", gameObject.EntityId().Id, deliveriesRequested);
    }

    private EntityId GetClosestController(Vector3f destination)
    {
        Vector3 dst = destination.ToUnityVector();
        EntityId closestId = new EntityId();
        float closest = float.MaxValue;
        foreach (ControllerInfo controller in SchedulerWriter.Data.controllers)
        {
            float distance = Vector3.Distance(dst, controller.location.ToUnityVector());
            if (distance < closest)
            {
                closest = distance;
                closestId = controller.controllerId;
            }
        }

        return closestId;
    }

    bool ValidPoint(ref Vector3f point)
    {
        foreach (Improbable.Controller.NoFlyZone zone in SchedulerWriter.Data.zones)
        {
            if (NoFlyZone.hasCollidedWith(zone, point))
            {
                return false;
            }
            
        }

        return true;
    }

    private Vector3f GetNonNFZPoint()
    {
        var squareSize = SimulationSettings.squareSize;

        float randX, randZ;
        Vector3f point = new Vector3f();

        do
        {
            point.x = UnityEngine.Random.Range(-SimulationSettings.maxX, SimulationSettings.maxX);
            point.z = UnityEngine.Random.Range(-SimulationSettings.maxZ, SimulationSettings.maxZ);
        } while (!ValidPoint(ref point));

        return point;
    }
}
