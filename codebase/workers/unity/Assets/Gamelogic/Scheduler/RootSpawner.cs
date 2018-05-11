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

	private void OnEnable()
	{
		Debug.LogWarningFormat("Controller_{0} Starting Up.", gameObject.EntityId().Id);

		deliveriesRequested = MetricsWriter.Data.deliveriesRequested;

        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);

		InvokeRepeating("RootSpawnerTick", SimulationSettings.SchedulerInterval, SimulationSettings.SchedulerInterval);
        InvokeRepeating("PrintMetrics", SimulationSettings.SchedulerMetricsInterval, SimulationSettings.SchedulerMetricsInterval);
	}

	private void OnDisable()
	{
		CancelInvoke();
	}

	void RootSpawnerTick()
    {
        Vector3f deliveryDestination = GetNonNFZPoint();
        if (deliveryDestination.y < 0)
        {
            return;
        }

        EntityId closestController = GetClosestController(deliveryDestination);
        if (closestController.Id < 0)
        {
            return;
        }

        SpatialOS.Commands.SendCommand(
            PositionWriter,
            DeliveryHandler.Commands.RequestDelivery.Descriptor,
            new DeliveryRequest(deliveryDestination),
            closestController)
                 .OnSuccess(HandleCommandSuccessCallback);
    }

    void HandleCommandSuccessCallback(DeliveryResponse response)
    {
        if (response.success)
        {
			MetricsWriter.Send(new SchedulerMetrics.Update().SetDeliveriesRequested(++deliveriesRequested));
        }
    }

    void PrintMetrics()
    {
        Debug.LogWarningFormat("METRICS Scheduler_{0} Deliveries_Requested {1}", gameObject.EntityId().Id, deliveriesRequested);
    }

    private EntityId GetClosestController(Vector3f destination)
    {
        Vector3 dst = destination.ToUnityVector();
        EntityId closestId = new EntityId(-1);
        float closest = float.MaxValue;
        foreach (ControllerInfo controller in SchedulerWriter.Data.controllers)
        {
            float distance = Vector3.Distance(dst, controller.location.ToUnityVector());
            if (distance < closest && distance > SimulationSettings.MinimumDeliveryDistance)
            {
                closest = distance;
                closestId = controller.controllerId;
            }
        }

        return closestId;
    }

    private Vector3f GetNonNFZPoint()
    {
        float randX, randZ;
        Vector3f point = new Vector3f();
        int tryCount = 0;
        bool invalidPoint = false;

        do
        {
            point.x = UnityEngine.Random.Range(-SimulationSettings.maxX, SimulationSettings.maxX);
            point.z = UnityEngine.Random.Range(-SimulationSettings.maxZ, SimulationSettings.maxZ);
            tryCount++;
            invalidPoint = NoFlyZone.hasCollidedWithAny(SchedulerWriter.Data.zones, point);
        } while (invalidPoint && tryCount < 10);

        if (invalidPoint)
        {
            return new Vector3f(0, -1, 0);
        }

        return point;
    }
}
