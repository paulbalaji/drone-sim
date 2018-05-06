using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
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
    private Controller.Writer ControllerWriter;

    [Require]
    private ControllerMetrics.Writer MetricsWriter;

    private float nextActionTime = 0.0f;

    DroneTranstructor droneTranstructor;

    GridGlobalLayer globalLayer;

    Improbable.Collections.Map<EntityId, DroneInfo> droneMap;

    Queue<TargetRequest> queue;

    bool stopSpawning = false;
    int completedDeliveries;

    private void OnEnable()
    {
        droneMap = ControllerWriter.Data.droneMap;
        queue = new Queue<TargetRequest>(ControllerWriter.Data.requestQueue);
        completedDeliveries = MetricsWriter.Data.completedDeliveries;

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(EnqueueTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();

        InvokeRepeating("ControllerTick", SimulationSettings.ControllerUpdateInterval, SimulationSettings.ControllerUpdateInterval);
        InvokeRepeating("PrintMetrics", SimulationSettings.ControllerMetricsInterval, SimulationSettings.ControllerMetricsInterval);
    }

    private void OnDisable()
    {
        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnCollision.DeregisterResponse();
    }

    void EnqueueTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        //Debug.LogWarning("CONTROLLER New Target Request");
        handle.Respond(new TargetResponse());
        queue.Enqueue(handle.Request);
        UpdateRequestQueue();
    }

    void UpdateRequestQueue()
    {
        ControllerWriter.Send(new Controller.Update().SetRequestQueue(new Improbable.Collections.List<TargetRequest>(queue.ToArray())));
    }

    void HandleCollision(Improbable.Entity.Component.ResponseHandle<Controller.Commands.Collision, CollisionRequest, CollisionResponse> handle)
    {
        handle.Respond(new CollisionResponse());

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

    void NextWaypointRequest(EntityId droneId, DroneInfo droneInfo)
    {
        //TODO: need to verify if the drone is actually at its target

        //Debug.LogWarning("is final waypoint?");
        //not final waypoint, get next waypoint
        if (droneInfo.waypoints.Count > droneInfo.nextWaypoint)
        {
            //Debug.LogWarning("send next waypoint back!");
            //SEND BACK 
            SpatialOS.Commands.SendCommand(
                ControllerWriter,
                DroneData.Commands.ReceiveNewTarget.Descriptor,
                new NewTargetRequest(droneInfo.waypoints[droneInfo.nextWaypoint]),
                droneId)
                     .OnSuccess((response) => IncrementNextWaypoint(droneId, droneInfo))
                     .OnFailure((response) => TargetReplyFailure(response));
            return;
        }

        DroneDeliveryComplete(droneId, droneInfo);
    }

    void TargetReplyFailure(ICommandErrorDetails errorDetails)
    {
        Debug.LogError("Unable to give drone new target");
    }

    void SendMetrics()
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedDeliveries(completedDeliveries));
    }

    void PrintMetrics()
    {
        Debug.LogFormat("METRICS Controller_{0} Completed_Deliveries {1}", gameObject.EntityId().Id, completedDeliveries);
    }

    void DroneDeliveryComplete(EntityId droneId, DroneInfo droneInfo)
    {
        if (droneInfo.returning)
        {
            completedDeliveries++;
            SendMetrics();
            //PrintMetrics();

            DestroyDrone(droneId);
        }
        else
        {
            droneInfo.returning = true;
            droneInfo.waypoints.Reverse();
            droneInfo.nextWaypoint = 0;

            droneMap.Remove(droneId);
            droneMap.Add(droneId, droneInfo);
            UpdateDroneMap();

            SpatialOS.Commands.SendCommand(
                ControllerWriter,
                DroneData.Commands.ReceiveNewTarget.Descriptor,
                new NewTargetRequest(droneInfo.waypoints[droneInfo.nextWaypoint]),
                droneId)
                    .OnSuccess((response) => IncrementNextWaypoint(droneId, droneInfo))
                    .OnFailure((response) => Debug.LogError("Unable to tell drone to return."));
        }
    }

    void UnableToDeliver(EntityId droneId)
    {
        //something went wrong so signal that back to drone!
        SpatialOS.Commands.SendCommand(
            ControllerWriter,
            DroneData.Commands.ReceiveNewTarget.Descriptor,
            new NewTargetRequest(new Vector3f(0, -1, 0)),
            droneId)
                 .OnFailure((response) => Debug.LogError("Unable to tell drone pathfinding failed."));

        // will also need to let scheduler know that pathfinding failed and that job needs to be given to another controller
    }

    private void IncrementNextWaypoint(EntityId droneId, DroneInfo droneInfo)
    {
        droneInfo.nextWaypoint++;
        droneMap.Remove(droneId);
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();
    }

    void HandleTargetRequest(TargetRequest request)
    {
        DroneInfo droneInfo;
        Vector3f nextTarget;

        //Debug.LogWarning("try get val");
        if (droneMap.TryGetValue(request.droneId, out droneInfo))
        {
            NextWaypointRequest(request.droneId, droneInfo);
            return;
        }

        if (request.destination.HasValue)
        {
            //Debug.LogWarning("point to point plan");
            //for new flight plan
            droneInfo.nextWaypoint = 0;
            droneInfo.returning = false;
            droneInfo.waypoints = globalLayer.generatePointToPointPlan(
                transform.position.ToSpatialVector3f(),
                request.destination.Value);

            //Debug.LogWarning("null check");
            if (droneInfo.waypoints == null)
            {
                UnableToDeliver(request.droneId);
                return;
            }

            droneMap.Add(request.droneId, droneInfo);
            UpdateDroneMap();

            //Debug.LogWarning("send first waypoint!");
            //SEND BACK 
            SpatialOS.Commands.SendCommand(
                ControllerWriter,
                DroneData.Commands.ReceiveNewTarget.Descriptor,
                new NewTargetRequest(droneInfo.waypoints[droneInfo.nextWaypoint]),
                request.droneId)
                     .OnSuccess((response) => IncrementNextWaypoint(request.droneId, droneInfo))
                     .OnFailure((response) => Debug.LogError("Unable to tell drone that pathfinding failed"));
            return;
        }
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
        if (queue.Count > 0)
        {
            //Debug.LogWarning("handling target request");
            HandleTargetRequest(queue.Dequeue());
            UpdateRequestQueue();
        }
    }
}
