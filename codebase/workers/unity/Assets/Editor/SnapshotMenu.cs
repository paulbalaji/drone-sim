using Assets.Gamelogic.Core;
using Assets.Gamelogic.EntityTemplates;
using Improbable;
using Improbable.Worker;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	public class SnapshotMenu : MonoBehaviour
	{
		[MenuItem("Improbable/Snapshots/Generate Default Snapshot")]
		private static void GenerateDefaultSnapshot()
		{
			var snapshotEntities = new Dictionary<EntityId, Entity>();
			var currentEntityId = 1;

			snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreatePlayerCreatorTemplate());
			snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateCubeTemplate());

			SaveSnapshot(snapshotEntities, "default");
		}

        [MenuItem("Improbable/Snapshots/Generate Phase 0 Snapshot")]
        private static void GeneratePhase0Snapshot()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreatePlayerCreatorTemplate());
            snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateServerNodeTemplate(new Coordinates(5, 0, 5)));

            snapshotEntities.Add(new EntityId(currentEntityId++), EntityTemplateFactory.CreateDroneTemplate(new Coordinates(10, 0, 10)));

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
