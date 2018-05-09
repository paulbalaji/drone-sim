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
    int completedDeliveries;
    int collisionsReported;

    private float nextSpawnTime = 0;

    private void OnEnable()
    {
        droneMap = ControllerWriter.Data.droneMap;

        completedDeliveries = MetricsWriter.Data.completedDeliveries;
        collisionsReported = MetricsWriter.Data.collisionsReported;

        departuresPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerDepartureOffset;
        arrivalsPoint = transform.position.ToCoordinates() + SimulationSettings.ControllerArrivalOffset;

        deliveryRequestQueue = new Queue<DeliveryRequest>(DeliveryHandlerWriter.Data.requestQueue);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(HandleTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();

        UnityEngine.Random.InitState((int)gameObject.EntityId().Id);
        InvokeRepeating("ControllerTick", UnityEngine.Random.Range(0, SimulationSettings.RequestHandlerInterval), SimulationSettings.RequestHandlerInterval);
        InvokeRepeating("PrintMetrics", SimulationSettings.ControllerMetricsInterval, SimulationSettings.ControllerMetricsInterval);
    }

    private void OnDisable()
    {
        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnCollision.DeregisterResponse();

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
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
                handle.Respond(new TargetResponse(droneInfo.waypoints[droneInfo.nextWaypoint], true));
                IncrementNextWaypoint(handle.Request.droneId);
            }
            else
            {
                if (droneInfo.returning)
                {
                    completedDeliveries++;
                    SendMetrics();
                    DestroyDrone(handle.Request.droneId);
                }
                else
                {
                    droneInfo.returning = true;
                    droneInfo.waypoints.Reverse();
                    droneInfo.nextWaypoint = 1;

                    droneMap.Remove(handle.Request.droneId);
                    droneMap.Add(handle.Request.droneId, droneInfo);
                    UpdateDroneMap();

                    handle.Respond(new TargetResponse(droneInfo.waypoints[droneInfo.nextWaypoint], true));
                    IncrementNextWaypoint(handle.Request.droneId);
                }
            }
        }
        else
        {
            UnsuccessfulTargetRequest(handle);
        }
    }

    void UnsuccessfulTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        handle.Respond(new TargetResponse(new Vector3f(), false));
    }

    void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        handle.Respond(new DeliveryResponse());

        if (deliveryRequestQueue.Count < SimulationSettings.MaxDeliveryRequestQueueSize)
        {
            deliveryRequestQueue.Enqueue(handle.Request);
        }
        else
        {
            //tell controller this job can't be done
        }
    }

    void UpdateDeliveryRequestQueue()
    {
        DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<DeliveryRequest>(deliveryRequestQueue.ToArray())));
    }

    void SendMetrics()
    {
        MetricsWriter.Send(new ControllerMetrics.Update().SetCompletedDeliveries(completedDeliveries));
    }

    void PrintMetrics()
    {
        Debug.LogWarningFormat("METRICS Controller_{0} Completed_Deliveries {1} Linked_Drones {2} Collisions_Reported {3}"
                               , gameObject.EntityId().Id
                               , completedDeliveries
                               , droneMap.Count
                               , collisionsReported);
    }

    void HandleCollision(Improbable.Entity.Component.ResponseHandle<Controller.Commands.Collision, CollisionRequest, CollisionResponse> handle)
    {
        handle.Respond(new CollisionResponse());

        collisionsReported++;

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

    private bool DroneDeliveryComplete(EntityId droneId, DroneInfo droneInfo)
    {
        if (droneInfo.returning)
        {
            completedDeliveries++;
            SendMetrics();
            //PrintMetrics();

            DestroyDrone(droneId);
            return true;
        }

        droneInfo.returning = true;
        droneInfo.waypoints.Reverse();
        droneInfo.nextWaypoint = 1;

        droneMap.Remove(droneId);
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();

        return false;
    }

    void UnableToDeliver(EntityId droneId)
    {
        // will also need to let scheduler know that pathfinding failed and that job needs to be given to another controller
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
        droneMap.Remove(droneId);
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();
    }

    void DroneDeploymentSuccess(EntityId droneId, DroneInfo droneInfo)
    {
        droneInfo.nextWaypoint++;
        droneMap.Add(droneId, droneInfo);
        UpdateDroneMap();

        nextSpawnTime = Time.time + SimulationSettings.DroneSpawnInterval;
    }

    void DroneDeploymentFailure()
    {
        // tell scheduler that the job couldn't be done
    }

    void HandleDeliveryRequest(DeliveryRequest request)
    {
        DroneInfo droneInfo;

        //Debug.LogWarning("point to point plan");
        //for new flight plan
        droneInfo.nextWaypoint = 1;
        droneInfo.returning = false;
        droneInfo.waypoints = globalLayer.generatePointToPointPlan(
            transform.position.ToSpatialVector3f(),
            request.destination);

        //Debug.LogWarning("null check");
        if (droneInfo.waypoints == null)
        {
            // let scheduler know that this job can't be done
            DroneDeploymentFailure();
            return;
        }

        //create drone
        //if successful, add to droneMap
        //if failure, tell scheduler job couldn't be done
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(
            departuresPoint,
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
            //&& Time.time > nextSpawnTime)
        {
            HandleDeliveryRequest(deliveryRequestQueue.Dequeue());
            UpdateDeliveryRequestQueue();
        }
    }
}
