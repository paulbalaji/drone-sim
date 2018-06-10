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

public interface Scheduler
{
	int GetQueueSize();
	float GetPotentialLost();
	float GetAvgPotentialLost();
	bool GetNextRequest(out QueueEntry deliveryRequest);
	void UpdateDeliveryRequestQueue();
	void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle);
}
