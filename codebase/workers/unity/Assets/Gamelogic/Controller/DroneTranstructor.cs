using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;

public class DroneTranstructor : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

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

        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
                 .OnSuccess(CreateDroneSuccess);
    }

    void CreateDroneSuccess(Improbable.Unity.Core.EntityQueries.CreateEntityResult response)
    {
        EntityId entityId = response.CreatedEntityId;
        ControllerWriter.Send(new Controller.Update().SetDroneCount(ControllerWriter.Data.droneCount + 1));
    }

    private void DestroyDrone(DestroyData destroyData)
    {
        SpatialOS.Commands.DeleteEntity(PositionWriter, destroyData.entityId)
                 .OnSuccess(DestroyDroneSuccess);
    }

    void DestroyDroneSuccess(Improbable.Unity.Core.EntityQueries.DeleteEntityResult response)
    {
        ControllerWriter.Send(new Controller.Update().SetDroneCount(ControllerWriter.Data.droneCount - 1));
    }
}
