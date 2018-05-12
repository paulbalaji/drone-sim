using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Scheduler;
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

    [Require]
    private DeliveryHandler.Writer DeliveryHandlerWriter;

    DroneTranstructor droneTranstructor;

    GridGlobalLayer globalLayer;

    Improbable.Collections.Map<EntityId, DroneInfo> droneMap;
    
    Queue<DeliveryRequest> deliveryRequestQueue;

    Coordinates departuresPoint;
    Coordinates arrivalsPoint;

    bool stopSpawning = false;
	int incomingRequests;
    int completedDeliveries;
	int completedRoundTrips;
    int collisionsReported;
	int failedDeliveries;

    private void OnEnable()
    {
        droneMap = ControllerWriter.Data.droneMap;

		incomingRequests = MetricsWriter.Data.incomingDeliveryRequests;
        completedDeliveries = MetricsWriter.Data.completedDeliveries;
		completedRoundTrips = MetricsWriter.Data.completedRoundTrips;
        collisionsReported = MetricsWriter.Data.collisionsReported;
		failedDeliveries = MetricsWriter.Data.failedDeliveries;

        departuresPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerDepartureOffset;
        arrivalsPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerArrivalOffset;

        deliveryRequestQueue = new Queue<DeliveryRequest>(DeliveryHandlerWriter.Data.requestQueue);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(HandleTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);
        ControllerWriter.CommandReceiver.OnUnlinkDrone.RegisterAsyncResponse(HandleUnlinkRequest);

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();

        UnityEngine.Random.InitState((int)gameObject.EntityId().Id);
        InvokeRepeating("ControllerTick", UnityEngine.Random.Range(0, SimulationSettings.RequestHandlerInterval), SimulationSettings.RequestHandlerInterval);
        InvokeRepeating("PrintMetrics", 0, SimulationSettings.ControllerMetricsInterval);
		InvokeRepeating("DroneMapPrune", UnityEngine.Random.Range(0, SimulationSettings.DroneMapPruningInterval), SimulationSettings.DroneMapPruningInterval);
    }

    private void OnDisable()
    {
		CancelInvoke();

        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnCollision.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnUnlinkDrone.DeregisterResponse();

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
    }

    void HandleUnlinkRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.UnlinkDrone, UnlinkRequest, UnlinkResponse> handle)
    {
        DroneInfo droneInfo;
        if (droneMap.TryGetValue(handle.Request.droneId, out droneInfo))
        {
			DestroyDrone(handle.Request.droneId);
            UpdateDroneMap();
        }

        handle.Respond(new UnlinkResponse());
        DroneDeploymentFailure();
    }

    void HandleTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        DroneInfo droneInfo;
        if (droneMap.TryGetValue(handle.Request.droneId, out droneInfo))
        {
            //TODO: need to verify if the drone is actually at its target

            //Debug.LogWarning("is final waypoint?");
            //final waypoint, figure out if it's back at controller or only just delivered
            if (droneInfo.nextWaypoint < droneInfo.waypoints.Count)
            {
                handle.Respond(new TargetResponse(droneInfo.waypoints[droneInfo.nextWaypoint], TargetResponseCode.SUCCESS));
                IncrementNextWaypoint(handle.Request.droneId);
            }
            else
            {
                if (droneInfo.returning)
                {
                    UnsuccessfulTargetRequest(handle, TargetResponseCode.JOURNEY_COMPLETE);
					MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedDeliveries(++completedDeliveries));
                    DestroyDrone(handle.Request.droneId);
                    UpdateDroneMap();
                }
                else
                {
					MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedRoundTrips(++completedRoundTrips));

                    droneInfo.returning = true;
                    droneInfo.waypoints.Reverse();
                    droneInfo.nextWaypoint = 2;
					droneInfo.latestCheckinTime
                        = Time.time
                        + (SimulationSettings.DroneETAConstant
                           * Vector3.Distance(
                               droneInfo.waypoints[0].ToUnityVector(),
                               droneInfo.waypoints[1].ToUnityVector())
                           / SimulationSettings.MaxDroneSpeed);

                    droneMap.Remove(handle.Request.droneId);
                    droneMap.Add(handle.Request.droneId, droneInfo);
                    UpdateDroneMap();

                    //ignore 0 because that's the point that we've just reached
                    //saved as 2, but sending 1 back to drone - only 1 spatial update instead of 2 now
                    handle.Respond(new TargetResponse(droneInfo.waypoints[1], TargetResponseCode.SUCCESS));
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

    void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
		MetricsWriter.Send(new ControllerMetrics.Update().SetIncomingDeliveryRequests(++incomingRequests));

        if (deliveryRequestQueue.Count >= SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            handle.Respond(new DeliveryResponse(false));
        }
        else
        {
            deliveryRequestQueue.Enqueue(handle.Request);
            handle.Respond(new DeliveryResponse(true));
        }
    }

    void UpdateDeliveryRequestQueue()
    {
        DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<DeliveryRequest>(deliveryRequestQueue.ToArray())));
    }

    void PrintMetrics()
    {
		Debug.LogWarningFormat("METRICS C_{0} drones {1} queue {2} deliveries {3} fullTrips {4} fails {5} collisions {6} totalRequests {7}"
                               , gameObject.EntityId().Id
		                       , droneMap.Count
		                       , deliveryRequestQueue.Count
                               , completedDeliveries
		                       , completedRoundTrips
                               , failedDeliveries
                               , collisionsReported
		                       , incomingRequests);
    }

    void HandleCollision(Improbable.Entity.Component.ResponseHandle<Controller.Commands.Collision, CollisionRequest, CollisionResponse> handle)
    {
        handle.Respond(new CollisionResponse());

		MetricsWriter.Send(new ControllerMetrics.Update().SetCollisionsReported(++collisionsReported));

        DestroyDrone(handle.Request.droneId);
        DestroyDrone(handle.Request.colliderId);
        UpdateDroneMap();
    }

    void DestroyDrone(EntityId entityId)
    {
        droneMap.Remove(entityId);
        droneTranstructor.DestroyDrone(entityId);
    }

    void UpdateDroneMap()
    {
        ControllerWriter.Send(new Controller.Update().SetDroneMap(droneMap));
    }

    void TargetReplyFailure(ICommandErrorDetails errorDetails)
    {
        Debug.LogError("Unable to give drone new target");
    }

    private void IncrementNextWaypoint(EntityId droneId)
    {
        DroneInfo droneInfo;
        if(droneMap.TryGetValue(droneId, out droneInfo))
        {
            IncrementNextWaypoint(droneId, droneInfo);
        }
    }

    private void IncrementNextWaypoint(EntityId droneId, DroneInfo droneInfo)
    {
        droneInfo.nextWaypoint++;
		droneInfo.latestCheckinTime
            = Time.time
            + (SimulationSettings.DroneETAConstant
               * Vector3.Distance(
				   droneInfo.waypoints[droneInfo.nextWaypoint - 1].ToUnityVector(),
				   droneInfo.waypoints[droneInfo.nextWaypoint - 2].ToUnityVector())
               / SimulationSettings.MaxDroneSpeed);
        droneMap.Remove(droneId);
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();
    }

    void DroneDeploymentSuccess(EntityId droneId, DroneInfo droneInfo)
    {
        droneInfo.nextWaypoint++;
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();
    }

    void DroneDeploymentFailure()
    {
		MetricsWriter.Send(new ControllerMetrics.Update().SetFailedDeliveries(++failedDeliveries));
    }

    void HandleDeliveryRequest(DeliveryRequest request)
    {
        DroneInfo droneInfo;
		Vector2 random;

		random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
		Vector3f departurePoint = departuresPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);

		random = UnityEngine.Random.insideUnitCircle * SimulationSettings.DronePadRadius;
		Vector3f arrivalPoint = arrivalsPoint.ToSpatialVector3f() + new Vector3f(random.x, 0, random.y);

        //Debug.LogWarning("point to point plan");
        //for new flight plan
        droneInfo.nextWaypoint = 1;
        droneInfo.returning = false;
        droneInfo.waypoints = globalLayer.generatePointToPointPlan(
			departurePoint,
            request.destination);
		droneInfo.latestCheckinTime
            = Time.time
            + (SimulationSettings.DroneETAConstant
               * Vector3.Distance(
				   departurePoint.ToUnityVector(),
                   droneInfo.waypoints[1].ToUnityVector())
               / SimulationSettings.MaxDroneSpeed);

        //Debug.LogWarning("null check");
        if (droneInfo.waypoints == null)
        {
            // let scheduler know that this job can't be done
            DroneDeploymentFailure();
            return;
        }

		//0th index only useful as last point in return journey
		//so make sure 0th index == last location in journey == arrivalsPoint
		droneInfo.waypoints[0] = arrivalPoint;

        //create drone
        //if successful, add to droneMap
        //if failure, tell scheduler job couldn't be done
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(
			departurePoint.ToCoordinates(),
            droneInfo.waypoints[droneInfo.nextWaypoint],
            gameObject.EntityId(),
            SimulationSettings.MaxDroneSpeed);
        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
                 .OnSuccess((response) => DroneDeploymentSuccess(response.CreatedEntityId, droneInfo))
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
        if (deliveryRequestQueue.Count > 0
            && droneMap.Count < ControllerWriter.Data.maxDroneCount)
        {
            HandleDeliveryRequest(deliveryRequestQueue.Dequeue());
            UpdateDeliveryRequestQueue();
        }
    }

	void DroneMapPrune()
	{
		if (!ControllerWriter.Data.initialised)
		{
			return;
		}

		DroneInfo droneInfo;
		List<EntityId> toPrune = new List<EntityId>();

		foreach(EntityId droneId in droneMap.Keys)
		{
			if (droneMap.TryGetValue(droneId, out droneInfo))
			{
				if (Time.time > droneInfo.latestCheckinTime)
                {
					Debug.LogErrorFormat("Pruning Drone for taking too long. Drone {0} Delivered: {0}", droneId, droneInfo.returning);
					toPrune.Add(droneId);
                }
			}
		}

		foreach(EntityId droneId in toPrune)
		{
			DestroyDrone(droneId);
		}

		UpdateDroneMap();
	}
}
