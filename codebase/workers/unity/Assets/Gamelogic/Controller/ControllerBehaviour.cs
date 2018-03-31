using Assets.Gamelogic.Core;
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

    [Require]
    private DroneSpawner.Writer DroneSpawnerWriter;

    [Require]
    private DroneDestroyer.Writer DroneDestroyerWriter;

    private float nextActionTime = 0.0f;
    private float period = 1f;

    private void OnEnable()
    {
        //register stuff
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);
    }

    private void OnDisable()
    {
        //deregister stuff
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);
    }

    void CalculateNewTarget(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        //calculate new target here
        float size = SimulationSettings.squareSize;
        Vector3f newTarget = new Vector3f(Random.Range(-size, size), 0, Random.Range(-size, size));

        //Debug.LogError("returning new target");

        //respond to drone with new target
        handle.Respond(new TargetResponse(newTarget));
    }

    void Update()
    {
        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            SpawnDrone();
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

            DroneSpawnerWriter.Send(new DroneSpawner.Update().AddSpawn(new SpawnData(spawn, target, speed, radius)));
        }
    }

    void DestroyDrone(EntityId entityId)
    {
        uint currentCount = ControllerWriter.Data.droneCount;
        if (currentCount > 0) {
            DroneDestroyerWriter.Send(new DroneDestroyer.Update().AddDestroy(new DestroyData(entityId)));
        }
    }
}
