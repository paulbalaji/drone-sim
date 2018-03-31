using Assets.Gamelogic.EntityTemplates;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;

public class DroneTranstructor : MonoBehaviour
{
    [Require]
    private Position.Writer PositionWriter;

    [Require]
    private DroneSpawner.Reader DroneSpawnerReader;

    [Require]
    private DroneDestroyer.Reader DroneDestroyerReader;

	private void OnEnable()
	{
        DroneSpawnerReader.SpawnTriggered.Add(CreateDrone);
        DroneDestroyerReader.DestroyTriggered.Add(DestroyDrone);
	}

    private void OnDisable()
    {
        DroneSpawnerReader.SpawnTriggered.Remove(CreateDrone);
        DroneDestroyerReader.DestroyTriggered.Remove(DestroyDrone);
    }

    private void CreateDrone(SpawnData spawnData)
    {
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(
            spawnData.position,
            spawnData.target,
            spawnData.speed,
            spawnData.radius);

        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate);
    }

    private void DestroyDrone(DestroyData destroyData)
    {
        SpatialOS.Commands.DeleteEntity(PositionWriter, destroyData.entityId);
    }

}
