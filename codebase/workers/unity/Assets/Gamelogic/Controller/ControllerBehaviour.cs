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

[WorkerType(WorkerPlatform.UnityWorker)]
public class ControllerBehaviour : MonoBehaviour
{
    [Require]
    private Position.Writer PositionWriter;

    [Require]
    private Controller.Writer ControllerWriter;

    [Require]
    private ControllerMetrics.Writer MetricsWriter;
    
	GridGlobalLayer globalLayer;

	Scheduler scheduler;

	Improbable.Collections.Map<EntityId, DeliveryInfo> deliveriesMap;
	Improbable.Collections.List<DroneInfo> droneSlots;

    Coordinates departuresPoint;
    Coordinates arrivalsPoint;

    bool stopSpawning = false;

    int completedDeliveries;
	int completedRoundTrips;
    int collisionsReported;

	int failedLaunches;
	int failedDeliveries;
	int failedReturns;
	int unknownRequests;

	int usedSlots;

	int revenue;
	float costs;
	float penalties;

	float avgWaitTime;
	int launches;
	float avgDeliveryTime;

    private void OnEnable()
    {
		deliveriesMap = ControllerWriter.Data.deliveriesMap;
		droneSlots = ControllerWriter.Data.droneSlots;

        completedDeliveries = MetricsWriter.Data.completedDeliveries;
		completedRoundTrips = MetricsWriter.Data.completedRoundTrips;
        collisionsReported = MetricsWriter.Data.collisionsReported;

		failedLaunches = MetricsWriter.Data.failedLaunches;
		failedDeliveries = MetricsWriter.Data.failedDeliveries;
		failedReturns = MetricsWriter.Data.failedReturns;
		unknownRequests = MetricsWriter.Data.unknownRequests;

		revenue = MetricsWriter.Data.revenue;
		costs = MetricsWriter.Data.costs;
		penalties = MetricsWriter.Data.penalties;

		avgWaitTime = MetricsWriter.Data.avgWaitTime;
		launches = MetricsWriter.Data.launches;
		avgDeliveryTime = MetricsWriter.Data.avgDeliveryTime;

		for (int i = 0; i < droneSlots.Count; i++)
		{
			if (droneSlots[i].occupied)
			{
				usedSlots++;
			}
		}

        departuresPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerDepartureOffset;
        arrivalsPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerArrivalOffset;
        
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(HandleTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);
        ControllerWriter.CommandReceiver.OnUnlinkDrone.RegisterAsyncResponse(HandleUnlinkRequest);
        
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();
		//scheduler = gameObject.GetComponent<FirstComeFirstServeScheduler>();
		scheduler = gameObject.GetComponent<LeastLostValueScheduler>();

        UnityEngine.Random.InitState((int)gameObject.EntityId().Id);
        InvokeRepeating("ControllerTick", UnityEngine.Random.Range(0, SimulationSettings.RequestHandlerInterval), SimulationSettings.RequestHandlerInterval);
        InvokeRepeating("PrintMetrics", 0, SimulationSettings.ControllerMetricsInterval);
		InvokeRepeating("DroneMapPrune", UnityEngine.Random.Range(0, SimulationSettings.DroneMapPruningInterval), SimulationSettings.DroneMapPruningInterval);
    }

    private void OnDisable()
    {
		CancelInvoke();

		deliveriesMap.Clear();
		droneSlots.Clear();

        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnCollision.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnUnlinkDrone.DeregisterResponse();
    }

