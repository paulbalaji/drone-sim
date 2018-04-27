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
public class DroneSpawner : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    DroneTranstructor droneTranstructor;
    Bitmap bitmap;

    private void OnEnable()
    {
        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();
        bitmap = gameObject.GetComponent<Bitmap>();

        InvokeRepeating("DroneSpawnerTick", gameObject.EntityId().Id * SimulationSettings.DroneSpawnerSpacing, SimulationSettings.DroneSpawnInterval);
    }

    void DroneSpawnerTick()
    {
        if (ControllerWriter.Data.initialised)
        {
            SpawnCompletelyRandomDrone();
        }
    }

    bool ValidPoint(float x, float z)
    {
        return bitmap.distanceToNoFlyZone(new Vector3f(x, 0, z)) > SimulationSettings.NFZ_PADDING_RAW;
    }

    Coordinates GetNonNFZPoint()
    {
        var squareSize = SimulationSettings.squareSize;

        float randX, randZ;

        do
        {
            randX = UnityEngine.Random.Range(-squareSize, squareSize);
            randZ = UnityEngine.Random.Range(-squareSize, squareSize);
        } while (!ValidPoint(randX, randZ));

        return new Coordinates(randX, 0, randZ);
    }

    void SpawnDrone(Coordinates spawn, Vector3f target, float speed = -1)
    {
        if (speed < 0)
        {
            speed = UnityEngine.Random.Range(5, SimulationSettings.MaxDroneSpeed);
        }

        droneTranstructor.CreateDrone(spawn, target, SimulationSettings.MaxDroneSpeed);
    }

    void SpawnCompletelyRandomDrone()
    {
        // TODO: check count < maxCount at the .OnSuccess stage as well
        // should be fine for now, but if you want to be more strict about limits
        if (ControllerWriter.Data.droneCount < ControllerWriter.Data.maxDroneCount)
        {
            var squareSize = SimulationSettings.squareSize;

            Coordinates spawn = GetNonNFZPoint();
            Vector3f target = GetNonNFZPoint().ToSpatialVector3f();

            SpawnDrone(spawn, target);
        }
    }
}
