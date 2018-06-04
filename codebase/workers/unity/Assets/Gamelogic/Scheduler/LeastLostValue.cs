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

public class LeastLostValueScheduler : MonoBehaviour, Scheduler
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

		requestQueue = new SortedSet<QueueEntry>(new LLVComparer());

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

		if (requestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            handle.Respond(new DeliveryResponse(false));
        }
        else
        {
			float estimatedTime = Vector3.Distance(gameObject.transform.position, handle.Request.destination.ToUnityVector()) / SimulationSettings.MaxDroneSpeed;
			QueueEntry queueEntry = new QueueEntry(Time.time, handle.Request, 0, estimatedTime);
			requestQueue.Add(queueEntry);
            handle.Respond(new DeliveryResponse(true));
        }
    }

	private float ExpectedValue(QueueEntry queueEntry)
	{
		return ExpectedValue(queueEntry.expectedDuration, queueEntry.request.packageInfo);
	}
    
	private float ExpectedValue(float estimatedTime, PackageInfo packageInfo)
	{
		return (float)PayloadGenerator.DeliveryValue(estimatedTime, packageInfo);
	}
    
	void Scheduler.UpdateDeliveryRequestQueue()
    {
		//DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<QueueEntry>(deliveryRequestQueue.ToArray())));
    }

	int Scheduler.GetQueueSize()
	{
		return requestQueue.Count;
	}

	int Scheduler.GetTotalRequests()
	{
		return incomingRequests;
	}

	private void SortQueue()
	{
		QueueEntry[] entries = new QueueEntry[requestQueue.Count];
		int i = 0;
		foreach(QueueEntry entry in requestQueue)
		{
			entries[i++] = entry;
		}

		for (int j = 0; j < entries.Length; j++)
		{
			float lostValue = 0;
			float maxDuration = float.MinValue;
			float entryDuration = entries[j].expectedDuration;

			for (int k = 0; k < entries.Length; k++)
			{
				if (j != k) {
					lostValue += ExpectedValue(entries[k].expectedDuration, entries[k].request.packageInfo)
						       - ExpectedValue(entries[k].expectedDuration + entryDuration, entries[j].request.packageInfo);

					maxDuration = Mathf.Max(maxDuration, entries[k].expectedDuration);
				}
			}

			float wonValue = ExpectedValue(entries[j].expectedDuration, entries[j].request.packageInfo)
			               - ExpectedValue(entries[j].expectedDuration + maxDuration, entries[j].request.packageInfo);

			entries[j].priority = lostValue - wonValue;
		}

		requestQueue.Clear();
		foreach(QueueEntry entry in entries)
		{
			requestQueue.Add(entry);
		}
	}

	bool Scheduler.GetNextRequest(out QueueEntry queueEntry)
    {
		if (requestQueue.Count > 0)
		{
			SortQueue();

			queueEntry = requestQueue.Min;
			requestQueue.Remove(queueEntry);
			return true;
		}

		queueEntry = new QueueEntry();
		return false;
    }
}

class LLVComparer : IComparer<QueueEntry>
{
	public int Compare(QueueEntry x, QueueEntry y)
	{
		//a > b ==> 1
        //a < b ==> -1
        //a == b ==> 0

		if (x.priority > y.priority)
		{
			return 1;
		}

		if (x.priority < y.priority)
		{
			return -1;
		}

		return 0;
	}
}