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
        [MenuItem("Improbable/Snapshots/London")]
        private static void London()
        {
            var snapshotEntities = new Dictionary<EntityId, Entity>();
            var currentEntityId = 1;

            NFZTemplate[] nfzTemplates = {
                NFZTemplate.BASIC_RECTANGLE
            };

            float maxX = 6000;
            float maxZ = 3500;
            float xStep = 2 * maxX / SimulationSettings.ControllerColumns;
            float zStep = 2 * maxZ / SimulationSettings.ControllerRows;

            float zCoord = -maxZ + zStep / 2;
            for (int i = 0; i < SimulationSettings.ControllerRows; i++)
            {
                float xCoord = -maxX + xStep / 2;
                for (int j = 0; j < SimulationSettings.ControllerColumns; j++)
                {
                    snapshotEntities.Add(
                        new EntityId(currentEntityId++),
                        EntityTemplateFactory.CreateControllerTemplate(
                            new Coordinates(xCoord, 0, zCoord),
                            new Vector3f(-maxX, 0, maxZ),
                            new Vector3f(maxX, 0, -maxZ),
                            nfzTemplates
                    ));
                    xCoord += xStep;
                }

                zCoord += zStep;
            }

            currentEntityId = SnapshotMenu.DisplayNoFlyZones(nfzTemplates, snapshotEntities, currentEntityId);

            snapshotEntities.Add(
                new EntityId(currentEntityId++),
                EntityTemplateFactory.CreateSchedulerTemplate(
                    new Vector3(0, 0, 0)
                )
            );

            SnapshotMenu.SaveSnapshot(snapshotEntities, "london");
        }
    }
}