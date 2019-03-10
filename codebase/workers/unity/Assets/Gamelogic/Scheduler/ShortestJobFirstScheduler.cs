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

public class ShortestJobFirstScheduler : MonoBehaviour, Scheduler
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
	
	private GridGlobalLayer _globalLayer;
	private ControllerBehaviour _controller;

    // Use this for initialization
    private void OnEnable()
    {
	    string schType;
	    if (!SpatialOS.Connection.GetWorkerFlag("drone_sim_scheduler_type").TryGetValue(out schType)
	        || !schType.Equals("SJF"))
	    {
		    this.enabled = false;
		    return;
	    }
	    
	    _globalLayer = GetComponent<GridGlobalLayer>();
	    _controller = GetComponent<ControllerBehaviour>();
	    
        incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
        potential = DeliveryHandlerWriter.Data.potential;
        rejections = DeliveryHandlerWriter.Data.rejections;
		rejecValue = DeliveryHandlerWriter.Data.rejectedValue;

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
	    
	    var random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
	    Vector3f departurePoint = _controller.DeparturesPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);
	    var plan = _globalLayer.generatePointToPointPlan(departurePoint, handle.Request.destination);
	    if (plan == null || plan.Count < 2)
	    {
		    handle.Respond(new DeliveryResponse(false));
		    return;
	    }

		float estimatedTime = _globalLayer.EstimatedPlanTime(plan);
		QueueEntry queueEntry = new QueueEntry(Time.time, handle.Request, 0, estimatedTime);

		if (requestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
			QueueEntry longestJob = requestQueue.Max;
			float duration, value, maxValue;
			if (estimatedTime > longestJob.expectedDuration)
			{
				handle.Respond(new DeliveryResponse(false));
                
				value = TimeValueFunctions.ExpectedProfit(estimatedTime, estimatedTime, queueEntry.request.packageInfo, queueEntry.request.timeValueFunction);
				potential += value;
				rejecValue += value;
                ++rejections;
				return;
			}

			requestQueue.Remove(longestJob);
			duration = Time.time - longestJob.timestamp + longestJob.expectedDuration;
			value = TimeValueFunctions.ExpectedProfit(duration, longestJob.expectedDuration, longestJob.request.packageInfo, longestJob.request.timeValueFunction);
			maxValue = TimeValueFunctions.ExpectedProfit(longestJob.expectedDuration, longestJob.expectedDuration, longestJob.request.packageInfo, longestJob.request.timeValueFunction);
			potential += value;
			rejecValue += maxValue;
            ++rejections;
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
        //two entries are the same if they have the same id
        if (x.request.id == y.request.id)
        {
            return 0;
        }

		if (x.expectedDuration > y.expectedDuration)
		{
			return 1;
		}

		return -1;
	}
}