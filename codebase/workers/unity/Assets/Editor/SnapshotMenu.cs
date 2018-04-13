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
	public class SnapshotMenu : MonoBehaviour
	{
        [MenuItem("Improbable/Snapshots/Generate Phase 1 Snapshot - 1 BASIC")]
        private static void GeneratePhase1SnapshotDev()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            NFZTemplate[] nfzTemplates = {
                NFZTemplate.BASIC
            };

            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateControllerTemplate(
                    new Coordinates(0, 0, 0),
                    new Vector3f(-1000, 0, 1000),
                    new Vector3f(1000, 0, -1000),
                    nfzTemplates
            ));

            currentEntityId = DisplayNoFlyZones(nfzTemplates, snapshotEntities, currentEntityId);

            //Coordinates spawn = new Coordinates(50, 0, 50);
            //snapshotEntities.Add(
            //    new EntityId(currentEntityId++),
            //    EntityTemplateFactory.CreateDroneTemplate(spawn, spawn.ToSpatialVector3f(), 2, 1)
            //);

            //spawn = new Coordinates(-50, 0, -50);
            //snapshotEntities.Add(
            //    new EntityId(currentEntityId++),
            //    EntityTemplateFactory.CreateDroneTemplate(spawn, spawn.ToSpatialVector3f(), 2, 1)
            //);

            SaveSnapshot(snapshotEntities, "phase1_basic");
        }

        private static int DisplayNoFlyZone(NFZTemplate template, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            float[] points = NFZ_Templates.getPoints(template);
            for (int i = 0; i < points.Length; i += 2)
            {
                snapshotEntities.Add(
                    new EntityId(currentEntityId++),
                    EntityTemplateFactory.CreateNfzNodeTemplate(new Coordinates(points[i], 0, points[i+1]))
                );
            }

            return currentEntityId;
        }

        private static int DisplayNoFlyZones(NFZTemplate[] templates, Dictionary<EntityId, Entity> snapshotEntities, int currentEntityId)
        {
            foreach(NFZTemplate template in templates)
            {
                currentEntityId = DisplayNoFlyZone(template, snapshotEntities, currentEntityId);
            }

            return currentEntityId;
        }

        //[MenuItem("Improbable/Snapshots/Generate Phase 0 DEV Snapshot")]
        //private static void GeneratePhase0SnapshotsDev()
        //{
            //var snapshotEntities = new Dictionary<EntityId, Entity>();
            //var currentEntityId = 1;
            //var numDrones = SimulationSettings.numDrones;
            //var squareSize = SimulationSettings.squareSize;

            ////snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreatePlayerCreatorTemplate());
            //snapshotEntities.Add(
            //    new EntityId(currentEntityId++), 
            //    EntityTemplateFactory.CreateControllerTemplate(
            //        new Coordinates(0, 0, 0), 
            //        new Vector3d(-1000, 0, 1000),
            //        new Vector3d(1000, 0, -1000),
            //        new NFZTemplate[]{})
            //);

            //var numDrones = SimulationSettings.numDrones;
            //var squareSize = SimulationSettings.squareSize;

            //for (int i = 0; i < numDrones; i++)
            //{
            //    Coordinates spawn = new Coordinates(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
            //    Vector3f target = new Vector3f(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
            //    float speed = Random.Range(2, 10);
            //    float radius = Random.Range(0.5f, 2);
            //    snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateDroneTemplate(spawn, target, speed, radius));
            //}

            //SaveSnapshot(snapshotEntities, "phase0dev");
        //}

        //[MenuItem("Improbable/Snapshots/Generate Phase 0 DEPLOY Snapshot")]
        //private static void GeneratePhase0Snapshots()
        //{
        //    //snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreatePlayerCreatorTemplate());
        //    //snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateServerNodeTemplate(new Coordinates(5, 0, 5)));

        //    //GenerateSinglePhase0Snapshot(10, 10);
        //    //GenerateSinglePhase0Snapshot(20, 20);
        //    //GenerateSinglePhase0Snapshot(50, 50);
        //    //GenerateSinglePhase0Snapshot(100, 50);
        //    //GenerateSinglePhase0Snapshot(100, 100);
        //    //GenerateSinglePhase0Snapshot(500, 100);
        //    GenerateSinglePhase0Snapshot(1000, 800);
        //}

        private static void GenerateSinglePhase0Snapshot(int numDrones, int squareSize)
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            for (int i = 0; i < numDrones; i++)
            {
                Coordinates spawn = new Coordinates(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
                Vector3f target = new Vector3f(Random.Range(-squareSize, squareSize), 0, Random.Range(-squareSize, squareSize));
                float speed = Random.Range(2, 10);
                float radius = Random.Range(0.5f, 2);
                snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateDroneTemplate(spawn, target, speed, radius));
            }

            //SaveSnapshot(snapshotEntities, "phase0_d" + numDrones + "s" + squareSize);
            SaveSnapshot(snapshotEntities, "phase0");
        }

        private static void SaveSnapshot(IDictionary<EntityId, Entity> snapshotEntities, string snapshotName)
        {
            string snapshotPath = Application.dataPath + "/../../../snapshots/" + snapshotName + ".snapshot";

            File.Delete(snapshotPath);
            using (SnapshotOutputStream stream = new SnapshotOutputStream(snapshotPath))
            {
                foreach (var kvp in snapshotEntities)
                {
                    var error = stream.WriteEntity(kvp.Key, kvp.Value);
                    if (error.HasValue)
                    {
                        Debug.LogErrorFormat("Failed to generate initial world snapshot: {0}", error.Value);
                        return;
                    }
                }
            }

            Debug.LogFormat("Successfully generated initial world snapshot at {0}", snapshotPath);
        }

		private static void SaveSnapshot(IDictionary<EntityId, Entity> snapshotEntities)
		{
            SaveSnapshot(snapshotEntities, "default");
		}
	}
}
