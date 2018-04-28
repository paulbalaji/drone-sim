using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using System;
using System.Collections;
using UnityEngine;

public class RootSpawner : MonoBehaviour
{
    [Require]
    private Position.Writer PositionWriter;

	void Start()
	{
        InvokeRepeating("RootSpawnerTick", SimulationSettings.DroneSpawnerSpacing, SimulationSettings.DroneSpawnerSpacing);
	}

    void RootSpawnerTick()
    {
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            DroneSpawnerComponent.Commands.RequestNewTarget.Descriptor,
            new DroneSpawnRequest(),
            new EntityId((long) UnityEngine.Random.Range(1, SimulationSettings.ControllerCount))
        );
    }
}
