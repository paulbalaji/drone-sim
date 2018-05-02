using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Worker;
using Improbable.Controller;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class LondonSnapshot : MonoBehaviour
    {
        [MenuItem("Improbable/Snapshots/LondonLarge")]
        private static void LondonLarge()
        {
            float maxX = 15750; //routable width is 31500m
            float maxZ = 7000; //routable height is 14000m

            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones = new Improbable.Collections.List<Improbable.Controller.NoFlyZone>();

            // NO FLY ZONES
            // start creating no fly zones from the editor
            NFZScript[] noFlyZoneScripts = FindObjectsOfType<NFZScript>();
            foreach (NFZScript noFlyZoneScript in noFlyZoneScripts)
            {
                noFlyZones.Add(noFlyZoneScript.GetNoFlyZone());
            }
            // end creation of no fly zones

            // CONTROLLERS
            // start placing controllers
            int firstController = currentEntityId;
            ControllerBehaviour[] controllerScripts = FindObjectsOfType<ControllerBehaviour>();
            foreach (ControllerBehaviour controllerScript in controllerScripts)
            {
                snapshotEntities.Add(
                    new EntityId(currentEntityId++),
                    EntityTemplateFactory.CreateControllerTemplate(
                        controllerScript.gameObject.transform.position.ToCoordinates(),
                        new Vector3f(-maxX, 0, maxZ),
                        new Vector3f(maxX, 0, -maxZ),
                        noFlyZones
                ));
            }
            int lastController = currentEntityId;
            // controller placement complete

            // make nfz nodes show up on the inspector map
            currentEntityId = ShowNoFlyZones(noFlyZones, snapshotEntities, currentEntityId);

            // SCHEDULER 
            // find and place scheduler
            RootSpawner rootSpawnerScript = FindObjectOfType<RootSpawner>();
            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateSchedulerTemplate(
                    rootSpawnerScript.gameObject.transform.position,
                    firstController,
                    lastController
                )
            );
            // end scheduler placement

            SnapshotMenu.SaveSnapshot(snapshotEntities, "london_large");
        }

        private static int ShowNoFlyZones(Improbable.Controller.NoFlyZone noFlyZone, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            foreach (Vector3f vertex in noFlyZone.vertices)
            {
                snapshotEntities.Add(
                    new EntityId(currentEntityId++),
                    EntityTemplateFactory.CreateNfzNodeTemplate(vertex.ToCoordinates())
                );
            }

            return currentEntityId;
        }

        private static int ShowNoFlyZones(Improbable.Collections.List<Improbable.Controller.NoFlyZone> noFlyZones, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            foreach (Improbable.Controller.NoFlyZone noFlyZone in noFlyZones)
            {
                currentEntityId = ShowNoFlyZones(noFlyZone, snapshotEntities, currentEntityId);
            }

            return currentEntityId;
        }
    }
}