    void HandleUnlinkRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.UnlinkDrone, UnlinkRequest, UnlinkResponse> handle)
    {
		DeliveryInfo droneInfo;
		if (deliveriesMap.TryGetValue(handle.Request.droneId, out droneInfo))
		{
			DestroyDrone(handle.Request.droneId, droneInfo.slot);
			DroneRetrieval(handle.Request.location.ToUnityVector());

			if (droneInfo.returning)
            {
                MetricsWriter.Send(new ControllerMetrics.Update()
				                   .SetFailedReturns(++failedReturns)
				                   .SetCosts(costs)
				                   .SetPenalties(penalties));
            }
            else
            {
				float penalty = PayloadGenerator.GetPackageCost(droneInfo.packageInfo) + SimulationSettings.FailedDeliveryPenalty;
				penalties += penalty / 100;
                MetricsWriter.Send(new ControllerMetrics.Update()
				                   .SetFailedDeliveries(++failedDeliveries)
				                   .SetCosts(costs)
                                   .SetPenalties(penalties));
            }

			UpdateDroneSlotsAndMap();
		}
		else
		{
			MetricsWriter.Send(new ControllerMetrics.Update().SetUnknownRequests(++unknownRequests));
		}

        handle.Respond(new UnlinkResponse());
    }

	void RegisterCompletedDelivery(DeliveryInfo deliveryInfo)
	{
		float deliveryTime = Time.time - deliveryInfo.timestamp;
		if (deliveryTime < 0)
		{
			Debug.LogError("DELIVERY TIME < 0 - BIG ERROR");
			return;
		}

		++completedDeliveries;
		avgDeliveryTime += deliveryTime;

		revenue += PayloadGenerator.DeliveryValue(deliveryTime, deliveryInfo.packageInfo);
	}

    void HandleTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
		DeliveryInfo deliveryInfo;
		if (deliveriesMap.TryGetValue(handle.Request.droneId, out deliveryInfo))
        {
            //Debug.LogWarning("is final waypoint?");
            //final waypoint, figure out if it's back at controller or only just delivered
			if (deliveryInfo.nextWaypoint < deliveryInfo.waypoints.Count)
            {
				handle.Respond(new TargetResponse(deliveryInfo.waypoints[deliveryInfo.nextWaypoint], TargetResponseCode.SUCCESS));
				IncrementNextWaypoint(handle.Request.droneId, handle.Request.energyUsed);
            }
            else
            {
				if (deliveryInfo.returning)
                {
                    UnsuccessfulTargetRequest(handle, TargetResponseCode.JOURNEY_COMPLETE);
					DestroyDrone(handle.Request.droneId, deliveryInfo.slot);
					MetricsWriter.Send(new ControllerMetrics.Update()
					                   .SetCompletedRoundTrips(++completedRoundTrips)
					                   .SetCosts(costs));
					UpdateDroneSlotsAndMap();
                }
                else
                {
					RegisterCompletedDelivery(deliveryInfo);
					MetricsWriter.Send(new ControllerMetrics.Update()
					                   .SetCompletedDeliveries(completedDeliveries)
					                   .SetAvgDeliveryTime(avgDeliveryTime)
					                   .SetRevenue(revenue));

					deliveryInfo.returning = true;
					deliveryInfo.waypoints.Reverse();
					Vector3f arrivalsGridPoint = globalLayer.GetClosestVector3fOnGrid(deliveryInfo.waypoints[0]);
					arrivalsGridPoint.y = deliveryInfo.waypoints[2].y;
					deliveryInfo.waypoints[1] = arrivalsGridPoint;
					deliveryInfo.nextWaypoint = 2;
					deliveryInfo.latestCheckinTime
                        = Time.time
                        + (SimulationSettings.DroneETAConstant
                           * Vector3.Distance(
							   deliveryInfo.waypoints[0].ToUnityVector(),
							   deliveryInfo.waypoints[1].ToUnityVector())
                           / SimulationSettings.MaxDroneSpeed);

					deliveriesMap.Remove(handle.Request.droneId);
					deliveriesMap.Add(handle.Request.droneId, deliveryInfo);

					DroneInfo droneInfo = droneSlots[deliveryInfo.slot];
					droneInfo.energyUsed = handle.Request.energyUsed;
					droneSlots[deliveryInfo.slot] = droneInfo;

					UpdateDroneSlotsAndMap();

                    //ignore 0 because that's the point that we've just reached
                    //saved as 2, but sending 1 back to drone - only 1 spatial update instead of 2 now
					handle.Respond(new TargetResponse(deliveryInfo.waypoints[1], TargetResponseCode.SUCCESS));
                }
            }
        }
        else
        {
            UnsuccessfulTargetRequest(handle, TargetResponseCode.WRONG_CONTROLLER);
        }
    }

    void UnsuccessfulTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle, TargetResponseCode responseCode)
    {
        handle.Respond(new TargetResponse(new Vector3f(), responseCode));
    }

    void PrintMetrics()
    {
		Debug.LogWarningFormat("METRICS {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}"
                               , gameObject.EntityId().Id
		                       , deliveriesMap.Count
		                       , scheduler.GetQueueSize()
                               , completedDeliveries
		                       , completedRoundTrips
		                       , revenue
                               , costs
		                       , penalties
		                       , avgDeliveryTime / completedDeliveries
		                       , avgWaitTime / launches
                               , failedDeliveries
		                       , failedReturns
		                       , failedLaunches
                               , collisionsReported
		                       , unknownRequests
		                       , scheduler.GetTotalRequests());
    }

    void HandleCollision(Improbable.Entity.Component.ResponseHandle<Controller.Commands.Collision, CollisionRequest, CollisionResponse> handle)
    {
        handle.Respond(new CollisionResponse());

		penalties += 2 * SimulationSettings.DroneReplacementCost;

        DestroyDrone(handle.Request.droneId);
        DestroyDrone(handle.Request.colliderId);

		MetricsWriter.Send(new ControllerMetrics.Update()
                           .SetCollisionsReported(++collisionsReported)
                           .SetPenalties(penalties)
		                   .SetCosts(costs));

		UpdateDroneSlotsAndMap();
    }

	void DestroyDrone(EntityId entityId)
	{
		DeliveryInfo deliveryInfo;
		if (deliveriesMap.TryGetValue(entityId, out deliveryInfo))
		{
			DestroyDrone(entityId, deliveryInfo.slot);
		}
		else
		{
			//if drone isn't in dronemap - just delete it anyway and correct controller will prune it out
			SpatialOS.Commands.DeleteEntity(PositionWriter, entityId);
		}
	}

    void AddEnergyCost(float energyUsed)
	{
		costs += energyUsed * SimulationSettings.CostPerWh;
	}

    void ReturnDroneSlot(int slot)
	{
		DroneInfo droneInfo = droneSlots[slot];
        droneInfo.occupied = false;
        droneInfo.deliveryId = new EntityId(-1);
		AddEnergyCost(droneInfo.energyUsed);
		droneInfo.energyUsed = 0;
        droneSlots[slot] = droneInfo;
        usedSlots--;
	}

    void DestroyDrone(EntityId entityId, int slot)
    {
		deliveriesMap.Remove(entityId);

		ReturnDroneSlot(slot);

		SpatialOS.Commands.DeleteEntity(PositionWriter, entityId);
    }

    void UpdateDroneSlots()
	{
		ControllerWriter.Send(new Controller.Update().SetDroneSlots(droneSlots));
	}

	//void UpdateDeliveriesMap()
  //  {
		//ControllerWriter.Send(new Controller.Update().SetDeliveriesMap(deliveriesMap));
    //}

	void UpdateDroneSlotsAndMap()
	{
		ControllerWriter.Send(new Controller.Update()
		                      .SetDeliveriesMap(deliveriesMap)
		                      .SetDroneSlots(droneSlots));
	}

    private void IncrementNextWaypoint(EntityId droneId, float batteryLevel)
    {
		DeliveryInfo deliveryInfo;
		if(deliveriesMap.TryGetValue(droneId, out deliveryInfo))
        {
			IncrementNextWaypoint(droneId, deliveryInfo, batteryLevel);
        }
    }

	private void IncrementNextWaypoint(EntityId droneId, DeliveryInfo deliveryInfo, float energyUsed)
    {
		deliveryInfo.nextWaypoint++;
		deliveryInfo.latestCheckinTime
            = Time.time
            + (SimulationSettings.DroneETAConstant
               * Vector3.Distance(
				   deliveryInfo.waypoints[deliveryInfo.nextWaypoint - 1].ToUnityVector(),
				   deliveryInfo.waypoints[deliveryInfo.nextWaypoint - 2].ToUnityVector())
               / SimulationSettings.MaxDroneSpeed);
		deliveriesMap.Remove(droneId);
		deliveriesMap.Add(droneId, deliveryInfo);

		DroneInfo droneInfo = droneSlots[deliveryInfo.slot];
		droneInfo.energyUsed = energyUsed;
        droneSlots[deliveryInfo.slot] = droneInfo;

		UpdateDroneSlotsAndMap();
    }

	void DroneDeploymentSuccess(EntityId droneId, DeliveryInfo deliveryInfo)
    {
		deliveryInfo.nextWaypoint++;
		deliveriesMap.Add(droneId, deliveryInfo);

		DroneInfo droneInfo = droneSlots[deliveryInfo.slot];
		droneInfo.deliveryId = droneId;
		droneInfo.occupied = true;
		droneSlots[deliveryInfo.slot] = droneInfo;

		avgWaitTime += Time.time - deliveryInfo.timestamp;
		++launches;

		MetricsWriter.Send(new ControllerMetrics.Update()
		                   .SetAvgWaitTime(avgWaitTime)
		                   .SetLaunches(launches));

		UpdateDroneSlotsAndMap();
    }

    void DroneDeploymentFailure()
    {
		usedSlots--;
		MetricsWriter.Send(new ControllerMetrics.Update().SetFailedLaunches(++failedLaunches));
    }

    public int AvailableSlots()
	{
		return droneSlots.Count - usedSlots;
	}

    private int GetNextSlot()
	{
		for (int i = 0; i < droneSlots.Count; i++)
		{
			if (!droneSlots[i].occupied)
			{
				usedSlots++;
				return i;
			}
		}

		return -1;
	}

	void HandleDeliveryRequest(QueueEntry entry)
    {
		DeliveryRequest request = entry.request;

		DeliveryInfo deliveryInfo;
		Vector2 random;

		deliveryInfo.timestamp = entry.timestamp;
		deliveryInfo.packageInfo = request.packageInfo;

		deliveryInfo.slot = GetNextSlot();
		if (deliveryInfo.slot < 0)
		{
			Debug.LogError("Something's gone terribly wrong with the slot mechanics.");
		}

		random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
		Vector3f departurePoint = departuresPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);

		random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
		Vector3f arrivalPoint = arrivalsPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);

        //Debug.LogWarning("point to point plan");
        //for new flight plan
		deliveryInfo.nextWaypoint = 1;
		deliveryInfo.returning = false;
		Improbable.Collections.List<Improbable.Vector3f> plan = globalLayer.generatePointToPointPlan(
			departurePoint,
            request.destination);
		
        //Debug.LogWarning("null check");
		if (plan == null || plan.Count < 2)
        {
            // let scheduler know that this job can't be done
			DroneDeploymentFailure();
            return;
        }

		deliveryInfo.waypoints = plan;

		//0th index only useful as last point in return journey
		//so make sure 0th index == last location in journey == arrivalsPoint
		deliveryInfo.waypoints[0] = arrivalPoint;
		deliveryInfo.latestCheckinTime
            = Time.time
            + (SimulationSettings.DroneETAConstant
               * Vector3.Distance(
                   departurePoint.ToUnityVector(),
				   deliveryInfo.waypoints[1].ToUnityVector())
               / SimulationSettings.MaxDroneSpeed);

        //create drone
        //if successful, add to droneMap
        //if failure, tell scheduler job couldn't be done
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(
			departurePoint.ToCoordinates(),
			deliveryInfo.waypoints[deliveryInfo.nextWaypoint],
            gameObject.EntityId(),
			request.packageInfo.weight + PayloadGenerator.GetPackagingWeight(request.packageInfo.type),
            SimulationSettings.MaxDroneSpeed);
        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
		         .OnSuccess((response) => DroneDeploymentSuccess(response.CreatedEntityId, deliveryInfo))
		         .OnFailure((response) => DroneDeploymentFailure());
    }

    private bool ReadyForDeployment()
	{
		return usedSlots < ControllerWriter.Data.maxDroneCount;
	}

    void ControllerTick()
    {
        if (!ControllerWriter.Data.initialised)
        {
            //Debug.LogWarning("call init global layer");
            globalLayer.InitGlobalLayer(ControllerWriter.Data.topLeft, ControllerWriter.Data.bottomRight);
            //Debug.LogWarning("Global Layer Ready");
            ControllerWriter.Send(new Controller.Update().SetInitialised(true));
            return;
        }

		//don't need to do anything if no requests in the queue
		QueueEntry nextRequest;
		if (ReadyForDeployment())
        {
			if (scheduler.GetNextRequest(out nextRequest))
			{
				HandleDeliveryRequest(nextRequest);
			}
        }

		scheduler.UpdateDeliveryRequestQueue();
    }

	void DroneMapPrune()
	{
		if (!ControllerWriter.Data.initialised)
		{
			return;
		}

		DeliveryInfo deliveryInfo;
		List<EntityId> toPrune = new List<EntityId>();

		foreach(EntityId droneId in deliveriesMap.Keys)
		{
			if (deliveriesMap.TryGetValue(droneId, out deliveryInfo))
			{
				if (Time.time > deliveryInfo.latestCheckinTime)
                {
					Debug.LogErrorFormat("Pruning Drone for taking too long. Drone {0} Delivered: {1}", droneId, deliveryInfo.returning);
					toPrune.Add(droneId);

					if (deliveryInfo.returning)
                    {
						++failedReturns;
                    }
                    else
                    {
						++failedDeliveries;
						float penalty = PayloadGenerator.GetPackageCost(deliveryInfo.packageInfo) + SimulationSettings.FailedDeliveryPenalty;
                        penalties += penalty / 100;
                    }
                }
			}
		}

		foreach(EntityId droneId in toPrune)
		{
			Improbable.Worker.IComponentData<Position> positionData = SpatialOS.GetLocalEntityComponent<Position>(droneId);
			if (positionData != null)
			{
				Coordinates coords = positionData.Get().Value.coords;
				coords.y = 0;
				DroneRetrieval(coords.ToUnityVector());
			}
			DestroyDrone(droneId);
		}

		MetricsWriter.Send(new ControllerMetrics.Update()
		                   .SetFailedReturns(failedReturns)
		                   .SetFailedDeliveries(failedDeliveries)
		                   .SetPenalties(penalties)
		                   .SetCosts(costs));
        
		UpdateDroneSlotsAndMap();
	}

	private void DroneRetrieval(Vector3 dronePosition)
	{
		//constant converts metres to miles, and applies fuel costs and truck mileage to produce a penalty
        //penalty reflects rough cost of sending a van out to collect a fallen drone
		penalties += Vector3.Distance(dronePosition, gameObject.transform.position) * SimulationSettings.TruckCostConstant;
	}
}
