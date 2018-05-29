using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Orders;
using Improbable.Metrics;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using System;
using System.Collections;
using UnityEngine;

public class OrderGeneratorBehaviour : MonoBehaviour
{
    [Require]
	private  OrderGeneratorComponent.Writer OrderWriter;

    [Require]
	private OrderGeneratorMetrics.Writer MetricsWriter;

    int deliveriesRequested;
	int failedRequests;
	int failedCommands;

	private void OnEnable()
	{
		Debug.LogWarningFormat("OrderGenerator_{0} Starting Up.", gameObject.EntityId().Id);

		deliveriesRequested = MetricsWriter.Data.deliveriesRequested;
		failedRequests = MetricsWriter.Data.failedRequests;
		failedCommands = MetricsWriter.Data.failedCommands;

        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);

		InvokeRepeating("RootSpawnerTick", 0, SimulationSettings.OrderGenerationInterval);
		InvokeRepeating("PrintMetrics", 0, SimulationSettings.OrderGenerationMetricsInterval);
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
			OrderWriter,
            DeliveryHandler.Commands.RequestDelivery.Descriptor,
			new DeliveryRequest(deliveryDestination, GeneratePayload()),
            closestController)
		         .OnSuccess((response) => DeliveryRequestCallback(response.success))
		         .OnFailure((response) => DeliveryRequestFail());
    }

    void DeliveryRequestCallback(bool success)
    {
		if (success)
		{
			MetricsWriter.Send(new OrderGeneratorMetrics.Update().SetDeliveriesRequested(++deliveriesRequested));
		}
		else
		{
			MetricsWriter.Send(new OrderGeneratorMetrics.Update().SetFailedRequests(++failedRequests));
		}
    }

    void DeliveryRequestFail()
	{
		MetricsWriter.Send(new OrderGeneratorMetrics.Update().SetFailedCommands(++failedCommands));
	}

    void PrintMetrics()
    {
		Debug.LogWarningFormat("METRICS Scheduler_{0} Success {1} Fails {2} Errors {3}"
		                       , gameObject.EntityId().Id
		                       , deliveriesRequested
		                       , failedRequests
		                       , failedCommands);
    }

	private PackageInfo GeneratePayload()
	{
		return PayloadGenerator.GetNextPackage();
	}

    private EntityId GetClosestController(Vector3f destination)
    {
        Vector3 dst = destination.ToUnityVector();
        EntityId closestId = new EntityId(-1);
        float closest = float.MaxValue;
		foreach (ControllerInfo controller in OrderWriter.Data.controllers)
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
        bool invalidPoint = false;

        //10 attempts per second to get a valid point
		for (int i = 0; i < 10; i++)
		{
			point.x = UnityEngine.Random.Range(-SimulationSettings.maxX, SimulationSettings.maxX);
            point.z = UnityEngine.Random.Range(-SimulationSettings.maxZ, SimulationSettings.maxZ);
			invalidPoint = NoFlyZone.hasCollidedWithAny(OrderWriter.Data.zones, point);

			if (!invalidPoint)
			{
				return point;
			}
		}

		return new Vector3f(0, -1, 0);
    }
}
