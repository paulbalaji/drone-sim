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
    public class Phase2Snapshots : MonoBehaviour
    {
        [MenuItem("Improbable/Snapshots/Phase2/justcontroller")]
        private static void GeneratePhase1SnapshotDev3v3()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            NFZTemplate[] nfzTemplates = {
                NFZTemplate.BASIC_ENCLOSURE
            };

            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateControllerTemplate(
                    new Coordinates(0, 0, 0),
                    new Vector3f(-1000, 0, 1000),
                    new Vector3f(1000, 0, -1000),
                    nfzTemplates
            ));

            currentEntityId = SnapshotMenu.DisplayNoFlyZones(nfzTemplates, snapshotEntities, currentEntityId);

            SnapshotMenu.SaveSnapshot(snapshotEntities, "phase2/advanced/justcontroller");
        }

        [MenuItem("Improbable/Snapshots/Phase2/basicnfz")]
        private static void basicnfz()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            NFZTemplate[] nfzTemplates = {
                NFZTemplate.BASIC_SQUARE
            };

            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateControllerTemplate(
                    new Coordinates(0, 0, 0),
                    new Vector3f(-1000, 0, 1000),
                    new Vector3f(1000, 0, -1000),
                    nfzTemplates
            ));

            currentEntityId = SnapshotMenu.DisplayNoFlyZones(nfzTemplates, snapshotEntities, currentEntityId);

            SnapshotMenu.SaveSnapshot(snapshotEntities, "phase2/advanced/basicnfz");
        }

        [MenuItem("Improbable/Snapshots/Phase2/nonfz")]
        private static void nonfz()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            NFZTemplate[] nfzTemplates = {
                //NFZTemplate.BASIC_ENCLOSURE
            };

            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateControllerTemplate(
                    new Coordinates(0, 0, 0),
                    new Vector3f(-1000, 0, 1000),
                    new Vector3f(1000, 0, -1000),
                    nfzTemplates
            ));

            currentEntityId = SnapshotMenu.DisplayNoFlyZones(nfzTemplates, snapshotEntities, currentEntityId);

            SnapshotMenu.SaveSnapshot(snapshotEntities, "phase2/advanced/nonfz");
        }
    }
}