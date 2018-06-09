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

	float potential;
	int rejections;

	// Use this for initialization
	private void OnEnable()
	{
		incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
		potential = DeliveryHandlerWriter.Data.potential;
		rejections = DeliveryHandlerWriter.Data.rejections;

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
			float duration = Vector3.Distance(gameObject.transform.position, handle.Request.destination.ToUnityVector()) / SimulationSettings.MaxDroneSpeed;
			float value = TimeValueFunctions.DeliveryValue(duration, handle.Request.packageInfo, handle.Request.timeValueFunction);
            potential += value;
            ++rejections;
        }
        else
        {
			deliveryRequestQueue.Enqueue(new QueueEntry(Time.time, handle.Request, 0, 0));
            handle.Respond(new DeliveryResponse(true));
        }
    }

	void Scheduler.UpdateDeliveryRequestQueue()
    {
		DeliveryHandlerWriter.Send(new DeliveryHandler.Update()
		                           .SetRequestQueue(new Improbable.Collections.List<QueueEntry>(deliveryRequestQueue.ToArray()))
		                           .SetPotential(potential)
		                           .SetRejections(rejections));
    }

	int Scheduler.GetQueueSize()
	{
		return deliveryRequestQueue.Count;
	}

	int Scheduler.GetPotentialLost()
    {
        return Mathf.RoundToInt(potential);
    }

    float Scheduler.GetAvgPotentialLost()
    {
        return potential / rejections;
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
