﻿using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

[WorkerType(WorkerPlatform.UnityWorker)]
public class ControllerBehaviour : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    private float nextActionTime = 0.0f;
    private float period = 1f;

    DroneTranstructor droneTranstructor;

    GridGlobalLayer globalLayer;

    Improbable.Collections.Map<EntityId, DroneInfo> droneMap;

    private void OnEnable()
    {
        

        ControllerWriter.DroneMapUpdated.AddAndInvoke(HandleAction);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();

        if (!ControllerWriter.Data.initialised)
        {
            globalLayer.InitGlobalLayer(ControllerWriter.Data.topLeft, ControllerWriter.Data.bottomRight);
            ControllerWriter.Send(new Controller.Update().SetInitialised(true));
        }
    }

    private void OnDisable()
    {
        ControllerWriter.DroneMapUpdated.Remove(HandleAction);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.DeregisterResponse();
    }

    void HandleAction(Improbable.Collections.Map<EntityId, DroneInfo> spatialDroneMap)
    {
        droneMap = spatialDroneMap;
    }

    void UpdateDroneMap()
    {
        ControllerWriter.Send(new Controller.Update().SetDroneMap(droneMap));
    }


    void CalculateNewTarget(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        DroneInfo droneInfo;

        if (droneMap.TryGetValue(handle.Request.droneId, out droneInfo))
        {
            //TODO: need to verify if the drone is actually at its target

            //not final waypoint, get next waypoint
            if (droneInfo.waypoints.Count > droneInfo.nextWaypoint) 
            {
                handle.Respond(new TargetResponse(droneInfo.waypoints[droneInfo.nextWaypoint]));

                droneInfo.nextWaypoint++;

                //stupidly you have to remove/add to update
                droneMap.Remove(handle.Request.droneId);
                droneMap.Add(handle.Request.droneId, droneInfo);
                UpdateDroneMap();

                return;
            }

            //if final waypoint, remove current flight plan
            droneMap.Remove(handle.Request.droneId);

            //for now just give it a new target and generate a random plan for that?
        }

        //for new flight plan
        droneInfo.nextWaypoint = 1;
        droneInfo.waypoints = globalLayer.generatePointToPointPlan(
            handle.Request.location,
            new Vector3f(-handle.Request.location.x, 0, -handle.Request.location.z));
        
        droneMap.Add(handle.Request.droneId, droneInfo);
        UpdateDroneMap();

        handle.Respond(new TargetResponse(droneInfo.waypoints[0]));
    }

    void Update()
    {
        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            //SpawnDrone();
        }
    }

    void SpawnDrone()
    {
        // TODO: check count < maxCount at the .OnSuccess stage as well
        // should be fine for now, but if you want to be more strict about limits
        uint currentCount = ControllerWriter.Data.droneCount;
        if (currentCount < ControllerWriter.Data.maxDroneCount)
        {
            var squareSize = SimulationSettings.squareSize;

            Coordinates spawn = new Coordinates(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
            Vector3f target = new Vector3f(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
            float speed = Random.Range(2, 10);
            float radius = Random.Range(0.5f, 2);

            droneTranstructor.CreateDrone(spawn, target, speed, radius);
        }
    }

    void DestroyDrone(EntityId entityId)
    {
        uint currentCount = ControllerWriter.Data.droneCount;
        if (currentCount > 0) {
            droneTranstructor.DestroyDrone(entityId);
        }
    }
}
