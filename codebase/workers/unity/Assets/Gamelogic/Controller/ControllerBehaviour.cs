using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
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

    bool stopSpawning = false;

    private void OnEnable()
    {
        ControllerWriter.DroneMapUpdated.AddAndInvoke(HandleAction);

        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);

        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        globalLayer = gameObject.GetComponent<GridGlobalLayer>();
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
        handle.Respond(new TargetResponse());
        Debug.LogWarning("CONTROLLER New Target Request");

        DroneInfo droneInfo;

        Debug.LogWarning("try get val");
        if (droneMap.TryGetValue(handle.Request.droneId, out droneInfo))
        {
            //TODO: need to verify if the drone is actually at its target

            Debug.LogWarning("is final waypoint?");
            //not final waypoint, get next waypoint
            if (droneInfo.waypoints.Count > droneInfo.nextWaypoint) 
            {
                Debug.LogWarning("send next waypoint back!");
                //SEND BACK 
                SpatialOS.Commands.SendCommand(
                    ControllerWriter,
                    DroneData.Commands.ReceiveNewTarget.Descriptor,
                    new NewTargetRequest(droneInfo.waypoints[droneInfo.nextWaypoint]),
                    handle.Request.droneId)
                         .OnFailure((response) => Debug.LogError("Unable to give drone new target"));
                //TODO: OnSuccess / OnFailure

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

        Debug.LogWarning("point to point plan");
        //for new flight plan
        droneInfo.nextWaypoint = 1;
        droneInfo.waypoints = globalLayer.generatePointToPointPlan(
            handle.Request.location,
            new Vector3f(-handle.Request.location.x, 0, -handle.Request.location.z));

        Debug.LogWarning("null check");
        if (droneInfo.waypoints == null)
        {
            //something went wrong so signal that back to drone!
            //TODO: OnSuccess / OnFailure + Send "failure" command back to drone
            SpatialOS.Commands.SendCommand(
                ControllerWriter,
                DroneData.Commands.ReceiveNewTarget.Descriptor,
                new NewTargetRequest(new Vector3f(0, -1, 0)),
                handle.Request.droneId)
                     .OnFailure((response) => Debug.LogError("Unable to tell drone it failed"));
            return;
        }

        droneMap.Add(handle.Request.droneId, droneInfo);
        UpdateDroneMap();

        Debug.LogWarning("send first waypoint!");
        //SEND BACK 
        SpatialOS.Commands.SendCommand(
            ControllerWriter,
            DroneData.Commands.ReceiveNewTarget.Descriptor,
            new NewTargetRequest(droneInfo.waypoints[0]),
            handle.Request.droneId)
                 .OnFailure((response) => Debug.LogError("Unable to find path for drone"));
        //TODO: OnSuccess / OnFailure
    }

    void Update()
    {
        if (!ControllerWriter.Data.initialised)
        {
            Debug.LogError("call init global layer");
            globalLayer.InitGlobalLayer(ControllerWriter.Data.topLeft, ControllerWriter.Data.bottomRight);
            Debug.LogError("Global Layer Ready");
            ControllerWriter.Send(new Controller.Update().SetInitialised(true));
            return;
        }

        if (!stopSpawning)
        {
            SpawnDrone(new Coordinates(100, 0, 100), new Vector3f(100, 0, 100), 50, 1);
            SpawnDrone(new Coordinates(100, 0, -100), new Vector3f(100, 0, -100), 50, 1);
            stopSpawning = true;
        }

        //if (Time.time > nextActionTime)
        //{
        //    nextActionTime += period;

        //    //SpawnDrone();
        //}
    }

    void SpawnDrone(Coordinates spawn, Vector3f target, float speed = -1, float radius = -1)
    {
        if (speed < 0)
        {
            speed = Random.Range(2, 10);
        }

        if (radius < 0)
        {
            radius = Random.Range(0.5f, 2);
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

            Coordinates spawn = new Coordinates(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
            Vector3f target = new Vector3f(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));

            SpawnDrone(spawn, target);
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
