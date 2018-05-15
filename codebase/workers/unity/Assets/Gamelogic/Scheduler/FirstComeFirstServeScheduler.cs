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
using System.Collections.Generic;
using UnityEngine;

public class FirstComeFirstServeScheduler : MonoBehaviour, Scheduler
{
	[Require]
    private ControllerMetrics.Writer MetricsWriter;

	[Require]
    private DeliveryHandler.Writer DeliveryHandlerWriter;

	Queue<DeliveryRequest> deliveryRequestQueue;

	int incomingRequests;

	// Use this for initialization
	private void OnEnable()
	{
		incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;

		deliveryRequestQueue = new Queue<DeliveryRequest>((int)SimulationSettings.MaxDeliveryRequestQueueSize);
        foreach (DeliveryRequest request in DeliveryHandlerWriter.Data.requestQueue)
        {
            deliveryRequestQueue.Enqueue(request);
        }

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);
	}

	private void OnDisable()
	{
		deliveryRequestQueue.Clear();

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

	void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetIncomingDeliveryRequests(++incomingRequests));

        if (deliveryRequestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            handle.Respond(new DeliveryResponse(false));
        }
        else
        {
            deliveryRequestQueue.Enqueue(handle.Request);
            handle.Respond(new DeliveryResponse(true));
        }
    }

	void Scheduler.UpdateDeliveryRequestQueue()
    {
        DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<DeliveryRequest>(deliveryRequestQueue.ToArray())));
    }

	int Scheduler.GetQueueSize()
	{
		return deliveryRequestQueue.Count;
	}

	int Scheduler.GetTotalRequests()
	{
		return incomingRequests;
	}

	bool Scheduler.GetNextRequest(out DeliveryRequest deliveryRequest)
    {
		if (deliveryRequestQueue.Count > 0)
		{
			deliveryRequest = deliveryRequestQueue.Dequeue();
			return true;
		}

		deliveryRequest = new DeliveryRequest();
		return false;
    }
}
