using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Core;
using Improbable.Player;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Unity.Core.Acls;
using Improbable.Worker;
using Quaternion = UnityEngine.Quaternion;
using UnityEngine;
using Improbable.Unity.Entity;
using Improbable.Collections;

public class EntityTemplateFactory : MonoBehaviour
{
    public static Entity CreatePlayerCreatorTemplate()
    {
        var playerCreatorEntityTemplate = EntityBuilder.Begin()
            .AddPositionComponent(Improbable.Coordinates.ZERO.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.PlayerCreatorPrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new PlayerCreation.Data(), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new ClientEntityStore.Data(new Map<string, EntityId>()), CommonRequirementSets.PhysicsOnly)
            .Build();

        return playerCreatorEntityTemplate;
    }

    public static Entity CreatePlayerTemplate(string clientId, EntityId playerCreatorId)
    {
        var playerTemplate = EntityBuilder.Begin()
            .AddPositionComponent(Improbable.Coordinates.ZERO.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.PlayerPrefabName)
            .SetPersistence(false)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new ClientAuthorityCheck.Data(), CommonRequirementSets.SpecificClientOnly(clientId))
            .AddComponent(new ClientConnection.Data(SimulationSettings.TotalHeartbeatsBeforeTimeout, clientId, playerCreatorId), CommonRequirementSets.PhysicsOnly)
            .Build();

        return playerTemplate;
    }

    public static Entity CreateCubeTemplate()
    {
        var cubeTemplate = EntityBuilder.Begin()
            .AddPositionComponent(Improbable.Coordinates.ZERO.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.CubePrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .Build();

        return cubeTemplate;
    }

    public static Entity CreateControllerTemplate(Improbable.Coordinates spawnPoint, Vector3d topLeft, Vector3d bottomRight, NFZTemplate[] templates)
    {
        List<Improbable.Controller.NoFlyZone> nfzs = new List<Improbable.Controller.NoFlyZone>();
        foreach(NFZTemplate template in templates)
        {
            nfzs.Add(NFZ_Templates.GetNoFlyZone(template));
        }

        var controllerTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawnPoint.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.ControllerPrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new Controller.Data(0, SimulationSettings.MaxDroneCountPerController, new Map<EntityId, DroneInfo>(), false, topLeft, bottomRight), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new GlobalLayer.Data(nfzs), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new BitmapComponent.Data(new Vector3d(), new Vector3d(), 0, 0, new List<GridType>(), false), CommonRequirementSets.PhysicsOnly)
            .Build();

        return controllerTemplate;
    }

    public static Entity CreateDroneTemplate(Improbable.Coordinates spawnPoint, Vector3f initialTarget, float droneSpeed, float hitRadius)
    {
        var droneTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawnPoint.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.DronePrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new DroneData.Data(initialTarget, droneSpeed, hitRadius, TargetPending.REQUEST), CommonRequirementSets.PhysicsOnly)
            .Build();

        return droneTemplate;
    }
}
