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

	float potential;
    int rejections;
	float rejecValue;

	bool sorted;

    // Use this for initialization
    private void OnEnable()
    {
	    string schType;
	    if (!SpatialOS.Connection.GetWorkerFlag("drone_sim_scheduler_type").TryGetValue(out schType)
	        || !schType.Equals("LLV"))
	    {
		    this.enabled = false;
		    return;
	    }
	    
        incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
        potential = DeliveryHandlerWriter.Data.potential;
        rejections = DeliveryHandlerWriter.Data.rejections;
		rejecValue = DeliveryHandlerWriter.Data.rejectedValue;

		requestQueue = new SortedSet<QueueEntry>(new LLVComparer());

		foreach (QueueEntry entry in DeliveryHandlerWriter.Data.requestQueue)
        {
			requestQueue.Add(entry);
        }

		sorted = true;

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

		requestQueue.Add(queueEntry);
		sorted = false;
        handle.Respond(new DeliveryResponse(true));
    }
    
	void Scheduler.UpdateDeliveryRequestQueue()
    {
        if (!sorted)
		{
			SortAndPrune();
		}

		Improbable.Collections.List<QueueEntry> queueList = new Improbable.Collections.List<QueueEntry>();
		foreach (QueueEntry entry in requestQueue)
        {
			queueList.Add(entry);
        }

		DeliveryHandlerWriter.Send(new DeliveryHandler.Update()
		                           .SetRequestQueue(queueList)
		                           .SetPotential(potential)
		                           .SetRejections(rejections)
		                           .SetRejectedValue(rejecValue));
    }

	int Scheduler.GetQueueSize()
	{
		return requestQueue.Count;
	}

	float Scheduler.GetPenalties()
	{
		return SimulationSettings.FailedDeliveryPenalty * rejections;
	}

	float Scheduler.GetRejectedValue()
    {
        return rejecValue;
    }

    float Scheduler.GetAvgRejectedValue()
    {
        return rejecValue / rejections;
    }

	float Scheduler.GetPotentialLost()
    {
        return potential;
    }

    float Scheduler.GetAvgPotentialLost()
    {
        return potential / rejections;
    }

	private void SortQueue()
	{
		QueueEntry[] entries = new QueueEntry[requestQueue.Count];
		int i = 0;
		foreach(QueueEntry entry in requestQueue)
		{
			entries[i++] = entry.DeepCopy();
		}

		for (int j = 0; j < entries.Length; j++)
		{
			float lostValue = 0;
			float maxDuration = float.MinValue;
			float entryDuration = entries[j].expectedDuration;
			float timePassed;

			for (int k = 0; k < entries.Length; k++)
			{
				if (j != k) {
					//timePassed = wait time so far + estimated time til the delivery
					timePassed = Time.time - entries[k].timestamp + entries[k].expectedDuration;
					lostValue += TimeValueFunctions.ExpectedProfit(timePassed, entries[k].expectedDuration, entries[k].request.packageInfo, entries[k].request.timeValueFunction)
						       - TimeValueFunctions.ExpectedProfit(timePassed + entryDuration, entries[k].expectedDuration, entries[j].request.packageInfo, entries[j].request.timeValueFunction);

					maxDuration = Mathf.Max(maxDuration, entries[k].expectedDuration);
				}
			}

			//timePassed = wait time so far + estimated time til the delivery
			timePassed = Time.time - entries[j].timestamp + entries[j].expectedDuration;
			float wonValue = TimeValueFunctions.ExpectedProfit(timePassed, entries[j].expectedDuration, entries[j].request.packageInfo, entries[j].request.timeValueFunction)
				           - TimeValueFunctions.ExpectedProfit(timePassed + maxDuration, entries[j].expectedDuration, entries[j].request.packageInfo, entries[j].request.timeValueFunction);

			entries[j].priority = lostValue - wonValue;
		}

		requestQueue.Clear();
		foreach(QueueEntry entry in entries)
		{
			requestQueue.Add(entry);
		}

		sorted = true;
	}

	private void PruneQueue()
	{
		int toRemove = requestQueue.Count - ((int)SimulationSettings.MaxDeliveryRequestQueueSize);
		for (int i = 0; i < toRemove; i++)
		{
			QueueEntry maxEntry = requestQueue.Max;
            float duration = Time.time - maxEntry.timestamp + maxEntry.expectedDuration;
			float value = TimeValueFunctions.ExpectedProfit(duration, maxEntry.expectedDuration, maxEntry.request.packageInfo, maxEntry.request.timeValueFunction);
			float maxValue = TimeValueFunctions.ExpectedProfit(maxEntry.expectedDuration, maxEntry.expectedDuration, maxEntry.request.packageInfo, maxEntry.request.timeValueFunction);
            requestQueue.Remove(maxEntry);

            potential += value;
			rejecValue += maxValue;
            ++rejections;
		}
	}

	private void SortAndPrune()
	{
		SortQueue();
		PruneQueue();
	}

	bool Scheduler.GetNextRequest(out QueueEntry queueEntry)
    {
		if (requestQueue.Count > 0)
		{
			SortAndPrune();

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
        //two entries are the same if they have the same id
		if (x.request.id == y.request.id)
		{
			return 0;
		}

		if (x.priority > y.priority)
		{
			return 1;
		}

		return -1;
	}
}