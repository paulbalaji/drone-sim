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

    DroneTranstructor droneTranstructor;

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

    private void OnEnable()
    {
		deliveriesMap = ControllerWriter.Data.deliveriesMap;
		droneSlots = ControllerWriter.Data.droneSlots;

        completedDeliveries = MetricsWriter.Data.completedDeliveries;
		completedRoundTrips = MetricsWriter.Data.completedRoundTrips;
        collisionsReported = MetricsWriter.Data.collisionsReported;
		failedDeliveries = MetricsWriter.Data.failedDeliveries;
		failedReturns = MetricsWriter.Data.failedReturns;
		unknownRequests = MetricsWriter.Data.unknownRequests;

        departuresPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerDepartureOffset;
        arrivalsPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerArrivalOffset;
        
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(HandleTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);
        ControllerWriter.CommandReceiver.OnUnlinkDrone.RegisterAsyncResponse(HandleUnlinkRequest);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();
		scheduler = gameObject.GetComponent<FirstComeFirstServeScheduler>();

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
			if (droneInfo.returning)
            {
                MetricsWriter.Send(new ControllerMetrics.Update().SetFailedReturns(++failedReturns));
            }
            else
            {
                MetricsWriter.Send(new ControllerMetrics.Update().SetFailedDeliveries(++failedDeliveries));
            }

			DestroyDrone(handle.Request.droneId);
			UpdateDeliveriesMap();
		}
		else
		{
			MetricsWriter.Send(new ControllerMetrics.Update().SetUnknownRequests(++unknownRequests));
		}

        handle.Respond(new UnlinkResponse());
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
                IncrementNextWaypoint(handle.Request.droneId);
            }
            else
            {
				if (deliveryInfo.returning)
                {
                    UnsuccessfulTargetRequest(handle, TargetResponseCode.JOURNEY_COMPLETE);
					MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedRoundTrips(++completedRoundTrips));
                    DestroyDrone(handle.Request.droneId);
                    UpdateDeliveriesMap();
                }
                else
                {
					MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedDeliveries(++completedDeliveries));

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
                    UpdateDeliveriesMap();

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
		Debug.LogWarningFormat("METRICS C_{0} drones {1} queue {2} deliveries {3} fullTrips {4} fDel {5} fRet {6} fLaunch {7} collisions {8} unknown {9} total {10}"
                               , gameObject.EntityId().Id
		                       , deliveriesMap.Count
		                       , scheduler.GetQueueSize()
                               , completedDeliveries
		                       , completedRoundTrips
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

		MetricsWriter.Send(new ControllerMetrics.Update().SetCollisionsReported(++collisionsReported));

        DestroyDrone(handle.Request.droneId);
        DestroyDrone(handle.Request.colliderId);
        UpdateDeliveriesMap();
    }

    void DestroyDrone(EntityId entityId)
    {
		deliveriesMap.Remove(entityId);
        droneTranstructor.DestroyDrone(entityId);
    }

    void UpdateDroneSlots()
	{
		ControllerWriter.Send(new Controller.Update().SetDroneSlots(droneSlots));
	}

	void UpdateDeliveriesMap()
    {
		ControllerWriter.Send(new Controller.Update().SetDeliveriesMap(deliveriesMap));
    }

    private void IncrementNextWaypoint(EntityId droneId)
    {
		DeliveryInfo deliveryInfo;
		if(deliveriesMap.TryGetValue(droneId, out deliveryInfo))
        {
			IncrementNextWaypoint(droneId, deliveryInfo);
        }
    }

	private void IncrementNextWaypoint(EntityId droneId, DeliveryInfo deliveryInfo)
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
        UpdateDeliveriesMap();
    }

	void DroneDeploymentSuccess(EntityId droneId, DeliveryInfo deliveryInfo)
    {
		deliveryInfo.nextWaypoint++;
		deliveriesMap.Add(droneId, deliveryInfo);
        UpdateDeliveriesMap();
    }

    void DroneDeploymentFailure()
    {
		MetricsWriter.Send(new ControllerMetrics.Update().SetFailedLaunches(++failedLaunches));
    }

    void HandleDeliveryRequest(DeliveryRequest request)
    {
		DeliveryInfo deliveryInfo;
		Vector2 random;

		deliveryInfo.slot = -1;

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
            SimulationSettings.MaxDroneSpeed);
        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
		         .OnSuccess((response) => DroneDeploymentSuccess(response.CreatedEntityId, deliveryInfo))
                 .OnFailure((response) => DroneDeploymentFailure());
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
		DeliveryRequest nextRequest;
		if (deliveriesMap.Count < ControllerWriter.Data.maxDroneCount)
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
                    }
                }
			}
		}

		foreach(EntityId droneId in toPrune)
		{
			DestroyDrone(droneId);
		}

		MetricsWriter.Send(new ControllerMetrics.Update()
		                   .SetFailedReturns(failedReturns)
		                   .SetFailedDeliveries(failedDeliveries));
        
		UpdateDeliveriesMap();
	}
}
