using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Core;
using Improbable.Player;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Scheduler;
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

    public static Entity CreateNfzNodeTemplate(Coordinates spawn)
    {
        var nfzTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawn, CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.NfzNodePrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .Build();

        return nfzTemplate;
    }

    public static Entity CreateControllerTemplate(Improbable.Coordinates spawnPoint, Vector3f topLeft, Vector3f bottomRight, List<Improbable.Controller.NoFlyZone> nfzs)
    {
        var controllerTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawnPoint.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.ControllerPrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new Controller.Data(SimulationSettings.MaxDroneCountPerController, new Map<EntityId, DroneInfo>(), false, topLeft, bottomRight, new List<TargetRequest>()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new GlobalLayer.Data(nfzs), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new BitmapComponent.Data(topLeft, bottomRight, 0, 0, 0, 0, new Improbable.Collections.Map<int, GridType>(), false), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new ReactiveLayer.Data(), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new DeliveryHandler.Data(), CommonRequirementSets.PhysicsOnly)
            .Build();

        return controllerTemplate;
    }

    public static Entity CreateControllerTemplate(Improbable.Coordinates spawnPoint, Vector3f topLeft, Vector3f bottomRight, NFZTemplate[] templates)
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
            .AddComponent(new Controller.Data(SimulationSettings.MaxDroneCountPerController, new Map<EntityId, DroneInfo>(), false, topLeft, bottomRight, new List<TargetRequest>()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new GlobalLayer.Data(nfzs), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new BitmapComponent.Data(topLeft, bottomRight, 0, 0, 0, 0, new Improbable.Collections.Map<int, GridType>(), false), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new ReactiveLayer.Data(), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new DeliveryHandler.Data(), CommonRequirementSets.PhysicsOnly)
            .Build();

        return controllerTemplate;
    }

    public static Entity CreateDroneTemplate(Improbable.Coordinates spawnPoint, Vector3f initialTarget, EntityId controller, float droneSpeed = SimulationSettings.MaxDroneSpeed)
    {
        var droneTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawnPoint.ToUnityVector(), CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.DronePrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new DroneData.Data(initialTarget, Mathf.Min(SimulationSettings.MaxDroneSpeed, droneSpeed), new Vector3f(), TargetPending.REQUEST, DroneStatus.JUST_SPAWNED, new Vector3f(0, -1, 0), controller), CommonRequirementSets.PhysicsOnly)
            .Build();

        return droneTemplate;
    }

    public static Entity CreateSchedulerTemplate(Vector3 spawnPoint, int firstController, int lastController, List<Improbable.Controller.NoFlyZone> nfzs, List<ControllerInfo> controllers)
    {
        var schedulerTemplate = EntityBuilder.Begin()
            .AddPositionComponent(spawnPoint, CommonRequirementSets.PhysicsOnly)
            .AddMetadataComponent(entityType: SimulationSettings.SchedulerPrefabName)
            .SetPersistence(true)
            .SetReadAcl(CommonRequirementSets.PhysicsOrVisual)
            .AddComponent(new Rotation.Data(Quaternion.identity.ToNativeQuaternion()), CommonRequirementSets.PhysicsOnly)
            .AddComponent(new Scheduler.Data(firstController, lastController, nfzs, controllers), CommonRequirementSets.PhysicsOnly)
            .Build();

        return schedulerTemplate;
    }
}
