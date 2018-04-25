using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
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

    private float nextActionTime = 0.0f;

    DroneTranstructor droneTranstructor;

    GridGlobalLayer globalLayer;

    Improbable.Collections.Map<EntityId, DroneInfo> droneMap;

    Queue<TargetRequest> queue;

    bool stopSpawning = false;

    private void OnEnable()
    {
        droneMap = ControllerWriter.Data.droneMap;
        queue = new Queue<TargetRequest>(ControllerWriter.Data.requestQueue);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(EnqueueTargetRequest);
        ControllerWriter.CommandReceiver.OnCollision.RegisterAsyncResponse(HandleCollision);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();

        InvokeRepeating("ControllerTick", SimulationSettings.ControllerUpdateInterval, SimulationSettings.ControllerUpdateInterval);
    }

    private void OnDisable()
    {
        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
        ControllerWriter.CommandReceiver.OnCollision.DeregisterResponse();
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

    void UpdateRequestQueue()
    {
        ControllerWriter.Send(new Controller.Update().SetRequestQueue(new Improbable.Collections.List<TargetRequest>(queue.ToArray())));
    }

    void EnqueueTargetRequest(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        //Debug.LogWarning("CONTROLLER New Target Request");
        handle.Respond(new TargetResponse());
        queue.Enqueue(handle.Request);
        UpdateRequestQueue();
    }

    void HandleTargetRequest(TargetRequest request)
    {
        DroneInfo droneInfo;

        //Debug.LogWarning("try get val");
        if (droneMap.TryGetValue(request.droneId, out droneInfo))
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
                    request.droneId)
                         .OnFailure((response) => Debug.LogError("Unable to give drone new target"));
                //TODO: OnSuccess / OnFailure

                droneInfo.nextWaypoint++;

                //stupidly you have to remove/add to update
                droneMap.Remove(request.droneId);
                droneMap.Add(request.droneId, droneInfo);
                UpdateDroneMap();

                return;
            }

            //if final waypoint, remove current flight plan
            droneMap.Remove(request.droneId);

            //for now just give it a new target and generate a random plan for that?
        }

        //Debug.LogWarning("point to point plan");
        //for new flight plan
        droneInfo.nextWaypoint = 1;

        //calculate the next final target
        //Debug.LogWarning("finding final destination");
        Vector3f nextTarget = request.target.HasValue
                                     ? request.target.Value 
                                     : new Vector3f(-request.location.x, 0, -request.location.z);

        //Debug.LogWarning("planning for final destination");
        droneInfo.waypoints = globalLayer.generatePointToPointPlan(request.location, nextTarget);

        //Debug.LogWarning("null check");
        if (droneInfo.waypoints == null)
        {
            //droneMap.Remove(request.droneId);
            //DestroyDrone(request.droneId);
            //return;

            //something went wrong so signal that back to drone!
            //TODO: OnSuccess / OnFailure + Send "failure" command back to drone
            SpatialOS.Commands.SendCommand(
                ControllerWriter,
                DroneData.Commands.ReceiveNewTarget.Descriptor,
                new NewTargetRequest(new Vector3f(0, -1, 0)),
                request.droneId)
                     .OnFailure((response) => Debug.LogError("Unable to tell drone it failed"));
            return;
        }

        droneMap.Add(request.droneId, droneInfo);
        UpdateDroneMap();

        //Debug.LogWarning("send first waypoint!");
        //SEND BACK 
        SpatialOS.Commands.SendCommand(
            ControllerWriter,
            DroneData.Commands.ReceiveNewTarget.Descriptor,
            new NewTargetRequest(droneInfo.waypoints[0]),
            request.droneId)
                 .OnFailure((response) => Debug.LogError("Unable to tell drone that pathfinding failed"));
        //TODO: OnSuccess / OnFailure
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

        //if (!stopSpawning)
        //{
        //    SpawnDrone(new Coordinates(400, 0, 400), new Vector3f(400, 0, 400), 50, 1);
        //    SpawnDrone(new Coordinates(400, 0, -400), new Vector3f(400, 0, -400), 50, 1);
        //    SpawnDrone(new Coordinates(-400, 0, -400), new Vector3f(-400, 0, -400), 50, 1);
        //    SpawnDrone(new Coordinates(-400, 0, 400), new Vector3f(-400, 0, 400), 50, 1);
        //    stopSpawning = true;
        //}

        //don't need to do anything if no requests in the queue
        if (queue.Count > 0)
        {
            //Debug.LogWarning("handling target request");
            HandleTargetRequest(queue.Dequeue());
            UpdateRequestQueue();
        }
    }

    void SpawnDrone(Coordinates spawn, Vector3f target, float speed = -1, float radius = -1)
    {
        if (speed < 0)
        {
            speed = UnityEngine.Random.Range(2, 10);
        }

        if (radius < 0)
        {
            radius = UnityEngine.Random.Range(0.5f, 2);
        }

        droneTranstructor.CreateDrone(spawn, target, speed, radius);
    }

    void SpawnCompletelyRandomDrone()
    {
        // TODO: check count < maxCount at the .OnSuccess stage as well
        // should be fine for now, but if you want to be more strict about limits
        uint currentCount = ControllerWriter.Data.droneCount;
        if (currentCount < ControllerWriter.Data.maxDroneCount)
        {
            var squareSize = SimulationSettings.squareSize;

            Coordinates spawn = new Coordinates(UnityEngine.Random.Range(-squareSize, squareSize), 0, UnityEngine.Random.Range(-squareSize, squareSize));
            Vector3f target = new Vector3f(UnityEngine.Random.Range(-squareSize, squareSize), 0, UnityEngine.Random.Range(-squareSize, squareSize));

            SpawnDrone(spawn, target);
        }
    }
}
