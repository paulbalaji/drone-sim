using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;

[WorkerType(WorkerPlatform.UnityWorker)]
public class DroneTranstructor : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    [Require]
    private Position.Writer PositionWriter;

    public void CreateDrone(Coordinates position, Vector3f target, float speed)
    {
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(position, target, gameObject.EntityId(), speed);

        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
                 .OnSuccess(CreateDroneSuccess);
    }

    void CreateDroneSuccess(Improbable.Unity.Core.EntityQueries.CreateEntityResult response)
    {
        EntityId entityId = response.CreatedEntityId;

        //Debug.LogWarning("DRONE COUNT FOR CONTROLLER #" + gameObject.EntityId().ToString() + ": " + ControllerWriter.Data.droneMap.Keys.Count);
    }

    public void DestroyDrone(EntityId entityId)
    {
        Debug.LogWarning("Destroying Drone, EntityId: " + entityId);
        SpatialOS.Commands.DeleteEntity(PositionWriter, entityId);
    }
}