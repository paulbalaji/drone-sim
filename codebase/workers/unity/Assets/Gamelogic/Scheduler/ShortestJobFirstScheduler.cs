﻿using Assets.Gamelogic.Core;
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

public class ShortestJobFirstScheduler : MonoBehaviour, Scheduler
{
	[Require]
    private ControllerMetrics.Writer MetricsWriter;

	[Require]
    private DeliveryHandler.Writer DeliveryHandlerWriter;

	SortedSet<QueueEntry> requestQueue;

	int incomingRequests;

	float penalties;

	// Use this for initialization
	private void OnEnable()
	{
		incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
		penalties = DeliveryHandlerWriter.Data.penalties;

		requestQueue = new SortedSet<QueueEntry>(new SJFComparer());

		foreach (QueueEntry entry in DeliveryHandlerWriter.Data.requestQueue)
        {
			requestQueue.Add(entry);
        }

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);
	}

	private void OnDisable()
	{
		requestQueue.Clear();

		DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

	public void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetIncomingDeliveryRequests(++incomingRequests));

		float estimatedTime = Vector3.Distance(gameObject.transform.position, handle.Request.destination.ToUnityVector()) / SimulationSettings.MaxDroneSpeed;
		QueueEntry queueEntry = new QueueEntry(Time.time, handle.Request, 0, estimatedTime);

		if (requestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
			QueueEntry longestJob = requestQueue.Max;
			if (estimatedTime > longestJob.expectedDuration)
			{
				handle.Respond(new DeliveryResponse(false));
				return;
			}

			requestQueue.Remove(longestJob);
        }

		requestQueue.Add(queueEntry);
        handle.Respond(new DeliveryResponse(true));
    }

	void Scheduler.UpdateDeliveryRequestQueue()
    {
		Improbable.Collections.List<QueueEntry> queueList = new Improbable.Collections.List<QueueEntry>();
		foreach (QueueEntry entry in requestQueue)
        {
			queueList.Add(entry);
        }
		DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(queueList));
    }

	int Scheduler.GetQueueSize()
	{
		return requestQueue.Count;
	}

	int Scheduler.GetTotalRequests()
	{
		return incomingRequests;
	}

	bool Scheduler.GetNextRequest(out QueueEntry queueEntry)
    {
		if (requestQueue.Count > 0)
		{
			queueEntry = requestQueue.Min;
			requestQueue.Remove(queueEntry);
			return true;
		}

		queueEntry = new QueueEntry();
		return false;
    }
}

class SJFComparer : IComparer<QueueEntry>
{
	public int Compare(QueueEntry x, QueueEntry y)
	{
		//a > b ==> 1
        //a < b ==> -1
        //a == b ==> 0

		if (x.expectedDuration > y.expectedDuration)
		{
			return 1;
		}

		if (x.expectedDuration < y.expectedDuration)
		{
			return -1;
		}

		return 0;
	}
}