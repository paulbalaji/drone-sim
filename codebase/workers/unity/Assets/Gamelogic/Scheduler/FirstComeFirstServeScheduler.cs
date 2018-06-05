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

	Queue<QueueEntry> deliveryRequestQueue;

	int incomingRequests;

	float penalties;

	// Use this for initialization
	private void OnEnable()
	{
		incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
		penalties = DeliveryHandlerWriter.Data.penalties;

		deliveryRequestQueue = new Queue<QueueEntry>((int)SimulationSettings.MaxDeliveryRequestQueueSize);
		foreach (QueueEntry entry in DeliveryHandlerWriter.Data.requestQueue)
        {
			deliveryRequestQueue.Enqueue(entry);
        }

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);
	}

	private void OnDisable()
	{
		deliveryRequestQueue.Clear();

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

	public void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetIncomingDeliveryRequests(++incomingRequests));

        if (deliveryRequestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            handle.Respond(new DeliveryResponse(false));
        }
        else
        {
			deliveryRequestQueue.Enqueue(new QueueEntry(Time.time, handle.Request, 0, 0));
            handle.Respond(new DeliveryResponse(true));
        }
    }

	void Scheduler.UpdateDeliveryRequestQueue()
    {
		DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<QueueEntry>(deliveryRequestQueue.ToArray())));
    }

	int Scheduler.GetQueueSize()
	{
		return deliveryRequestQueue.Count;
	}

	int Scheduler.GetTotalRequests()
	{
		return incomingRequests;
	}

	bool Scheduler.GetNextRequest(out QueueEntry queueEntry)
    {
		if (deliveryRequestQueue.Count > 0)
		{
			queueEntry = deliveryRequestQueue.Dequeue();
			return true;
		}

		queueEntry = new QueueEntry();
		return false;
    }
}
