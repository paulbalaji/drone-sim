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

	long orders;

	private bool LimitDeliveryRequests = false;
	private int MaxDeliveriesToRequest = 2000;

	private void OnEnable()
	{
		Debug.LogWarningFormat("OrderGenerator_{0} Starting Up.", gameObject.EntityId().Id);

		string maxDels;
		LimitDeliveryRequests = SpatialOS.Connection.GetWorkerFlag("drone_sim_max_delivery_requests")
			.TryGetValue(out maxDels);
		
		if (LimitDeliveryRequests && !int.TryParse(maxDels, out MaxDeliveriesToRequest))
		{
			MaxDeliveriesToRequest = 2000;
		}

		orders = OrderWriter.Data.orders;

		deliveriesRequested = MetricsWriter.Data.deliveriesRequested;
		failedRequests = MetricsWriter.Data.failedRequests;
		failedCommands = MetricsWriter.Data.failedCommands;

		UnityEngine.Random.InitState(SimulationSettings.OrderGeneratorSeed);

		if (!LimitDeliveryRequests || deliveriesRequested < MaxDeliveriesToRequest)
		{
			InvokeRepeating("RootSpawnerTick", 0, SimulationSettings.OrderGenerationInterval);
			InvokeRepeating("PrintMetrics", 0, SimulationSettings.OrderGenerationMetricsInterval);
		}
	}

	private void OnDisable()
	{
		CancelInvoke();
	}

	void RootSpawnerTick()
    {
	    if (LimitDeliveryRequests && deliveriesRequested >= MaxDeliveriesToRequest)
	    {
		    Debug.LogWarningFormat("FINALLY REQUESTED {0} DELIVERIES", deliveriesRequested);
		    CancelInvoke();
	    }
	    
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

		OrderWriter.Send(new OrderGeneratorComponent.Update().SetOrders(++orders));

        SpatialOS.Commands.SendCommand(
			OrderWriter,
            DeliveryHandler.Commands.RequestDelivery.Descriptor,
			new DeliveryRequest(orders, deliveryDestination, GeneratePayload(), GenerateTVF(false)),
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

	private DeliveryType GenerateDeliveryType()
	{
		return (DeliveryType) UnityEngine.Random.Range((int)0, (int)SimulationSettings.NumDeliveryTypes);
	}

	// TODO: update properly with TVF A and TVF B
	private TimeValueFunction GenerateTVF(bool random)
	{
		//if bool true, return random tvf
		if (random)
		{
			return GenerateRandomTVF();
		}

        //if bool false, return either TVF B or TVF B by 50% chance of each
		if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
		{
			//return TVF A
			return TimeValueFunctions.GenerateTypeA(GenerateDeliveryType());
		}

		//return TVF B
		return TimeValueFunctions.GenerateTypeB(GenerateDeliveryType());
	}



	private TimeValueFunction GenerateRandomTVF()
	{
		Improbable.Collections.List<bool> steps = new Improbable.Collections.List<bool>(SimulationSettings.TVFSteps);
		int numSteps = 0;
		for (int i = 0; i < steps.Capacity; i++)
		{
			if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
			{
				steps.Add(true);
				++numSteps;
			}
			else
			{
				steps.Add(false);
			}
		}

		return new TimeValueFunction(steps, numSteps, GenerateDeliveryType());
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
