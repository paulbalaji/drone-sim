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
	float rejecValue;
	
	private GridGlobalLayer _globalLayer;
	private ControllerBehaviour _controller;

	// Use this for initialization
	private void OnEnable()
	{
		string schType;
		if (!SpatialOS.Connection.GetWorkerFlag("drone_sim_scheduler_type").TryGetValue(out schType)
		    || !schType.Equals("FCFS"))
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
	    
	    var random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
	    Vector3f departurePoint = _controller.DeparturesPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);
	    var plan = _globalLayer.generatePointToPointPlan(departurePoint, handle.Request.destination);
	    if (plan == null || plan.Count < 2)
	    {
		    handle.Respond(new DeliveryResponse(false));
		    return;
	    }

	    float expectedDuration = _globalLayer.EstimatedPlanTime(plan);
        if (deliveryRequestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            handle.Respond(new DeliveryResponse(false));
			float value = TimeValueFunctions.DeliveryValue(expectedDuration, handle.Request.packageInfo, handle.Request.timeValueFunction);
            potential += value;
			rejecValue += value;
            ++rejections;
        }
        else
        {
			deliveryRequestQueue.Enqueue(new QueueEntry(Time.time, handle.Request, 0, expectedDuration));
            handle.Respond(new DeliveryResponse(true));
        }
    }

	void Scheduler.UpdateDeliveryRequestQueue()
    {
		DeliveryHandlerWriter.Send(new DeliveryHandler.Update()
		                           .SetRequestQueue(new Improbable.Collections.List<QueueEntry>(deliveryRequestQueue.ToArray()))
		                           .SetPotential(potential)
		                           .SetRejections(rejections)
		                           .SetRejectedValue(rejecValue));
    }

	int Scheduler.GetQueueSize()
	{
		return deliveryRequestQueue.Count;
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
		if (deliveryRequestQueue.Count > 0)
		{
			queueEntry = deliveryRequestQueue.Dequeue();
			return true;
		}

		queueEntry = new QueueEntry();
		return false;
    }
}
