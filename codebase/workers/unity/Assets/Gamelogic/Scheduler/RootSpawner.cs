using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Scheduler;
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

    [Require]
    private Scheduler.Writer SchedulerWriter;

    private int controllerNum;

	void Start()
	{
        InvokeRepeating("RootSpawnerTick", SimulationSettings.DroneSpawnerSpacing, SimulationSettings.DroneSpawnerSpacing);
	}

	private void OnEnable()
	{
        controllerNum = SchedulerWriter.Data.firstController;
	}

	void RootSpawnerTick()
    {
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            DroneSpawnerComponent.Commands.RequestNewTarget.Descriptor,
            new DroneSpawnRequest(),
            new EntityId(controllerNum++)
        );

        if (controllerNum > SchedulerWriter.Data.lastController)
        {
            controllerNum = SchedulerWriter.Data.firstController;
        }
    }
}
